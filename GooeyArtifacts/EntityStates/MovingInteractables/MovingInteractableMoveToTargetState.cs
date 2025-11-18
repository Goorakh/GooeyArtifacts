using GooeyArtifacts.Utils;
using GooeyArtifacts.Utils.Extensions;
using RoR2;
using RoR2.ConVar;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.EntityStates.MovingInteractables
{
    [EntityStateType]
    public sealed class MovingInteractableMoveToTargetState : MovingInteractableBaseState
    {
        static readonly BoolConVar _cvDrawPathData = new BoolConVar("goo_draw_interactable_paths", ConVarFlags.SenderMustBeServer, "0", "");

        public Vector3 Destination;

        Vector3 _startPosition;
        Quaternion _startRotation = Quaternion.identity;

        float _moveSpeed = 10f;
        float _rotationSpeed = 135;

        float _stepHeight = 2f;
        float _maxStepRotation = 35f;

        Path _path;
        PathTraveller _pathTraveller;

        float _totalPathDistance;

        float _estimatedTimeRemaining;

        Vector3 _targetPosition;
        Vector3 _directTargetPosition;
        Quaternion _targetRotation = Quaternion.identity;

        bool _targetAtEnd;

        bool _hasCreatedPathVisualizers;
        readonly List<DebugOverlay.MeshDrawer> _pathDrawers = [];
        DebugOverlay.MeshDrawer _currentPositionVisualizer;
        DebugOverlay.MeshDrawer _currentTargetPositionVisualizer;

        bool _areVisualizersActive;

        public override void OnEnter()
        {
            base.OnEnter();

            _startPosition = transform.position;
            _startRotation = transform.rotation;

            if (isAuthority)
            {
                if (spawnCard)
                {
                    _moveSpeed = Mathf.Max(5f, Util.Remap(spawnCard.directorCreditCost, 0f, 50f, 10f, 7.5f));
                }
                else
                {
                    _moveSpeed = 10f;
                }

                if (Util.GuessRenderBoundsMeshOnly(gameObject, out Bounds prefabBounds))
                {
                    Vector3 size = prefabBounds.size;

                    float maxWidth = Mathf.Max(size.x, size.z);
                    float height = size.y;

                    float sizeCoefficient;
                    if (maxWidth > height)
                    {
                        sizeCoefficient = height / maxWidth;
                    }
                    else
                    {
                        sizeCoefficient = Mathf.Sqrt(maxWidth / height);
                    }

                    _moveSpeed *= sizeCoefficient;
                    _maxStepRotation *= sizeCoefficient;
                }

                _rotationSpeed = Mathf.Clamp(Util.Remap(_moveSpeed, 0f, 10f, 30f, 135f), 30f, 180f);

                if (tryInitializePath())
                {
                    _totalPathDistance = _pathTraveller.Path.CalculateTotalDistance();
                }
                else
                {
                    Log.Debug($"{Util.GetGameObjectHierarchyName(gameObject)} failed to initialize path to target position, resetting state");
                    outer.SetNextStateToMain();
                }
            }
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);

            writer.Write(_moveSpeed);
            writer.Write(_totalPathDistance);
            writer.Write(_maxStepRotation);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);

            _moveSpeed = reader.ReadSingle();
            _totalPathDistance = reader.ReadSingle();
            _maxStepRotation = reader.ReadSingle();
        }

        bool tryInitializePath()
        {
            SceneInfo sceneInfo = SceneInfo.instance;
            if (!sceneInfo)
                return false;

            NodeGraph nodeGraph = sceneInfo.GetNodeGraph(nodeGraphType);
            if (!nodeGraph)
                return false;

            NodeGraph.PathRequest pathRequest = new NodeGraph.PathRequest
            {
                startPos = transform.position,
                endPos = Destination,
                hullClassification = hullSize,
                maxJumpHeight = float.PositiveInfinity,
                maxSpeed = _moveSpeed,
                path = new Path(nodeGraph)
            };

            PathTask pathTask = nodeGraph.ComputePath(pathRequest);
            if (!pathTask.wasReachable || pathTask.status != PathTask.TaskStatus.Complete)
                return false;

            Path path = pathTask.path;
            path.RemoveDuplicateWaypoints();
            if (path.waypointsCount < 1)
                return false;

            _path = path;
            _pathTraveller = new PathTraveller(_path, gameObject);

            // TODO: Account for RoR2.OccupyNearbyNodes component
            if (occupyPosition)
            {
                float nodeSearchDistance = HullDef.Find(hullSize).radius * 5f;

                NodeGraph.NodeIndex currentOccupiedNode = nodeGraph.FindClosestNode(transform.position, hullSize, nodeSearchDistance);
                if (currentOccupiedNode != NodeGraph.NodeIndex.invalid)
                {
                    NodeUtils.SetNodeOccupied(nodeGraph, currentOccupiedNode, false);
                }

                NodeGraph.NodeIndex destinationNode = nodeGraph.FindClosestNode(Destination, hullSize, nodeSearchDistance);
                if (destinationNode != NodeGraph.NodeIndex.invalid)
                {
                    NodeUtils.SetNodeOccupied(nodeGraph, destinationNode, true);
                }
            }

            return true;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!gameObject || !transform)
                return;

            if (isAuthority)
            {
                fixedUpdateAuthority();
            }
            else
            {
                _estimatedTimeRemaining = Mathf.Max(0f, (_totalPathDistance / _moveSpeed) - fixedAge);
            }

            float stepMagnitude = Mathf.Sqrt(Mathf.Min(1f, fixedAge)
                                             * Mathf.Clamp01(Util.Remap(_estimatedTimeRemaining, 0f, 2.5f, 0f, 1f)));

            float stepValue = 4f * Mathf.PI * fixedAge;

            if (transformSyncController)
            {
                Quaternion currentRotation = transform.rotation * Quaternion.Inverse(transformSyncController.RotationOffset);

                transformSyncController.PositionOffset = currentRotation * new Vector3(0f, Mathf.Abs(Mathf.Sin(stepValue)) * _stepHeight * stepMagnitude, 0f);
                transformSyncController.RotationOffset = Quaternion.AngleAxis(Mathf.Sin(stepValue + (Mathf.PI / 2f)) * _maxStepRotation * stepMagnitude, Vector3.forward);
            }
        }

        void fixedUpdateAuthority()
        {
            if (_path == null || _pathTraveller == null)
            {
                outer.SetNextStateToMain();
                return;
            }

            setVisualizersActive(_cvDrawPathData.value);

            float moveDelta = _moveSpeed * Time.fixedDeltaTime;
            if (!_targetAtEnd)
            {
                PathTraveller.TravelData travelData = _pathTraveller.AdvancePosition(moveDelta);

                _directTargetPosition = travelData.CurrentPosition;

                Vector3 groundPosition = _directTargetPosition;
                Vector3 groundNormal = travelData.InterpolatedNormal;

                if (tryFindProperGroundPosition(groundPosition, -travelData.InterpolatedNormal, out Vector3 properGroundNormal, out Vector3 properGroundPosition))
                {
                    groundPosition = properGroundPosition;
                    groundNormal = properGroundNormal;
                }

                if (travelData.Direction.sqrMagnitude > Mathf.Epsilon)
                {
                    _targetRotation = Util.QuaternionSafeLookRotation(travelData.Direction, groundNormal);
                }

                _targetPosition = groundPosition;
                _targetAtEnd = travelData.IsAtEnd;

                _estimatedTimeRemaining = travelData.RemainingTotalDistance / _moveSpeed;
            }
            else
            {
                _estimatedTimeRemaining = 0f;
            }

            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            if (transformSyncController)
            {
                position -= transformSyncController.PositionOffset;
                rotation *= Quaternion.Inverse(transformSyncController.RotationOffset);
            }

            float positionSmoothSpeed = _moveSpeed;

            // Add speed to catch up if far away
            positionSmoothSpeed += Mathf.Clamp((_targetPosition - position).sqrMagnitude / (5f * 5f), 1f, 5f);

            // Add speed when close to end
            positionSmoothSpeed += Mathf.Max(0f, Util.Remap(_estimatedTimeRemaining, 0f, 1f, 5f, 0f));

            position = Vector3.MoveTowards(position, _targetPosition, positionSmoothSpeed * Time.fixedDeltaTime);
            rotation = Quaternion.RotateTowards(rotation, _targetRotation, _rotationSpeed * Time.fixedDeltaTime);

            _currentTargetPositionVisualizer?.transform.SetPositionAndRotation(_targetPosition, _targetRotation);

            _currentPositionVisualizer?.transform.SetPositionAndRotation(position, rotation);

            Vector3 offsetPosition = position;
            Quaternion offsetRotation = rotation;
            
            if (transformSyncController)
            {
                offsetPosition += transformSyncController.PositionOffset;
                offsetRotation *= transformSyncController.RotationOffset;
            }

            transform.position = offsetPosition;
            transform.rotation = offsetRotation;

            if (_targetAtEnd && (position - _targetPosition).sqrMagnitude <= 0.1f)
            {
                Vector3 targetRotationOffset = _startRotation.eulerAngles;
                targetRotationOffset.y = _targetRotation.eulerAngles.y + UnityEngine.Random.Range(-15f, 15f);

                Quaternion targetRotation = Quaternion.Euler(targetRotationOffset);
                Vector3 targetPosition = _directTargetPosition;

                outer.SetNextState(new MovingInteractableSettleState
                {
                    TargetRotation = targetRotation,
                    TargetPosition = targetPosition
                });
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            if (transformSyncController)
            {
                transformSyncController.PositionOffset = Vector3.zero;
                transformSyncController.RotationOffset = Quaternion.identity;
            }

            _path?.Dispose();
            _path = null;

            foreach (DebugOverlay.MeshDrawer drawer in _pathDrawers)
            {
                try
                {
                    drawer.Dispose();
                }
                catch (NullReferenceException)
                {
                    if (drawer.transform)
                    {
                        Destroy(drawer.transform.gameObject);
                    }
                }
            }

            _pathDrawers.Clear();

            _currentPositionVisualizer = null;
            _currentTargetPositionVisualizer = null;
        }

        bool tryFindProperGroundPosition(Vector3 position, Vector3 down, out Vector3 normal, out Vector3 properPosition)
        {
            float radius = HullDef.Find(hullSize).radius;

            RaycastHit closestHit = default;
            float closestHitSqrDistance = float.PositiveInfinity;

            const float MAX_SEARCH_DISTANCE = 10f;

            int hitCount = HGPhysics.SphereCastAll(out RaycastHit[] hits, position + (-down * MAX_SEARCH_DISTANCE), radius, down, MAX_SEARCH_DISTANCE * 2f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);

            try
            {
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hitCandidate = hits[i];

                    // UnityDocs: For colliders that overlap the sphere at the start of the sweep, RaycastHit.normal is set opposite to the direction of the sweep, RaycastHit.distance is set to zero, and the zero vector gets returned in RaycastHit.point
                    if (hitCandidate.distance <= 0f)
                        continue;

                    // Not using hit.distance here since we don't care about the distance from the ray origin,
                    // we only care about how far away the hit point is from the *current* position
                    float hitSqrDistance = (hitCandidate.point - position).sqrMagnitude;
                    if (hitSqrDistance >= closestHitSqrDistance)
                        continue;

                    if (TransformUtils.IsPartOfEntity(hitCandidate.transform, gameObject))
                        continue;

                    int overlapCount = HGPhysics.OverlapSphere(out Collider[] overlapColliders, hitCandidate.point + (hitCandidate.normal * radius), radius / 2f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);

                    try
                    {
                        bool overlapsWithTerrain = false;

                        for (int j = 0; j < overlapCount; j++)
                        {
                            Collider overlapCollider = overlapColliders[j];
                            if (overlapCollider && !TransformUtils.IsPartOfEntity(overlapCollider.transform, gameObject))
                            {
                                overlapsWithTerrain = true;
                                break;
                            }
                        }

                        if (overlapsWithTerrain)
                            continue;
                    }
                    finally
                    {
                        HGPhysics.ReturnResults(overlapColliders);
                    }

                    closestHit = hitCandidate;
                    closestHitSqrDistance = hitSqrDistance;
                }

                if (float.IsInfinity(closestHitSqrDistance))
                {
                    normal = -down;
                    properPosition = position;
                    return false;
                }

                normal = closestHit.normal;
                properPosition = closestHit.point;
                return true;
            }
            finally
            {
                HGPhysics.ReturnResults(hits);
            }
        }

        void setVisualizersActive(bool active)
        {
            if (_areVisualizersActive == active)
                return;

            _areVisualizersActive = active;

            if (_hasCreatedPathVisualizers)
            {
                foreach (DebugOverlay.MeshDrawer pathDrawer in _pathDrawers)
                {
                    pathDrawer.enabled = active;
                }
            }
            else
            {
                if (active)
                {
                    NodeGraph nodeGraph = SceneInfo.instance ? SceneInfo.instance.GetNodeGraph(nodeGraphType) : null;
                    if (nodeGraph)
                    {
                        using WireMeshBuilder meshBuilder = new WireMeshBuilder();

                        static DebugOverlay.MeshDrawer createOwnerMeshDrawer(WireMeshBuilder meshBuilder)
                        {
                            DebugOverlay.MeshDrawer drawer = DebugOverlay.GetMeshDrawer();
                            drawer.hasMeshOwnership = true;
                            drawer.mesh = meshBuilder.GenerateMesh();
                            return drawer;
                        }

                        if (_path != null)
                        {
                            Vector3 previousPosition = _startPosition;
                            for (int i = 0; i < _path.waypointsCount; i++)
                            {
                                if (nodeGraph.GetNodePosition(_path[i].nodeIndex, out Vector3 currentPosition))
                                {
                                    meshBuilder.AddLine(previousPosition, Color.yellow, currentPosition, Color.yellow);
                                    previousPosition = currentPosition;
                                }
                            }

                            _pathDrawers.Add(createOwnerMeshDrawer(meshBuilder));
                        }

                        meshBuilder.Clear();
                        meshBuilder.AddLine(Vector3.zero, Color.green, Vector3.up, Color.green);
                        meshBuilder.AddLine(Vector3.zero, Color.green, Vector3.forward, Color.green);
                        meshBuilder.AddLine(Vector3.left, Color.green, Vector3.right, Color.green);

                        _pathDrawers.Add(_currentTargetPositionVisualizer = createOwnerMeshDrawer(meshBuilder));

                        meshBuilder.Clear();
                        meshBuilder.AddLine(Vector3.zero, Color.red, Vector3.up, Color.red);
                        meshBuilder.AddLine(Vector3.zero, Color.red, Vector3.forward, Color.red);
                        meshBuilder.AddLine(Vector3.left, Color.red, Vector3.right, Color.red);

                        _pathDrawers.Add(_currentPositionVisualizer = createOwnerMeshDrawer(meshBuilder));

                        _hasCreatedPathVisualizers = true;
                    }
                }
            }
        }
    }
}
