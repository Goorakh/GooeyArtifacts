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

            if (NetworkServer.active)
            {
                ServerLockObjectTracker lockObjectTracker = self.gameObject.EnsureComponent<ServerLockObjectTracker>();
                lockObjectTracker.SetLockObject(self.lockGameObject);
            }
        }

        class ServerLockObjectTracker : MonoBehaviour
        {
            SyncExternalTransformPseudoParent _pseudoParentController;

            public void SetLockObject(GameObject lockObject)
            {
                bool hadLockOnObject = _pseudoParentController && _pseudoParentController.TargetObject == lockObject;
                bool shouldHaveLockOnObject = lockObject;

                if (hadLockOnObject != shouldHaveLockOnObject)
                {
                    if (shouldHaveLockOnObject)
                    {
                        if (!_pseudoParentController)
                        {
                            lockObject.transform.GetPositionAndRotation(out Vector3 lockPosition, out Quaternion lockRotation);
                            Vector3 lockLocalPosition = transform.InverseTransformPoint(lockPosition);
                            Quaternion lockLocalRotation = Quaternion.Inverse(transform.rotation) * lockRotation;

                            GameObject pseudoParentControllerObj = Instantiate(Prefabs.SyncExternalNetObjectPseudoParentPrefab, lockLocalPosition, lockLocalRotation);
                            _pseudoParentController = pseudoParentControllerObj.GetComponent<SyncExternalTransformPseudoParent>();
                            _pseudoParentController.Parent = gameObject;
                            NetworkServer.Spawn(pseudoParentControllerObj);
                        }

                        _pseudoParentController.TargetObject = lockObject;
                    }
                    else
                    {
                        if (_pseudoParentController)
                        {
                            NetworkServer.Destroy(_pseudoParentController.gameObject);
                        }
                    }
                }
            }
        }
    }
}
