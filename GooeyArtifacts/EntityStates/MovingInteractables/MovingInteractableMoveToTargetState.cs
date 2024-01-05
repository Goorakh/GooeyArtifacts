using GooeyArtifacts.Utils;
using RoR2;
using RoR2.ConVar;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooeyArtifacts.EntityStates.MovingInteractables
{
    [EntityStateType]
    public class MovingInteractableMoveToTargetState : MovingInteractableBaseState
    {
        static readonly BoolConVar _cvDrawPathData = new BoolConVar("goo_draw_interactable_paths", ConVarFlags.None, "0", "");

        public Vector3 Destination;

        Quaternion _startRotation;

        float _moveSpeed = 10f;

        float _stepHeight = 2f;
        float _maxStepRotation = 35f;

        Path _path;
        PathTraveller _pathTraveller;

        Vector3 _currentPosition;
        Quaternion _currentRotation;

        Vector3 _targetPosition;
        Vector3 _directTargetPosition;
        Quaternion _targetRotation;

        bool _targetAtEnd;

        bool _hasCreatedPathVisualizers;
        readonly List<DebugOverlay.MeshDrawer> _pathDrawers = new List<DebugOverlay.MeshDrawer>();
        DebugOverlay.MeshDrawer _currentPositionVisualizer;
        DebugOverlay.MeshDrawer _currentTargetPositionVisualizer;

        bool _areVisualizersActive;

        public override void OnEnter()
        {
            base.OnEnter();

            _startRotation = transform.rotation;

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

            if (!tryInitializePath())
            {
                outer.SetNextStateToMain();
            }
        }

        bool tryInitializePath()
        {
            SceneInfo sceneInfo = SceneInfo.instance;
            if (!sceneInfo)
                return false;

            NodeGraph nodeGraph = sceneInfo.GetNodeGraph(nodeGraphType);
            if (!nodeGraph)
                return false;

            _path = new Path(nodeGraph);

            NodeGraph.PathRequest pathRequest = new NodeGraph.PathRequest
            {
                startPos = transform.position,
                endPos = Destination,
                hullClassification = hullSize,
                maxJumpHeight = float.PositiveInfinity,
                maxSpeed = float.PositiveInfinity,
                path = _path
            };

            PathTask pathTask = nodeGraph.ComputePath(pathRequest);
            if (!pathTask.wasReachable || pathTask.status != PathTask.TaskStatus.Complete)
                return false;
            
            _pathTraveller = new PathTraveller(_path);

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

            _currentPosition = transform.position;
            _currentRotation = transform.rotation;

            return true;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!transform || _pathTraveller is null)
                return;

            setVisualizersActive(_cvDrawPathData.value);

            float estimatedTimeRemaining;

            float moveDelta = _moveSpeed * Time.fixedDeltaTime;
            if (!_targetAtEnd)
            {
                PathTraveller.TravelData travelData = _pathTraveller.AdvancePosition(moveDelta);

                _directTargetPosition = travelData.CurrentPosition;

                Vector3 groundPosition = _directTargetPosition;
                Vector3 groundNormal = travelData.InterpolatedNormal;

                if (tryFindProperGroundPosition(travelData.CurrentPosition, -travelData.InterpolatedNormal, out Vector3 properGroundNormal, out Vector3 properGroundPosition))
                {
                    groundPosition = properGroundPosition;
                    groundNormal = properGroundNormal;
                }

                _targetPosition = groundPosition;
                _targetAtEnd = travelData.IsAtEnd;
                _targetRotation = Util.QuaternionSafeLookRotation(travelData.Direction, groundNormal);

                estimatedTimeRemaining = travelData.RemainingTotalDistance / _moveSpeed;
            }
            else
            {
                estimatedTimeRemaining = 0f;
            }

            _currentTargetPositionVisualizer?.transform.SetPositionAndRotation(_targetPosition, _targetRotation);

            float stepMagnitude = Mathf.Sqrt(Mathf.Min(1f, Util.Remap(fixedAge, 0f, 1f, 0f, 1f))
                                             * Mathf.Clamp01(Util.Remap(estimatedTimeRemaining, 0f, 2.5f, 0f, 1f)));

            float stepValue = 4f * Mathf.PI * fixedAge;

            _currentPosition = Vector3.MoveTowards(_currentPosition, _targetPosition, moveDelta + (1f * Time.fixedDeltaTime));
            _currentRotation = Quaternion.RotateTowards(_currentRotation, _targetRotation, 135f * Time.fixedDeltaTime);

            _currentPositionVisualizer?.transform.SetPositionAndRotation(_currentPosition, _currentRotation);

            transform.position = _currentPosition + (_currentRotation * new Vector3(0f, Mathf.Abs(Mathf.Sin(stepValue)) * _stepHeight * stepMagnitude, 0f));
            transform.rotation = Quaternion.AngleAxis(Mathf.Sin(stepValue + (Mathf.PI / 2f)) * _maxStepRotation * stepMagnitude, _currentRotation * Vector3.forward) * _currentRotation;

            if (_targetAtEnd && _currentPosition == _targetPosition)
            {
                Vector3 targetEuler = _startRotation.eulerAngles;
                targetEuler.y = _currentRotation.eulerAngles.y + UnityEngine.Random.Range(-15f, 15f);

                outer.SetNextState(new MovingInteractableSettleState
                {
                    TargetRotation = Quaternion.Euler(targetEuler),
                    TargetPosition = _directTargetPosition
                });
            }
        }

        public override void OnExit()
        {
            base.OnExit();

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

            bool isValidCollision(Transform otherTransform)
            {
                if (otherTransform.IsChildOf(transform))
                    return false;

                EntityLocator entityLocator = otherTransform.GetComponentInParent<EntityLocator>();
                if (entityLocator && entityLocator.entity && !isValidCollision(entityLocator.entity.transform))
                    return false;

                return true;
            }

            bool sphereCast(Ray ray, float maxDistance, out RaycastHit hit)
            {
                RaycastHit[] hits = Physics.SphereCastAll(ray, radius, maxDistance, LayerIndex.world.mask);

                if (hits.Length == 0)
                {
                    hit = default;
                    return false;
                }

                float closestHitDistance = float.PositiveInfinity;
                RaycastHit closestHit = default;

                foreach (RaycastHit hitCandidate in hits)
                {
                    // UnityDocs: For colliders that overlap the sphere at the start of the sweep, RaycastHit.normal is set opposite to the direction of the sweep, RaycastHit.distance is set to zero, and the zero vector gets returned in RaycastHit.point
                    if (hitCandidate.distance <= 0f)
                        continue;

                    if (hitCandidate.distance >= closestHitDistance)
                        continue;

                    if (!isValidCollision(hitCandidate.transform))
                        continue;

                    closestHit = hitCandidate;
                    closestHitDistance = hitCandidate.distance;
                }

                hit = closestHit;
                return !float.IsInfinity(closestHitDistance);
            }

            RaycastHit hit;

            const float SEARCH_INCREMENT = 0.5f;

            for (int i = 1; i <= 50; i++)
            {
                if (sphereCast(new Ray(position + (-down * (i * SEARCH_INCREMENT)), down), SEARCH_INCREMENT * 1.05f, out hit))
                {
                    normal = hit.normal;
                    properPosition = hit.point;
                    return true;
                }
            }

            if (sphereCast(new Ray(position + (-down * radius), down), radius * 3.5f, out hit))
            {
                normal = hit.normal;
                properPosition = hit.point;
                return true;
            }

            normal = -down;
            properPosition = position;
            return false;
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

                        Vector3 previousPosition = _currentPosition;
                        for (int i = 0; i < _path.waypointsCount; i++)
                        {
                            if (nodeGraph.GetNodePosition(_path[i].nodeIndex, out Vector3 position))
                            {
                                meshBuilder.AddLine(previousPosition, Color.yellow, position, Color.yellow);
                                previousPosition = position;
                            }
                        }

                        _pathDrawers.Add(createOwnerMeshDrawer(meshBuilder));

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
