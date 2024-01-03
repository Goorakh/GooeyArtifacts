using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Utils
{
    public class SyncExternalNetworkedObjectTransform : NetworkBehaviour
    {
        NetworkInstanceId _targetObjectNetId;
        GameObject _targetObject;

        const uint TARGET_OBJECT_DIRTY_BIT = 1 << 0;

        public GameObject TargetObject
        {
            get
            {
                return _targetObject;
            }
            set
            {
                SetSyncVarGameObject(value, ref _targetObject, TARGET_OBJECT_DIRTY_BIT, ref _targetObjectNetId);

                if (value)
                {
                    transform.position = value.transform.position;
                    transform.rotation = value.transform.rotation;
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

        public override void PreStartClient()
        {
            if (!_targetObjectNetId.IsEmpty())
            {
                _targetObject = ClientScene.FindLocalObject(_targetObjectNetId);
                updateClientObjectTransform();
            }
        }

        void Update()
        {
            if (hasAuthority)
            {
                if (_targetObject && _targetObject.transform.hasChanged)
                {
                    transform.SetPositionAndRotation(_targetObject.transform.position, _targetObject.transform.rotation);
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

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(_targetObject);

                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;

            if ((dirtyBits & TARGET_OBJECT_DIRTY_BIT) != 0)
            {
                writer.Write(_targetObject);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _targetObjectNetId = reader.ReadNetworkId();
                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();
            if ((dirtyBits & TARGET_OBJECT_DIRTY_BIT) != 0)
            {
                _targetObject = reader.ReadGameObject();
            }
        }
    }
}
