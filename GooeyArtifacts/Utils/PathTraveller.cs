using RoR2;
using UnityEngine;

namespace GooeyArtifacts.Utils
{
    public class PathTraveller
    {
        public readonly Path Path;

        int _lastWaypointIndex;
        float _currentWaypointDistanceTravelled;

        public PathTraveller(Path path)
        {
            Path = path;
        }

        // TODO: Learn math (follow jump arcs for jump nodes instead of just interpolating like normal)
        public TravelData AdvancePosition(float moveDelta)
        {
            _currentWaypointDistanceTravelled += moveDelta;

            for (; _lastWaypointIndex < Path.waypointsCount - 1; _lastWaypointIndex++)
            {
                WaypointTraversalData waypointData = new WaypointTraversalData(Path, _lastWaypointIndex);

                if (_currentWaypointDistanceTravelled >= waypointData.TotalDistance)
                {
                    _currentWaypointDistanceTravelled -= waypointData.TotalDistance;
                    continue;
                }

                float totalDistanceRemaining = waypointData.TotalDistance - _currentWaypointDistanceTravelled;
                for (int i = _lastWaypointIndex + 2; i < Path.waypointsCount; i++)
                {
                    WaypointTraversalData traversalData = new WaypointTraversalData(Path, i - 1);
                    totalDistanceRemaining += traversalData.TotalDistance;
                }

                float travelFraction = Mathf.Clamp01(_currentWaypointDistanceTravelled / waypointData.TotalDistance);

                return new TravelData(Path, waypointData.Start, waypointData.End, travelFraction, totalDistanceRemaining, false);
            }

            return new TravelData(Path, Path[Path.waypointsCount - 2], Path[Path.waypointsCount - 1], 1f, 0f, true);
        }

        public readonly struct TravelData
        {
            public readonly Vector3 CurrentPosition;

            public readonly Vector3 Direction;
            public readonly Vector3 InterpolatedNormal;

            public readonly float RemainingTotalDistance;

            public readonly bool IsAtEnd;

            public TravelData(Path path, Path.Waypoint start, Path.Waypoint end, float travelFraction, float remainingTotalDistance, bool isAtEnd)
            {
                path.nodeGraph.GetNodePosition(start.nodeIndex, out Vector3 startPosition);
                path.nodeGraph.GetNodePosition(end.nodeIndex, out Vector3 endPosition);

                CurrentPosition = Vector3.Lerp(startPosition, endPosition, travelFraction);

                Direction = (endPosition - startPosition).normalized;

                Vector3 startNormal = WorldUtils.GetEnvironmentNormalAtPoint(startPosition);
                Vector3 endNormal = WorldUtils.GetEnvironmentNormalAtPoint(endPosition);

                InterpolatedNormal = Quaternion.Slerp(Util.QuaternionSafeLookRotation(startNormal), Util.QuaternionSafeLookRotation(endNormal), travelFraction) * Vector3.forward;

                RemainingTotalDistance = remainingTotalDistance;

                IsAtEnd = isAtEnd;
            }
        }

        readonly struct WaypointTraversalData
        {
            public readonly Path.Waypoint Start;
            public readonly Path.Waypoint End;

            public readonly Vector3 StartPosition;
            public readonly Vector3 EndPosition;

            public readonly float TotalDistance;

            public WaypointTraversalData(Path path, int startIndex)
            {
                Start = path[startIndex];
                path.nodeGraph.GetNodePosition(Start.nodeIndex, out StartPosition);

                End = path[startIndex + 1];
                path.nodeGraph.GetNodePosition(End.nodeIndex, out EndPosition);

                TotalDistance = (EndPosition - StartPosition).magnitude;
            }
        }
    }
}
