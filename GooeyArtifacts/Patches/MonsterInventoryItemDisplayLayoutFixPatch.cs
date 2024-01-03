using RoR2;
using RoR2.UI;
using UnityEngine.UI;

namespace GooeyArtifacts.Patches
{
    // Fixes the item container not resizing to fit the item icons until there is some other UI update on the info panel
    static class MonsterInventoryItemDisplayLayoutFixPatch
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.ItemInventoryDisplay.UpdateDisplay += ItemInventoryDisplay_UpdateDisplay;
        }

        static void ItemInventoryDisplay_UpdateDisplay(On.RoR2.UI.ItemInventoryDisplay.orig_UpdateDisplay orig, ItemInventoryDisplay self)
        {
            orig(self);

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            LayoutRebuilder.MarkLayoutForRebuild(self.rectTransform);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
        }
    }
}
