using System.Collections.Generic;
using UnityEngine;

using Path = RoR2.Path;

namespace GooeyArtifacts.Utils.Extensions
{
    public static class PathExtensions
    {
        public static void RemoveDuplicateWaypoints(this Path path)
        {
            // The array layout that Path uses for its waypoints baffles me,
            // it fills the array back to front making iterating
            // and resizing more complicated for seemingly no reason.
            // So this is now making a new list every call because I CBA dealing with this, cry about it.

            List<Path.Waypoint> waypoints = [];
            path.WriteWaypointsToList(waypoints);

            int removedWaypoints = 0;
            for (int i = waypoints.Count - 1; i > 0; i--)
            {
                Path.Waypoint prevWaypoint = waypoints[i - 1];

                while (i < waypoints.Count && prevWaypoint.Equals(waypoints[i]))
                {
                    waypoints.RemoveAt(i);
                    removedWaypoints++;
                }
            }

            if (removedWaypoints > 0)
            {
                path.Clear();

                for (int i = waypoints.Count - 1; i >= 0; i--)
                {
                    Path.Waypoint waypoint = waypoints[i];

                    path.PushWaypointToFront(waypoint.nodeIndex, waypoint.minJumpHeight);
                }
            }
        }

        public static float CalculateTotalDistance(this Path path)
        {
            float totalDistance = 0f;
            for (int i = 0; i < path.waypointsCount - 1; i++)
            {
                if (!path.nodeGraph.GetNodePosition(path[i].nodeIndex, out Vector3 nodePositionA))
                {
                    Log.Error($"Failed to get node position at index {i} in path");
                    continue;
                }

                if (!path.nodeGraph.GetNodePosition(path[i + 1].nodeIndex, out Vector3 nodePositionB))
                {
                    Log.Error($"Failed to get node position at index {i + 1} in path");
                    continue;
                }

                totalDistance += Vector3.Distance(nodePositionA, nodePositionB);
            }

            return totalDistance;
        }
    }
}
