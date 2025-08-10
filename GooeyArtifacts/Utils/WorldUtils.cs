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

        public static Vector3 GetEnvironmentNormalAtPoint(Vector3 position, GameObject entity, float backtrackDistance = 1f)
        {
            return GetEnvironmentNormalAtPoint(position, Vector3.up, entity, backtrackDistance);
        }

        public static Vector3 GetEnvironmentNormalAtPoint(Vector3 position, Vector3 up, GameObject entity, float backtrackDistance = 1f)
        {
            if (!entity)
            {
                return GetEnvironmentNormalAtPoint(position, up, backtrackDistance);
            }

            int hitCount = HGPhysics.RaycastAll(out RaycastHit[] hits, position + (up * backtrackDistance), -up, backtrackDistance * 1.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);

            try
            {
                RaycastHit closestHit = default;
                float closestHitSqrDistance = float.PositiveInfinity;

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hitCandidate = hits[i];

                    // Not using hit.distance here since we don't care about the distance from the ray origin,
                    // we only care about how far away the hit point is from the desired position
                    float hitSqrDistance = (hitCandidate.point - position).sqrMagnitude;
                    if (hitSqrDistance >= closestHitSqrDistance)
                        continue;

                    if (TransformUtils.IsPartOfEntity(hitCandidate.transform, entity))
                        continue;

                    closestHit = hitCandidate;
                    closestHitSqrDistance = hitSqrDistance;
                }

                if (!float.IsFinite(closestHitSqrDistance))
                {
                    return Vector3.up;
                }

                return closestHit.normal;
            }
            finally
            {
                HGPhysics.ReturnResults(hits);
            }
        }
    }
}
