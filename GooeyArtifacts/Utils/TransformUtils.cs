using RoR2;
using UnityEngine;

namespace GooeyArtifacts.Utils
{
    public static class TransformUtils
    {
        public static bool IsPartOfEntity(Transform transform, GameObject entity)
        {
            if (!transform || !entity)
                return false;

            if (transform.IsChildOf(entity.transform))
                return true;

            EntityLocator entityLocator = transform.GetComponentInParent<EntityLocator>();
            if (entityLocator && entityLocator.entity && (entityLocator.entity == entity || entityLocator.entity.transform.IsChildOf(entity.transform)))
                return true;

            return false;
        }
    }
}
