using RoR2;
using UnityEngine;

namespace GooeyArtifacts.Utils
{
    public static class WorldUtils
    {
        public static Vector3 GetEnvironmentNormalAtPoint(Vector3 position, float backtrackDistance = 1f)
        {
            return GetEnvironmentNormalAtPoint(position, Vector3.up, backtrackDistance);
        }

        public static Vector3 GetEnvironmentNormalAtPoint(Vector3 position, Vector3 up, float backtrackDistance = 1f)
        {
            if (Physics.Raycast(new Ray(position + (up * backtrackDistance), -up), out RaycastHit hit, backtrackDistance * 1.5f, LayerIndex.world.mask))
            {
                return hit.normal;
            }

            return Vector3.up;
        }
    }
}
