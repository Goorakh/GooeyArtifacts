using GooeyArtifacts.Items;
using RoR2;
using RoR2.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.AllItemsBreakable
{
    public static class AllItemsBreakableArtifactManager
    {
        static readonly GameObject _watchBreakEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/FragileDamageBonus/DelicateWatchProcEffect.prefab").WaitForCompletion();

        static bool isBreakableFilter(ItemIndex itemIndex)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
            return !itemDef.hidden && itemDef.canRemove;
        }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.HealthComponent.UpdateLastHitTime += HealthComponent_UpdateLastHitTime;

            On.RoR2.UI.HealthBar.CheckInventory += HealthBar_CheckInventory;
        }

        static void HealthComponent_UpdateLastHitTime(On.RoR2.HealthComponent.orig_UpdateLastHitTime orig, HealthComponent self, float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker)
        {
            orig(self, damageValue, damagePosition, damageIsSilent, attacker);

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

                EffectData watchBreakEffectData = new EffectData
                {
                    origin = self.transform.position
                };
                watchBreakEffectData.SetNetworkedObjectReference(self.gameObject);

                EffectManager.SpawnEffect(_watchBreakEffect, watchBreakEffectData, true);
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

            ref bool hasLowHealthItem = ref self.hasLowHealthItem;

            hasLowHealthItem = hasLowHealthItem || (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(ArtifactDefs.AllItemsBreakable) && inventory.itemAcquisitionOrder.Any(isBreakableFilter));
        }
    }
}
