using GooeyArtifacts.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.MovingInteractables
{
    public class InteractableMoveController : MonoBehaviour
    {
        public InteractableSpawnCard SpawnCardServer;

        SyncExternalNetworkedObjectTransform _interactableTransformSyncController;
        public GameObject InteractableObject
        {
            get
            {
                if (!_interactableTransformSyncController)
                    return null;

                return _interactableTransformSyncController.TargetObject;
            }
            set
            {
                if (value)
                {
                    if (!_interactableTransformSyncController)
                    {
                        GameObject syncNetObjectTransformObj = GameObject.Instantiate(Prefabs.SyncExternalNetObjectTransformPrefab);

                        _interactableTransformSyncController = syncNetObjectTransformObj.GetComponent<SyncExternalNetworkedObjectTransform>();
                        _interactableTransformSyncController.TargetObject = value;

                        NetworkServer.Spawn(syncNetObjectTransformObj);
                    }
                    else
                    {
                        _interactableTransformSyncController.TargetObject = value;
                    }
                }
                else
                {
                    if (_interactableTransformSyncController)
                    {
                        NetworkServer.Destroy(_interactableTransformSyncController.gameObject);
                        _interactableTransformSyncController = null;
                    }
                }
            }
        }
    }
}
