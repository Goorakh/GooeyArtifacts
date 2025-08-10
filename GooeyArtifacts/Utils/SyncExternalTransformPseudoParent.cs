using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Utils
{
    public class SyncExternalTransformPseudoParent : NetworkBehaviour
    {
        [SyncVar]
        public GameObject TargetObject;

        [SyncVar]
        public GameObject Parent;

        new Transform transform;

        void Awake()
        {
            transform = base.transform;
        }

        void LateUpdate()
        {
            if (TargetObject && Parent)
            {
                Transform targetTransform = TargetObject.transform;
                Transform parentTransform = Parent.transform;

                transform.GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation);

                Vector3 position = parentTransform.TransformPoint(localPosition);
                Quaternion rotation = parentTransform.rotation * localRotation;

                targetTransform.transform.SetPositionAndRotation(position, rotation);
            }
        }
    }
}
