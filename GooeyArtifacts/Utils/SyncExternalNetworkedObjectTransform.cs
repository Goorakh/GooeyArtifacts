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

        void Awake()
        {
            transform = base.transform;
            _netTransform = GetComponent<NetworkTransform>();
        }

        void OnEnable()
        {
            if (_netTransform)
            {
                _netTransform.clientMoveCallback3D = clientMoveCallback;
            }

            updateTargetObjectTransform();
        }

        void OnDisable()
        {
            if (_netTransform)
            {
                _netTransform.clientMoveCallback3D = null;
            }
        }

        public override void PreStartClient()
        {
            if (!_targetObjectNetId.IsEmpty())
            {
                _targetObject = ClientScene.FindLocalObject(_targetObjectNetId);
                updateTargetObjectTransform();
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
                updateTargetObjectTransform();
            }
        }

        bool clientMoveCallback(ref Vector3 position, ref Vector3 velocity, ref Quaternion rotation)
        {
            updateTargetObjectTransform(position, rotation);
            return true;
        }

        void updateTargetObjectTransform()
        {
            updateTargetObjectTransform(transform.position, transform.rotation);
        }

        void updateTargetObjectTransform(Vector3 position, Quaternion rotation)
        {
            if (_targetObject)
            {
                _targetObject.transform.SetPositionAndRotation(position, rotation);
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
