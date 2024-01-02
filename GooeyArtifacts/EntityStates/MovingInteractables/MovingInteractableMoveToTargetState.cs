using GooeyArtifacts.Utils;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace GooeyArtifacts.EntityStates.MovingInteractables
{
    [EntityStateType]
    public class MovingInteractableMoveToTargetState : MovingInteractableBaseState
    {
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
        Quaternion _targetRotation;

        bool _targetAtEnd;

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

            if (SceneInfo.instance)
            {
                NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(nodeGraphType);
                if (nodeGraph)
                {
                    HullDef hullDef = HullDef.Find(hullSize);

                    _path = new Path(nodeGraph);

                    NodeGraph.PathRequest pathRequest = new NodeGraph.PathRequest
                    {
                        startPos = transform.position,
                        endPos = Destination,
                        hullClassification = hullSize,
                        maxJumpHeight = hullDef.height,
                        maxSpeed = hullDef.radius * 3f,
                        path = _path
                    };

                    PathTask pathTask = nodeGraph.ComputePath(pathRequest);
                    if (pathTask.wasReachable && pathTask.status == PathTask.TaskStatus.Complete)
                    {
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
                        return;
                    }
                }
            }

            outer.SetNextStateToMain();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!transform || _pathTraveller is null)
                return;

            float estimatedTimeRemaining;

            float moveDelta = _moveSpeed * Time.fixedDeltaTime;
            if (!_targetAtEnd)
            {
                PathTraveller.TravelData travelData = _pathTraveller.AdvancePosition(moveDelta);

                _targetPosition = travelData.CurrentPosition;
                _targetAtEnd = travelData.IsAtEnd;
                _targetRotation = Util.QuaternionSafeLookRotation(travelData.Direction, travelData.InterpolatedNormal);

                estimatedTimeRemaining = travelData.RemainingTotalDistance / _moveSpeed;
            }
            else
            {
                estimatedTimeRemaining = 0f;
            }

            _currentPosition = Vector3.MoveTowards(_currentPosition, _targetPosition, moveDelta);

            float stepMagnitude = 1f;
            stepMagnitude *= Mathf.Min(1f, Util.Remap(fixedAge, 0f, 2f, 0f, 1f));
            stepMagnitude *= Mathf.Clamp01(Util.Remap(estimatedTimeRemaining, 0f, 2.5f, 0f, 1f));
            stepMagnitude = Mathf.Sqrt(stepMagnitude);

            float stepValue = 4f * Mathf.PI * fixedAge;
            transform.position = _currentPosition + new Vector3(0f, Mathf.Abs(Mathf.Sin(stepValue)) * _stepHeight * stepMagnitude, 0f);

            _currentRotation = Quaternion.RotateTowards(_currentRotation, _targetRotation, 135f * Time.fixedDeltaTime);
            transform.rotation = Quaternion.AngleAxis(Mathf.Sin(stepValue + (Mathf.PI / 2f)) * _maxStepRotation * stepMagnitude, _currentRotation * Vector3.forward) * _currentRotation;

            if (_targetAtEnd && _currentPosition == _targetPosition)
            {
                Vector3 targetEuler = _startRotation.eulerAngles;
                targetEuler.y = _currentRotation.eulerAngles.y + UnityEngine.Random.Range(-15f, 15f);

                outer.SetNextState(new MovingInteractableSettleState
                {
                    TargetRotation = Quaternion.Euler(targetEuler),
                    TargetPosition = _targetPosition
                });
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            _path?.Dispose();
            _path = null;
        }
    }
}
