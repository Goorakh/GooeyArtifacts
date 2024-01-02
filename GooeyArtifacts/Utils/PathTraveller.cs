using RoR2;
using RoR2.Navigation;
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

        public Vector3 AdvancePosition(float moveDelta, out TravelData travelData, out bool isAtEnd)
        {
            TravelData getEndTravelData(out Vector3 endNodePosition)
            {
                Path.nodeGraph.GetNodePosition(Path[Path.waypointsCount - 1].nodeIndex, out endNodePosition);
                Path.nodeGraph.GetNodePosition(Path[Path.waypointsCount - 2].nodeIndex, out Vector3 prevNodePosition);

                return new TravelData((endNodePosition - prevNodePosition).normalized, Vector3.up, 0f);
            }

            if (_lastWaypointIndex >= Path.waypointsCount - 1)
            {
                travelData = getEndTravelData(out Vector3 nodePosition);

                isAtEnd = true;
                return nodePosition;
            }

            _currentWaypointDistanceTravelled += moveDelta;

            WaypointTraversalData getCurrentWaypointData()
            {
                Path.Waypoint prevWaypoint = Path[_lastWaypointIndex];
                Path.Waypoint nextWaypoint = Path[_lastWaypointIndex + 1];

                return new WaypointTraversalData(Path, prevWaypoint, nextWaypoint);
            }

            WaypointTraversalData waypointData = getCurrentWaypointData();

            while (_currentWaypointDistanceTravelled >= waypointData.Distance)
            {
                _currentWaypointDistanceTravelled -= waypointData.Distance;
                _lastWaypointIndex++;

                if (_lastWaypointIndex >= Path.waypointsCount - 1)
                {
                    travelData = getEndTravelData(out Vector3 nodePosition);

                    isAtEnd = true;
                    return nodePosition;
                }

                waypointData = getCurrentWaypointData();
            }

            float travelFraction = Mathf.Clamp01(_currentWaypointDistanceTravelled / waypointData.Distance);

            Vector3 startNormal = WorldUtils.GetEnvironmentNormalAtPoint(waypointData.StartPosition);
            Vector3 endNormal = WorldUtils.GetEnvironmentNormalAtPoint(waypointData.EndPosition);

            Vector3 normal = Quaternion.Slerp(Quaternion.LookRotation(startNormal), Quaternion.LookRotation(endNormal), travelFraction) * Vector3.forward;

            float totalDistanceRemaining = waypointData.Distance - _currentWaypointDistanceTravelled;
            for (int i = _lastWaypointIndex + 2; i < Path.waypointsCount; i++)
            {
                NodeGraph.NodeIndex prevWaypointNode = Path[i - 1].nodeIndex;
                NodeGraph.NodeIndex nextWaypointNode = Path[i].nodeIndex;

                Path.nodeGraph.GetNodePosition(prevWaypointNode, out Vector3 prevWaypointPosition);
                Path.nodeGraph.GetNodePosition(nextWaypointNode, out Vector3 nextWaypointPosition);

                totalDistanceRemaining += Vector3.Distance(prevWaypointPosition, nextWaypointPosition);
            }

            travelData = new TravelData(waypointData.TravelDirection, normal, totalDistanceRemaining);
            isAtEnd = false;
            return Vector3.Lerp(waypointData.StartPosition, waypointData.EndPosition, travelFraction);
        }

        public readonly struct TravelData
        {
            public readonly Vector3 Direction;
            public readonly Vector3 InterpolatedNormal;

            public readonly float RemainingTotalDistance;

            public TravelData(Vector3 direction, Vector3 interpolatedNormal, float remainingTotalDistance)
            {
                Direction = direction;
                InterpolatedNormal = interpolatedNormal;

                RemainingTotalDistance = remainingTotalDistance;
            }
        }

        readonly struct WaypointTraversalData
        {
            public readonly Path.Waypoint Start;
            public readonly Path.Waypoint End;

            public readonly Vector3 StartPosition;
            public readonly Vector3 EndPosition;

            public readonly Vector3 TravelDirection;

            public readonly float Distance;

            public WaypointTraversalData(Path path, Path.Waypoint start, Path.Waypoint end)
            {
                Start = start;
                path.nodeGraph.GetNodePosition(start.nodeIndex, out StartPosition);

                End = end;
                path.nodeGraph.GetNodePosition(end.nodeIndex, out EndPosition);

                Vector3 positionDiff = EndPosition - StartPosition;
                Distance = positionDiff.magnitude;
                TravelDirection = positionDiff / Distance;
            }
        }
    }
}
