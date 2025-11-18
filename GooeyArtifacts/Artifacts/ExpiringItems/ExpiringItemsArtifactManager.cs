using RoR2;

namespace GooeyArtifacts.Artifacts.ExpiringItems
{
    public static class ExpiringItemsArtifactManager
    {
        [SystemInitializer]
        static void Init()
        {
            RunArtifactManager.onArtifactEnabledGlobal += RunArtifactManager_onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += RunArtifactManager_onArtifactDisabledGlobal;
        }

        static void RunArtifactManager_onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != ArtifactDefs.ExpiringItems)
                return;

            SceneDirector.onPrePopulateSceneServer += SceneDirector_onPrePopulateSceneServer;
            On.RoR2.ItemDef.AttemptGrant += ItemDef_AttemptGrant;
        }

        static void RunArtifactManager_onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != ArtifactDefs.ExpiringItems)
                return;

            SceneDirector.onPrePopulateSceneServer -= SceneDirector_onPrePopulateSceneServer;
            On.RoR2.ItemDef.AttemptGrant -= ItemDef_AttemptGrant;
        }

        static void SceneDirector_onPrePopulateSceneServer(SceneDirector sceneDirector)
        {
            sceneDirector.onPopulateCreditMultiplier *= 1.25f;
        }

        static void ItemDef_AttemptGrant(On.RoR2.ItemDef.orig_AttemptGrant orig, ref PickupDef.GrantContext context)
        {
            UniquePickup pickup = context.controller.pickup;
            pickup.decayValue = 1f;
            context.controller.pickup = pickup;
            orig(ref context);
        }
    }
}
