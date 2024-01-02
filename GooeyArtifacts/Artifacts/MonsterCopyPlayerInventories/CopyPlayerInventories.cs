using RoR2;
using System.Linq;
using UnityEngine;

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
            int[] oldItemStacks = ItemCatalog.GetPerItemBuffer<int>();
            _inventory.WriteItemStacks(oldItemStacks);

            for (int i = _inventory.itemAcquisitionOrder.Count - 1; i >= 0; i--)
            {
                ItemIndex itemIndex = _inventory.itemAcquisitionOrder[i];
                _inventory.RemoveItem(itemIndex, _inventory.GetItemCount(itemIndex));
            }

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
                        _inventory.GiveItem(itemIndex, inventory.GetItemCount(itemIndex));
                    }
                }
            }

            Inventory[] allNonPlayersInventories = CharacterMaster.readOnlyInstancesList.Where(m => m.teamIndex != TeamIndex.Player)
                                                                                        .Select(m => m.inventory)
                                                                                        .Where(i => i)
                                                                                        .Distinct()
                                                                                        .ToArray();

            if (allNonPlayersInventories.Length > 0)
            {
                foreach (ItemIndex i in ItemCatalog.allItems)
                {
                    int stackDiff = _inventory.GetItemCount(i) - oldItemStacks[(int)i];
                    if (stackDiff != 0)
                    {
#if DEBUG
                        if (stackDiff > 0)
                        {
                            Log.Debug($"Adding {Language.GetString(ItemCatalog.GetItemDef(i).nameToken)} (x{stackDiff}) to monster inventories");
                        }
                        else
                        {
                            Log.Debug($"Removing {Language.GetString(ItemCatalog.GetItemDef(i).nameToken)} (x{-stackDiff}) from monster inventories");
                        }
#endif

                        foreach (Inventory inventory in allNonPlayersInventories)
                        {
                            inventory.GiveItem(i, stackDiff);
                        }
                    }
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
