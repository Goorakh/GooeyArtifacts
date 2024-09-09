using GooeyArtifacts.Utils;
using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GooeyArtifacts.Artifacts.ExpiringItems
{
    public class ItemExpirationTimerDisplayController : MonoBehaviour
    {
        static Sprite _timerSprite;

        [SystemInitializer]
        static void Init()
        {
            _timerSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.ItemExpirationTimerIcon);

            On.RoR2.UI.ItemIcon.Awake += (orig, self) =>
            {
                orig(self);

                ItemExpirationTimerDisplayController expirationDisplayController = self.gameObject.AddComponent<ItemExpirationTimerDisplayController>();
                expirationDisplayController._itemIcon = self;
            };

            On.RoR2.UI.ItemInventoryDisplay.AllocateIcons += (orig, self, desiredItemCount) =>
            {
                orig(self, desiredItemCount);

                List<ItemIcon> itemIcons = self.itemIcons;

                if (itemIcons is null)
                    return;

                foreach (ItemIcon icon in itemIcons)
                {
                    if (icon.TryGetComponent(out ItemExpirationTimerDisplayController itemExpirationDisplay))
                    {
                        itemExpirationDisplay.OwnerInventory = self.inventory;
                    }
                }
            };
        }

        ItemHeldTimeTracker _itemHeldTimeTracker;
        Inventory _ownerInventory;
        public Inventory OwnerInventory
        {
            get
            {
                return _ownerInventory;
            }
            set
            {
                if (_ownerInventory == value)
                    return;

                _ownerInventory = value;
                _itemHeldTimeTracker = _ownerInventory ? _ownerInventory.GetComponent<ItemHeldTimeTracker>() : null;
            }
        }

        ItemIcon _itemIcon;

        GameObject _timerObject;
        Image _timerImage;

        bool _timerVisible;
        public bool TimerVisible
        {
            get
            {
                return _timerVisible;
            }
            set
            {
                if (_timerVisible == value)
                    return;

                _timerVisible = value;

                if (_timerObject)
                {
                    _timerObject.SetActive(_timerVisible);
                }
                else
                {
                    if (_timerVisible)
                    {
                        _timerObject = new GameObject("Timer");
                        _timerObject.transform.SetParent(transform, false);
                        RectTransform timerTransform = _timerObject.AddComponent<RectTransform>();

                        timerTransform.localScale = Vector3.one * 64f;
                        timerTransform.localPosition = new Vector3(32f, -32f, 0f);
                        timerTransform.sizeDelta = Vector2.one;

                        _timerImage = _timerObject.AddComponent<Image>();
                        _timerImage.type = Image.Type.Filled;
                        _timerImage.fillClockwise = false;
                        _timerImage.fillMethod = Image.FillMethod.Radial360;
                        _timerImage.fillOrigin = 2;
                        _timerImage.sprite = _timerSprite;
                    }
                }
            }
        }

        public void SetTimerFraction(float fraction)
        {
            if (_timerImage)
            {
                _timerImage.fillAmount = fraction;
            }
        }

        bool shouldTimerBeVisible(out float fraction)
        {
            fraction = 0f;

            if (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(ArtifactDefs.ExpiringItems))
                return false;

            if (!OwnerInventory || !_itemHeldTimeTracker)
                return false;

            ItemIndex itemIndex = _itemIcon.itemIndex;

            if (!ExpiringItemsArtifactManager.ItemCanExpireFilter(itemIndex))
                return false;

            float timeHeld = _itemHeldTimeTracker.GetItemHeldTime(itemIndex);
            if (timeHeld < 0f)
                return false;

            float expirationTime = ExpiringItemsArtifactManager.GetExpirationTime(itemIndex, _ownerInventory.GetItemCount(itemIndex));
            fraction = Mathf.Clamp01(1f - (timeHeld / expirationTime));
            return true;
        }

        void FixedUpdate()
        {
            TimerVisible = shouldTimerBeVisible(out float fraction);
            if (TimerVisible)
            {
                SetTimerFraction(fraction);

                ItemIndex itemIndex = _itemIcon.itemIndex;

                Color timerColor = Color.white;

                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef)
                {
                    ItemTierDef itemTier = ItemTierCatalog.GetItemTierDef(itemDef.tier);
                    if (itemTier)
                    {
                        timerColor = ColorCatalog.GetColor(itemTier.colorIndex);
                    }
                }

                _timerImage.color = timerColor;
            }
        }
    }
}
