using GooeyArtifacts.ThirdParty.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Utils
{
    public class SyncExternalNetworkedObjectTransform : NetworkBehaviour
    {
        [SyncVar]
        GameObject _targetObject;
        public GameObject TargetObject
        {
            get
            {
                return _targetObject;
            }
            set
            {
                if (_targetObject == value)
                    return;

                _targetObject = value;

                if (_targetObject)
                {
                    Transform targetTransform = _targetObject.transform;
                    transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);

                    updateClientObjectTransform();
                }
            }
        }

        new Transform transform;
        NetworkTransform _netTransform;

        Vector3 _targetPositionSmoothVelocity;
        Quaternion _targetRotationSmoothVelocity;

        void Awake()
        {
            transform = base.transform;
            _netTransform = GetComponent<NetworkTransform>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            updateClientObjectTransform();
        }

        void Update()
        {
            if (NetworkServer.active)
            {
                if (_targetObject)
                {
                    Transform targetTransform = _targetObject.transform;
                    transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
                }
            }
            else
            {
                updateClientObjectTransform(true, Time.deltaTime);
            }
        }

        void updateClientObjectTransform()
        {
            updateClientObjectTransform(false, 0f);
        }

        void updateClientObjectTransform(bool smooth, float deltaTime)
        {
            updateClientObjectTransform(transform.position, transform.rotation, smooth, deltaTime);
        }

        void updateClientObjectTransform(Vector3 targetPosition, Quaternion targetRotation, bool smooth, float deltaTime)
        {
            if (!_targetObject)
                return;

            Transform targetTransform = _targetObject.transform;

            if (smooth)
            {
                float smoothTime = _netTransform.sendInterval;

                targetTransform.position = Vector3.SmoothDamp(targetTransform.position, targetPosition, ref _targetPositionSmoothVelocity, smoothTime, float.PositiveInfinity, deltaTime);

                targetTransform.rotation = QuaternionUtil.SmoothDamp(targetTransform.rotation, targetRotation, ref _targetRotationSmoothVelocity, smoothTime, float.PositiveInfinity, deltaTime);
            }
            else
            {
                targetTransform.SetPositionAndRotation(targetPosition, targetRotation);
                _targetPositionSmoothVelocity = Vector3.zero;
                _targetRotationSmoothVelocity = Quaternion.identity;
            }
        }
    }
}
