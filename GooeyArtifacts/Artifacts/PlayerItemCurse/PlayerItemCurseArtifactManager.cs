using HG;
using R2API;
using RoR2;
using System;

namespace GooeyArtifacts.Artifacts.PlayerItemCurse
{
    public static class PlayerItemCurseArtifactManager
    {
        static readonly float[] _tierCurseWeights;
        const float EQUIPMENT_WEIGHT = 5f;

        static PlayerItemCurseArtifactManager()
        {
            _tierCurseWeights = new float[(int)ItemTier.AssignedAtRuntime + 1];
            Array.Fill(_tierCurseWeights, 1f);

            const float TIER_1_WEIGHT = 1f;
            const float TIER_2_WEIGHT = 1.5f;
            const float TIER_3_WEIGHT = 2f;
            const float LUNAR_WEIGHT = 7f;
            const float BOSS_WEIGHT = 7f;

            const float VOID_TIER_MULT = 1.25f;
            const float VOID_TIER_1_WEIGHT = TIER_1_WEIGHT * VOID_TIER_MULT;
            const float VOID_TIER_2_WEIGHT = TIER_2_WEIGHT * VOID_TIER_MULT;
            const float VOID_TIER_3_WEIGHT = TIER_3_WEIGHT * VOID_TIER_MULT;
            const float VOID_BOSS_WEIGHT = BOSS_WEIGHT * VOID_TIER_MULT;

            _tierCurseWeights[(int)ItemTier.Tier1] = TIER_1_WEIGHT;
            _tierCurseWeights[(int)ItemTier.Tier2] = TIER_2_WEIGHT;
            _tierCurseWeights[(int)ItemTier.Tier3] = TIER_3_WEIGHT;
            _tierCurseWeights[(int)ItemTier.Lunar] = LUNAR_WEIGHT;
            _tierCurseWeights[(int)ItemTier.Boss] = BOSS_WEIGHT;
            _tierCurseWeights[(int)ItemTier.VoidTier1] = VOID_TIER_1_WEIGHT;
            _tierCurseWeights[(int)ItemTier.VoidTier2] = VOID_TIER_2_WEIGHT;
            _tierCurseWeights[(int)ItemTier.VoidTier3] = VOID_TIER_3_WEIGHT;
            _tierCurseWeights[(int)ItemTier.VoidBoss] = VOID_BOSS_WEIGHT;
        }

        [SystemInitializer]
        static void Init()
        {
            RunArtifactManager.onArtifactEnabledGlobal += RunArtifactManager_onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += RunArtifactManager_onArtifactDisabledGlobal;

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(ArtifactDefs.PlayerItemCurse))
                return;

            Inventory inventory = sender.inventory;
            if (!inventory)
                return;

            float totalInventoryValue = 0f;
            foreach (ItemIndex itemIndex in inventory.itemAcquisitionOrder)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (!itemDef || itemDef.hidden || itemDef.tier == ItemTier.NoTier)
                    continue;

                totalInventoryValue += inventory.GetItemCountEffective(itemIndex) * ArrayUtils.GetSafe(_tierCurseWeights, (int)itemDef.tier, 1f);
            }

            int equipmentSlotCount = inventory.GetEquipmentSlotCount();
            for (uint slot = 0; slot < equipmentSlotCount; slot++)
            {
                int equipmentSetCount = inventory.GetEquipmentSetCount(slot);
                for (uint set = 0; set < equipmentSetCount; set++)
                {
                    if (inventory.GetEquipment(slot, set).equipmentIndex != EquipmentIndex.None)
                    {
                        totalInventoryValue += EQUIPMENT_WEIGHT;
                    }
                }
            }

            args.baseCurseAdd += 0.025f * totalInventoryValue;
        }

        static void RunArtifactManager_onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef == ArtifactDefs.PlayerItemCurse)
            {
                markAllPlayerStatsDirty();
            }
        }

        static void RunArtifactManager_onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef == ArtifactDefs.PlayerItemCurse)
            {
                markAllPlayerStatsDirty();
            }
        }

        static void markAllPlayerStatsDirty()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                body.MarkAllStatsDirty();
            }
        }
    }
}
