using GooeyArtifacts.Utils.Extensions;
using HG;
using RoR2;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.ExpiringItems
{
    public class ItemHeldTimeTracker : NetworkBehaviour
    {
        Run.FixedTimeStamp[] _itemPickupTimestamps;
        const uint ITEM_PICKUP_TIMESTAMPS_DIRTY_BIT = 1 << 0;

        int[] _recordedItemStacks;

        public Inventory Inventory { get; private set; }

        void Awake()
        {
            _itemPickupTimestamps = ItemCatalog.GetPerItemBuffer<Run.FixedTimeStamp>();
            ArrayUtils.SetAll(_itemPickupTimestamps, Run.FixedTimeStamp.positiveInfinity);

            Inventory = GetComponent<Inventory>();
            Inventory.onInventoryChanged += onInventoryChanged;

            _recordedItemStacks = ItemCatalog.RequestItemStackArray();
            Inventory.WriteItemStacks(_recordedItemStacks);
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }

        void onInventoryChanged()
        {
            if (!NetworkServer.active)
                return;

            bool itemsChanged = false;

            for (ItemIndex i = 0; i < (ItemIndex)ItemCatalog.itemCount; i++)
            {
                int recordedItemCount = _recordedItemStacks[(int)i];
                int currentItemCount = Inventory.GetItemCount(i);

                if (recordedItemCount != currentItemCount)
                {
                    _itemPickupTimestamps[(int)i] = currentItemCount > 0 ? Run.FixedTimeStamp.now : Run.FixedTimeStamp.positiveInfinity;
                    _recordedItemStacks[(int)i] = currentItemCount;

                    itemsChanged = true;
                }
            }

            if (itemsChanged)
            {
                SetDirtyBit(ITEM_PICKUP_TIMESTAMPS_DIRTY_BIT);
            }
        }

        public float GetItemHeldTime(ItemIndex itemIndex)
        {
            Run.FixedTimeStamp pickupTime = ArrayUtils.GetSafe(_itemPickupTimestamps, (int)itemIndex, Run.FixedTimeStamp.positiveInfinity);
            return pickupTime.isInfinity ? -1f : pickupTime.timeSinceClamped;
        }

        public void SetAllItemsPickedUpNow()
        {
            bool anyChanged = false;
            for (int i = 0; i < _itemPickupTimestamps.Length; i++)
            {
                if (!_itemPickupTimestamps[i].isInfinity)
                {
                    _itemPickupTimestamps[i] = Run.FixedTimeStamp.now;
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                SetDirtyBit(ITEM_PICKUP_TIMESTAMPS_DIRTY_BIT);
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WriteItemPickupTimestamps(_itemPickupTimestamps);

                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;

            if ((dirtyBits & ITEM_PICKUP_TIMESTAMPS_DIRTY_BIT) != 0)
            {
                writer.WriteItemPickupTimestamps(_itemPickupTimestamps);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                reader.ReadItemPickupTimestamps(_itemPickupTimestamps);
                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();

            if ((dirtyBits & ITEM_PICKUP_TIMESTAMPS_DIRTY_BIT) != 0)
            {
                reader.ReadItemPickupTimestamps(_itemPickupTimestamps);
            }
        }
    }
}
