using GooeyArtifacts.Utils;
using HarmonyLib;
using HG;
using MonoMod.RuntimeDetour;
using RoR2;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Patches
{
    static class PurchaseInteractionLockPositionSyncPatch
    {
        [SystemInitializer]
        static void Init()
        {
            MethodInfo lockObjectSetter = AccessTools.DeclaredPropertySetter(typeof(PurchaseInteraction), nameof(PurchaseInteraction.NetworklockGameObject));
            if (lockObjectSetter is not null)
            {
                new Hook(lockObjectSetter, PurchaseInteraction_set_NetworklockGameObject);
            }
            else
            {
                Log.Error("Unable to find lock object setter method");
            }
        }

        delegate void orig_set_NetworklockGameObject(PurchaseInteraction self, GameObject value);

        static void PurchaseInteraction_set_NetworklockGameObject(orig_set_NetworklockGameObject orig, PurchaseInteraction self, GameObject value)
        {
            orig(self, value);

            if (!NetworkServer.active)
                return;

            if (value)
            {
                LockObjectLocalPositionMaintainer lockPositionMaintainer = self.gameObject.EnsureComponent<LockObjectLocalPositionMaintainer>();
                lockPositionMaintainer.LockObject = value;
            }
        }

        class LockObjectLocalPositionMaintainer : MonoBehaviour
        {
            Matrix4x4 _lockObjectLocalTransform = Matrix4x4.identity;

            SyncExternalNetworkedObjectTransform _lockObjectTransformSyncController;
            public GameObject LockObject
            {
                get
                {
                    if (!_lockObjectTransformSyncController)
                        return null;

                    return _lockObjectTransformSyncController.TargetObject;
                }
                set
                {
                    if (value)
                    {
                        Transform lockTransform = value.transform;
                        _lockObjectLocalTransform = transform.worldToLocalMatrix * Matrix4x4.TRS(lockTransform.position, lockTransform.rotation, Vector3.one);

                        if (!_lockObjectTransformSyncController)
                        {
                            GameObject syncNetObjectTransformObj = GameObject.Instantiate(Prefabs.SyncExternalNetObjectTransformPrefab);
                            _lockObjectTransformSyncController = syncNetObjectTransformObj.GetComponent<SyncExternalNetworkedObjectTransform>();

                            _lockObjectTransformSyncController.TargetObject = value;

                            NetworkServer.Spawn(syncNetObjectTransformObj);
                        }
                        else
                        {
                            _lockObjectTransformSyncController.TargetObject = value;
                        }
                    }
                    else
                    {
                        _lockObjectLocalTransform = Matrix4x4.identity;

                        if (_lockObjectTransformSyncController)
                        {
                            NetworkServer.Destroy(_lockObjectTransformSyncController.gameObject);
                            _lockObjectTransformSyncController = null;
                        }
                    }
                }
            }

            void FixedUpdate()
            {
                GameObject lockObject = LockObject;
                if (lockObject)
                {
                    Matrix4x4 lockMatrix = transform.localToWorldMatrix * _lockObjectLocalTransform;
                    lockObject.transform.SetPositionAndRotation(lockMatrix.GetColumn(3), lockMatrix.rotation);
                }
            }
        }
    }
}
