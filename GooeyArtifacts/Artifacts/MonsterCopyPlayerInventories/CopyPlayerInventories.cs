using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.MonsterCopyPlayerInventories
{
    [RequireComponent(typeof(Inventory))]
    public class CopyPlayerInventories : MonoBehaviour
    {
        Inventory _inventory;

        bool _inventoryDirty;

        static bool itemCopyFilter(ItemIndex itemIndex)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
            return itemDef && !itemDef.hidden;
        }

        void Awake()
        {
            _inventory = GetComponent<Inventory>();
        }

        void OnEnable()
        {
            Inventory.onInventoryChangedGlobal += Inventory_onInventoryChangedGlobal;
            CharacterMaster.onStartGlobal += CharacterMaster_onStartGlobal;

            refreshInventory();
        }

        void OnDisable()
        {
            Inventory.onInventoryChangedGlobal -= Inventory_onInventoryChangedGlobal;
            CharacterMaster.onStartGlobal -= CharacterMaster_onStartGlobal;
        }

        void FixedUpdate()
        {
            if (_inventoryDirty)
            {
                refreshInventory();
                _inventoryDirty = false;
            }
        }

        void refreshInventory()
        {
            if (!NetworkServer.active)
                return;

            int[] oldItemStacks = ItemCatalog.GetPerItemBuffer<int>();
            _inventory.WriteItemStacks(oldItemStacks);

            List<ItemIndex> itemOrder = [.. _inventory.itemAcquisitionOrder];
            int[] newItemStacks = ItemCatalog.GetPerItemBuffer<int>();

            foreach (PlayerCharacterMasterController playerController in PlayerCharacterMasterController.instances)
            {
                CharacterMaster master = playerController.master;
                if (!master)
                    continue;

                Inventory inventory = master.inventory;
                if (!inventory)
                    continue;

                foreach (ItemIndex itemIndex in inventory.itemAcquisitionOrder)
                {
                    if (itemCopyFilter(itemIndex))
                    {
                        if (!itemOrder.Contains(itemIndex))
                        {
                            itemOrder.Add(itemIndex);
                        }

                        newItemStacks[(int)itemIndex] += inventory.GetItemCount(itemIndex);
                    }
                }
            }

            Inventory[] allNonPlayersInventories = CharacterMaster.readOnlyInstancesList.Where(m => m.teamIndex != TeamIndex.Player)
                                                                                        .Select(m => m.inventory)
                                                                                        .Where(i => i)
                                                                                        .Distinct()
                                                                                        .ToArray();

            foreach (ItemIndex item in itemOrder)
            {
                int currentItemCount = oldItemStacks[(int)item];
                int newItemCount = newItemStacks[(int)item];

                int itemCountDiff = newItemCount - currentItemCount;
                if (itemCountDiff == 0)
                    continue;

                if (itemCountDiff > 0)
                {
                    Log.Debug($"Adding {Language.GetString(ItemCatalog.GetItemDef(item).nameToken)} (x{itemCountDiff}) to inventories");
                }
                else
                {
                    Log.Debug($"Removing {Language.GetString(ItemCatalog.GetItemDef(item).nameToken)} (x{-itemCountDiff}) from inventories");
                }

                _inventory.GiveItem(item, itemCountDiff);

                foreach (Inventory inventory in allNonPlayersInventories)
                {
                    inventory.GiveItem(item, itemCountDiff);
                }
            }
        }

        void Inventory_onInventoryChangedGlobal(Inventory inventory)
        {
            if (inventory && inventory.TryGetComponent(out CharacterMaster master) && master.teamIndex == TeamIndex.Player)
            {
                _inventoryDirty = true;
            }
        }

        void CharacterMaster_onStartGlobal(CharacterMaster master)
        {
            if (!NetworkServer.active)
                return;

            if (master.teamIndex != TeamIndex.Player)
            {
                Inventory inventory = master.inventory;
                if (inventory)
                {
                    inventory.AddItemsFrom(_inventory);
                }
            }
        }
    }
}
