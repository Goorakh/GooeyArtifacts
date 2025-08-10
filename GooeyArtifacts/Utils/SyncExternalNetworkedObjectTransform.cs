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

            [Server]
            set
            {
                if (_targetObject == value)
                    return;

                if (_targetObject)
                {
                    Transform targetTransform = _targetObject.transform;

                    targetTransform.position -= _positionOffset;
                    targetTransform.rotation *= Quaternion.Inverse(_rotationOffset);
                }

                _targetObject = value;

                if (_targetObject)
                {
                    Transform targetTransform = _targetObject.transform;

                    targetTransform.position += _positionOffset;
                    targetTransform.rotation *= _rotationOffset;

                    updateServerObjectTransform();
                }
            }
        }

        Vector3 _positionOffset = Vector3.zero;
        public Vector3 PositionOffset
        {
            get
            {
                return _positionOffset;
            }
            set
            {
                if (_targetObject)
                {
                    _targetObject.transform.position += value - _positionOffset;
                }

                _positionOffset = value;
            }
        }

        Quaternion _rotationOffset = Quaternion.identity;
        public Quaternion RotationOffset
        {
            get
            {
                return _rotationOffset;
            }
            set
            {
                if (_targetObject)
                {
                    _targetObject.transform.rotation *= Quaternion.Inverse(_rotationOffset) * value;
                }

                _rotationOffset = value;
            }
        }

        public bool EnableClientTransformControl;

        new Transform transform;
        NetworkTransform _netTransform;

        Vector3 _targetPositionSmoothVelocity = Vector3.zero;
        Vector4 _targetRotationSmoothVelocity = Vector4.zero;

        void Awake()
        {
            transform = base.transform;
            _netTransform = GetComponent<NetworkTransform>();
        }

        void OnDestroy()
        {
            if (NetworkServer.active)
            {
                TargetObject = null;
            }
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
                updateServerObjectTransform();
            }
            else
            {
                updateClientObjectTransform(true, Time.deltaTime);
            }
        }

        [Server]
        void updateServerObjectTransform()
        {
            if (_targetObject)
            {
                Transform targetTransform = _targetObject.transform;
                transform.SetPositionAndRotation(targetTransform.position - _positionOffset, targetTransform.rotation * Quaternion.Inverse(_rotationOffset));
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

            if (EnableClientTransformControl)
                return;

            Transform targetTransform = _targetObject.transform;

            Vector3 position = targetTransform.position - _positionOffset;
            Quaternion rotation = targetTransform.rotation * Quaternion.Inverse(_rotationOffset);

            if (smooth)
            {
                float smoothTime = _netTransform.sendInterval;

                position = Vector3.SmoothDamp(position, targetPosition, ref _targetPositionSmoothVelocity, smoothTime, float.PositiveInfinity, deltaTime);

                rotation = QuaternionUtil.SmoothDamp(rotation, targetRotation, ref _targetRotationSmoothVelocity, smoothTime, float.PositiveInfinity, deltaTime);
            }
            else
            {
                position = targetPosition;
                rotation = targetRotation;

                _targetPositionSmoothVelocity = Vector3.zero;
                _targetRotationSmoothVelocity = Vector4.zero;
            }

            targetTransform.SetPositionAndRotation(position + _positionOffset, rotation * _rotationOffset);
        }
    }
}
