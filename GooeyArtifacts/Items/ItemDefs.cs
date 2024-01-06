using GooeyArtifacts.Utils;
using GooeyArtifacts.Utils.Extensions;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace GooeyArtifacts.Items
{
    public static class ItemDefs
    {
        public static readonly ItemDef GenericBrokenItem;

        static ItemDefs()
        {
            // GenericBrokenItem
            {
                GenericBrokenItem = ScriptableObject.CreateInstance<ItemDef>();
                GenericBrokenItem.name = nameof(GenericBrokenItem);

                GenericBrokenItem.deprecatedTier = ItemTier.NoTier;
                GenericBrokenItem.canRemove = false;

                GenericBrokenItem.nameToken = "ITEM_GENERIC_BROKEN_NAME";
                GenericBrokenItem.descriptionToken = "ITEM_GENERIC_BROKEN_DESC";
                GenericBrokenItem.pickupToken = "ITEM_GENERIC_BROKEN_PICKUP";

                GenericBrokenItem.pickupIconSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.GenericBrokenItemPickupSprite);
            }
        }

        internal static void AddItemDefsTo(NamedAssetCollection<ItemDef> itemDefs)
        {
            itemDefs.Add(new ItemDef[]
            {
                GenericBrokenItem
            });
        }

#if DEBUG
        [ConCommand(commandName = "dump_item_icons")]
        static void CCDumpArtifactIcons(ConCommandArgs args)
        {
            static void dumpArtifactIcons(ItemDef item)
            {
                string path = System.IO.Path.Combine(Main.PluginDirectory, "items_dump");
                System.IO.Directory.CreateDirectory(path);

                void writeSpriteToFile(Sprite sprite, string fileName)
                {
                    if (!sprite)
                        return;

                    Texture2D texture = sprite.texture;
                    if (!texture)
                        return;

                    using TemporaryTexture readable = texture.AsReadable();

                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(path, fileName), readable.Texture.EncodeToPNG());
                }

                writeSpriteToFile(item.pickupIconSprite, $"{item.name}.png");
            }

            foreach (ItemDef item in ItemCatalog.allItemDefs)
            {
                dumpArtifactIcons(item);
            }
        }
#endif
    }
}
