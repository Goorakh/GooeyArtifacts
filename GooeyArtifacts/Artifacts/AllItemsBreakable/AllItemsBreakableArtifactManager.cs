using GooeyArtifacts.Items;
using GooeyArtifacts.Utils;
using RoR2;
using RoR2.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.AllItemsBreakable
{
    public static class AllItemsBreakableArtifactManager
    {
        static EffectIndex _itemBreakEffectIndex = EffectIndex.Invalid;

        [SystemInitializer(typeof(EffectCatalog))]
        static void Init()
        {
            On.RoR2.HealthComponent.UpdateLastHitTime += HealthComponent_UpdateLastHitTime;

            On.RoR2.UI.HealthBar.CheckInventory += HealthBar_CheckInventory;

            AssetLoadUtils.LoadAssetTemporary<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_FragileDamageBonus.DelicateWatchProcEffect_prefab, watchBreakEffectPrefab =>
            {
                _itemBreakEffectIndex = EffectCatalog.FindEffectIndexFromPrefab(watchBreakEffectPrefab);
                if (_itemBreakEffectIndex == EffectIndex.Invalid)
                {
                    Log.Error($"Failed to find item break effect index");
                }
            });
        }

        static bool isBreakableFilter(ItemIndex itemIndex)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
            return !itemDef.hidden && itemDef.canRemove;
        }

        static void HealthComponent_UpdateLastHitTime(On.RoR2.HealthComponent.orig_UpdateLastHitTime orig, HealthComponent self, float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker, bool delayedDamage, bool firstHitOfDelayedDamage)
        {
            orig(self, damageValue, damagePosition, damageIsSilent, attacker, delayedDamage, firstHitOfDelayedDamage);

            if (!NetworkServer.active || !self.body || damageValue <= 0f)
                return;

            if (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(ArtifactDefs.AllItemsBreakable))
                return;

            if (!self.isHealthLow)
                return;

            Inventory inventory = self.body.inventory;
            if (!inventory)
                return;

            int brokenItemStacks = 0;
            int totalBrokenItemCount = 0;

            for (int i = inventory.itemAcquisitionOrder.Count - 1; brokenItemStacks < 7 && i >= 0; i--)
            {
                ItemIndex itemIndex = inventory.itemAcquisitionOrder[i];
                if (!isBreakableFilter(itemIndex))
                    continue;

                int itemCount = inventory.GetItemCount(itemIndex);
                inventory.RemoveItem(itemIndex, itemCount);
                totalBrokenItemCount += itemCount;

                CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, ItemDefs.GenericBrokenItem.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);

                brokenItemStacks++;
            }

            if (brokenItemStacks > 0)
            {
                inventory.GiveItem(ItemDefs.GenericBrokenItem, totalBrokenItemCount);

                if (_itemBreakEffectIndex != EffectIndex.Invalid)
                {
                    EffectData watchBreakEffectData = new EffectData
                    {
                        origin = self.transform.position
                    };

                    watchBreakEffectData.SetNetworkedObjectReference(self.gameObject);

                    EffectManager.SpawnEffect(_itemBreakEffectIndex, watchBreakEffectData, true);
                }
            }
        }

        static void HealthBar_CheckInventory(On.RoR2.UI.HealthBar.orig_CheckInventory orig, HealthBar self)
        {
            orig(self);

            HealthComponent healthComponent = self.source;
            if (!healthComponent)
                return;

            CharacterBody body = healthComponent.body;
            if (!body)
                return;

            Inventory inventory = body.inventory;
            if (!inventory)
                return;

            self.hasLowHealthItem |= RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(ArtifactDefs.AllItemsBreakable) && inventory.itemAcquisitionOrder.Any(isBreakableFilter);
        }
    }
}
