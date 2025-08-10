using RoR2;
using UnityEngine;

namespace GooeyArtifacts.Utils
{
    public class PathTraveller
    {
        public readonly Path Path;

        int _lastWaypointIndex;
        float _currentWaypointDistanceTravelled;

        readonly PathLinkData[] _pathLinks = [];

        public PathTraveller(Path path, GameObject travellingEntity)
        {
            Path = path;

            if (Path.waypointsCount > 0)
            {
                _pathLinks = new PathLinkData[Path.waypointsCount - 1];

                PathNode endNode = new PathNode(Path, Path[Path.waypointsCount - 1], travellingEntity);

                float totalPathDistance = 0f;
                for (int i = Path.waypointsCount - 2; i >= 0; i--)
                {
                    PathNode current = new PathNode(Path, Path[i], travellingEntity);

                    PathLinkData link = new PathLinkData(current, endNode, totalPathDistance);
                    _pathLinks[i] = link;

                    totalPathDistance += link.TotalDistance;

                    endNode = current;
                }
            }
        }

        public TravelData AdvancePosition(float moveDelta)
        {
            _currentWaypointDistanceTravelled += moveDelta;

            for (; _lastWaypointIndex < Path.waypointsCount - 1; _lastWaypointIndex++)
            {
                PathLinkData linkData = _pathLinks[_lastWaypointIndex];

                if (_currentWaypointDistanceTravelled >= linkData.TotalDistance)
                {
                    _currentWaypointDistanceTravelled -= linkData.TotalDistance;
                    continue;
                }

                float travelFraction = Mathf.Clamp01(_currentWaypointDistanceTravelled / linkData.TotalDistance);

                return new TravelData(Path, linkData.Start, linkData.End, travelFraction, linkData.RemainingTotalDistance - _currentWaypointDistanceTravelled, false);
            }

            PathLinkData lastLink = _pathLinks[^1];
            return new TravelData(Path, lastLink.Start, lastLink.End, 1f, 0f, true);
        }

        public readonly struct TravelData
        {
            public readonly Vector3 CurrentPosition;

            public readonly Vector3 Direction;
            public readonly Vector3 InterpolatedNormal;

            public readonly float RemainingTotalDistance;

            public readonly bool IsAtEnd;

            public TravelData(Path path, PathNode start, PathNode end, float travelFraction, float remainingTotalDistance, bool isAtEnd)
            {
                CurrentPosition = Vector3.Lerp(start.Position, end.Position, travelFraction);

                Direction = (end.Position - start.Position).normalized;

                InterpolatedNormal = Quaternion.Slerp(Util.QuaternionSafeLookRotation(start.Normal), Util.QuaternionSafeLookRotation(end.Normal), travelFraction) * Vector3.forward;

                RemainingTotalDistance = remainingTotalDistance;

                IsAtEnd = isAtEnd;
            }
        }

        public readonly struct PathNode
        {
            public readonly Vector3 Position;
            public readonly Vector3 Normal;

            public readonly float MinJumpHeight;

            public PathNode(Path path, Path.Waypoint waypoint, GameObject entity)
            {
                MinJumpHeight = waypoint.minJumpHeight;

                if (!path.nodeGraph.GetNodePosition(waypoint.nodeIndex, out Position))
                {
                    Log.Error("Failed to find node position");
                }
                else
                {
                    Normal = WorldUtils.GetEnvironmentNormalAtPoint(Position, entity);
                }
            }
        }

        readonly struct PathLinkData
        {
            public readonly PathNode Start;
            public readonly PathNode End;

            public readonly float TotalDistance;

            public readonly float RemainingTotalDistance;

            public PathLinkData(PathNode start, PathNode end, float remainingTotalDistance)
            {
                Start = start;
                End = end;

                TotalDistance = (End.Position - Start.Position).magnitude;

                RemainingTotalDistance = remainingTotalDistance + TotalDistance;
            }
        }
    }
}
