using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.ExpiringItems
{
    public static class ExpiringItemsArtifactManager
    {
        public static bool ItemCanExpireFilter(ItemIndex itemIndex)
        {
            ItemDef item = ItemCatalog.GetItemDef(itemIndex);
            return !item.hidden && item.canRemove && item.tier != ItemTier.NoTier;
        }

        public static float GetExpirationTime(ItemIndex itemIndex, int itemCount)
        {
            float durationMinutes;
            switch (ItemCatalog.GetItemDef(itemIndex).tier)
            {
                case ItemTier.Tier1:
                    durationMinutes = 4.5f;
                    break;
                case ItemTier.Tier2:
                    durationMinutes = 5.5f;
                    break;
                case ItemTier.Tier3:
                    durationMinutes = 6.5f;
                    break;
                case ItemTier.Lunar:
                case ItemTier.Boss:
                    durationMinutes = 7.5f;
                    break;
                case ItemTier.VoidTier1:
                    durationMinutes = 5.5f;
                    break;
                case ItemTier.VoidTier2:
                    durationMinutes = 6.5f;
                    break;
                case ItemTier.VoidTier3:
                    durationMinutes = 7.5f;
                    break;
                case ItemTier.VoidBoss:
                    durationMinutes = 8.5f;
                    break;
                default:
                    durationMinutes = 4.5f;
                    break;
            }

            if (SceneCatalog.mostRecentSceneDef)
            {
                switch (SceneCatalog.mostRecentSceneDef.cachedName)
                {
                    case "moon":
                    case "moon2":
                        durationMinutes *= 2.5f;
                        break;
                }
            }

            return Mathf.Max(1f, durationMinutes * 60f * Mathf.Exp(-(itemCount - 1) / 5f));
        }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterMaster.Awake += (orig, self) =>
            {
                orig(self);
                self.gameObject.AddComponent<ItemHeldTimeTracker>();
            };

            RunArtifactManager.onArtifactEnabledGlobal += RunArtifactManager_onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += RunArtifactManager_onArtifactDisabledGlobal;
        }

        static void RunArtifactManager_onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != ArtifactDefs.ExpiringItems)
                return;

            if (NetworkServer.active)
            {
                foreach (ItemHeldTimeTracker itemHeldTimeTracker in InstanceTracker.GetInstancesList<ItemHeldTimeTracker>())
                {
                    itemHeldTimeTracker.SetAllItemsPickedUpNow();
                }
            }

            RoR2Application.onFixedUpdate += onFixedUpdate;

            SceneDirector.onPrePopulateSceneServer += SceneDirector_onPrePopulateSceneServer;
        }

        static void RunArtifactManager_onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != ArtifactDefs.ExpiringItems)
                return;

            RoR2Application.onFixedUpdate -= onFixedUpdate;

            SceneDirector.onPrePopulateSceneServer -= SceneDirector_onPrePopulateSceneServer;
        }

        static void SceneDirector_onPrePopulateSceneServer(SceneDirector sceneDirector)
        {
            sceneDirector.onPopulateCreditMultiplier *= 1.25f;
        }

        static void onFixedUpdate()
        {
            if (!NetworkServer.active)
                return;

            foreach (ItemHeldTimeTracker itemHeldTimeTracker in InstanceTracker.GetInstancesList<ItemHeldTimeTracker>())
            {
                for (int i = itemHeldTimeTracker.Inventory.itemAcquisitionOrder.Count - 1; i >= 0; i--)
                {
                    ItemIndex itemIndex = itemHeldTimeTracker.Inventory.itemAcquisitionOrder[i];
                    if (!ItemCanExpireFilter(itemIndex))
                        continue;

                    float totalTimeHeld = itemHeldTimeTracker.GetItemHeldTime(itemIndex);
                    if (totalTimeHeld >= GetExpirationTime(itemIndex, itemHeldTimeTracker.Inventory.GetItemCount(itemIndex)))
                    {
                        itemHeldTimeTracker.Inventory.RemoveItem(itemIndex);

#if DEBUG
                        Log.Debug($"Removed item {Language.GetString(ItemCatalog.GetItemDef(itemIndex).nameToken)} from {itemHeldTimeTracker.name}");
#endif
                    }
                }
            }
        }
    }
}
