using GooeyArtifacts.Utils;
using HG;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
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
            HashSet<MethodInfo> patchedSetLockObjectMethods = [];

            foreach (GameObject networkedPrefab in ContentManager.networkedObjectPrefabs)
            {
                foreach (IInteractableLockable lockableInteractable in networkedPrefab.GetComponents<IInteractableLockable>())
                {
                    InterfaceMapping interfaceMapping = lockableInteractable.GetType().GetInterfaceMap(typeof(IInteractableLockable));
                    int methodIndex = Array.FindIndex(interfaceMapping.InterfaceMethods, m => m.Name == nameof(IInteractableLockable.SetLockObject));
                    MethodInfo setLockObjectMethod = ArrayUtils.GetSafe(interfaceMapping.TargetMethods, methodIndex);
                    if (setLockObjectMethod != null && patchedSetLockObjectMethods.Add(setLockObjectMethod))
                    {
                        new Hook(setLockObjectMethod, IInteractableLockable_SetLockObject);
                    }
                }
            }
        }

        delegate void orig_SetLockObject(IInteractableLockable self, GameObject value);
        static void IInteractableLockable_SetLockObject(orig_SetLockObject orig, IInteractableLockable self, GameObject lockObject)
        {
            orig(self, lockObject);

            if (NetworkServer.active)
            {
                GameObject lockableObject = self?.GetGameObject();
                if (lockableObject)
                {
                    ServerLockObjectTracker lockObjectTracker = lockableObject.EnsureComponent<ServerLockObjectTracker>();
                    lockObjectTracker.SetLockObject(lockObject);
                }
            }
        }

        sealed class ServerLockObjectTracker : MonoBehaviour
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
