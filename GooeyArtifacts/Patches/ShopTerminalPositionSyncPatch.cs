using GooeyArtifacts.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Patches
{
    static class ShopTerminalPositionSyncPatch
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.ShopTerminalBehavior.Start += ShopTerminalBehavior_Start;
        }

        static void ShopTerminalBehavior_Start(On.RoR2.ShopTerminalBehavior.orig_Start orig, ShopTerminalBehavior self)
        {
            orig(self);

            if (NetworkServer.active)
            {
                self.gameObject.AddComponent<ShopTerminalOffsetMaintainer>();

                GameObject syncNetObjectTransformObj = GameObject.Instantiate(Prefabs.SyncExternalNetObjectTransformPrefab);

                SyncExternalNetworkedObjectTransform syncExternalTransform = syncNetObjectTransformObj.GetComponent<SyncExternalNetworkedObjectTransform>();
                syncExternalTransform.TargetObject = self.gameObject;

                NetworkServer.Spawn(syncNetObjectTransformObj);
            }
        }

        class ShopTerminalOffsetMaintainer : MonoBehaviour
        {
            ShopTerminalBehavior _terminalBehavior;

            Matrix4x4 _terminalLocalTransform = Matrix4x4.identity;

            bool _isInitialized;

            void Awake()
            {
                _terminalBehavior = GetComponent<ShopTerminalBehavior>();
            }

            void FixedUpdate()
            {
                if (!_terminalBehavior)
                {
                    Destroy(this);
                    return;
                }

                MultiShopController multiShopController = _terminalBehavior.serverMultiShopController;
                if (!_isInitialized)
                {
                    if (multiShopController)
                    {
                        _terminalLocalTransform = multiShopController.transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

                        _isInitialized = true;
                    }
                }
                else if (!multiShopController)
                {
                    _isInitialized = false;
                }
                else
                {
                    Matrix4x4 matrix = multiShopController.transform.localToWorldMatrix * _terminalLocalTransform;
                    transform.SetPositionAndRotation(matrix.GetColumn(3), matrix.rotation);
                }
            }
        }
    }
}
