using GooeyArtifacts.Utils;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.MovingInteractables
{
    public class InteractableMoveController : NetworkBehaviour
    {
        [NonSerialized]
        public SpawnCard SpawnCardServer;

        [SyncVar]
        GameObject _transformSyncControllerObject;

        bool _markedForCleanup;

        EntityStateMachine _stateMachine;

        MemoizedGetComponent<SyncExternalNetworkedObjectTransform> _transformSyncController;

        public SyncExternalNetworkedObjectTransform TransformSyncController
        {
            get
            {
                return _transformSyncController.Get(_transformSyncControllerObject);
            }

            [Server]
            set
            {
                _transformSyncControllerObject = value ? value.gameObject : null;
            }
        }

        public GameObject InteractableObject
        {
            get
            {
                SyncExternalNetworkedObjectTransform transformSyncController = TransformSyncController;
                return transformSyncController ? transformSyncController.TargetObject : null;
            }
        }

        void Awake()
        {
            _stateMachine = GetComponent<EntityStateMachine>();
        }

        [Server]
        public void MarkForCleanup()
        {
            _markedForCleanup = true;
        }

        [Server]
        public void UnmarkForCleanup()
        {
            _markedForCleanup = false;
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                if (_markedForCleanup && _stateMachine && _stateMachine.IsInMainState())
                {
                    NetworkServer.Destroy(_transformSyncControllerObject);
                    NetworkServer.Destroy(gameObject);
                    _markedForCleanup = false;
                }
            }
        }
    }
}
