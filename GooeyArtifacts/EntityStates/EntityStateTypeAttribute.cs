using System;
using System.Collections.Generic;
using System.Linq;

namespace GooeyArtifacts.EntityStates
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class EntityStateTypeAttribute : HG.Reflection.SearchableAttribute
    {
        public new Type target => base.target as Type;

        public static IEnumerable<Type> GetAllEntityStateTypes()
        {
            return GetInstances<EntityStateTypeAttribute>().Cast<EntityStateTypeAttribute>()
                                                           .Select(a => a.target);
        }
    }
}
