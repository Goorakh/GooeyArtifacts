using GooeyArtifacts.Utils;
using RoR2;
using System.Collections;
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
                self.StartCoroutine(waitForServerInitThenSyncTerminalTransform(self));
            }
        }

        static IEnumerator waitForServerInitThenSyncTerminalTransform(ShopTerminalBehavior terminalBehavior)
        {
            while (terminalBehavior && !terminalBehavior.serverMultiShopController)
            {
                yield return null;
            }

            if (!terminalBehavior || !terminalBehavior.serverMultiShopController)
                yield break;
            
            MultiShopController multiShopController = terminalBehavior.serverMultiShopController;
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
