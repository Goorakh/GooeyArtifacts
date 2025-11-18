using GooeyArtifacts.Utils;
using RoR2;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Patches
{
    static class DroneVendorTerminalPositionSyncPatch
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.DroneVendorTerminalBehavior.Start += ShopTerminalBehavior_Start;
        }

        static void ShopTerminalBehavior_Start(On.RoR2.DroneVendorTerminalBehavior.orig_Start orig, DroneVendorTerminalBehavior self)
        {
            orig(self);

            if (NetworkServer.active)
            {
                self.StartCoroutine(waitForServerInitThenSyncTerminalTransform(self));
            }
        }

        static IEnumerator waitForServerInitThenSyncTerminalTransform(DroneVendorTerminalBehavior terminalBehavior)
        {
            while (terminalBehavior && !terminalBehavior.serverMultiShopController)
            {
                yield return null;
            }

            if (!terminalBehavior || !terminalBehavior.serverMultiShopController)
                yield break;

            DroneVendorMultiShopController multiShopController = terminalBehavior.serverMultiShopController;
            Transform multiShopTransform = multiShopController.transform;

            terminalBehavior.transform.GetPositionAndRotation(out Vector3 terminalPosition, out Quaternion terminalRotation);
            Vector3 terminalLocalPosition = multiShopTransform.InverseTransformPoint(terminalPosition);
            Quaternion terminalLocalRotation = Quaternion.Inverse(multiShopTransform.rotation) * terminalRotation;

            GameObject pseudoParentControllerObj = GameObject.Instantiate(Prefabs.SyncExternalNetObjectPseudoParentPrefab, terminalLocalPosition, terminalLocalRotation);
            SyncExternalTransformPseudoParent pseudoParentController = pseudoParentControllerObj.GetComponent<SyncExternalTransformPseudoParent>();
            pseudoParentController.Parent = multiShopTransform.gameObject;
            pseudoParentController.TargetObject = terminalBehavior.gameObject;
            NetworkServer.Spawn(pseudoParentControllerObj);
        }
    }
}
