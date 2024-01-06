using GooeyArtifacts.Utils;
using GooeyArtifacts.Utils.Extensions;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace GooeyArtifacts.Artifacts
{
    public static class ArtifactDefs
    {
        public static readonly ArtifactDef MonsterCopyPlayerItems;

        public static readonly ArtifactDef PlayerItemCurse;

        public static readonly ArtifactDef PillarsEveryStage;

        public static readonly ArtifactDef MovingInteractables;

        public static readonly ArtifactDef AllItemsBreakable;

        static ArtifactDefs()
        {
            // MonsterCopyPlayerItems
            {
                MonsterCopyPlayerItems = ScriptableObject.CreateInstance<ArtifactDef>();
                MonsterCopyPlayerItems.cachedName = nameof(MonsterCopyPlayerItems);

                MonsterCopyPlayerItems.nameToken = "ARTIFACT_MONSTER_COPY_PLAYER_ITEMS_NAME";
                MonsterCopyPlayerItems.descriptionToken = "ARTIFACT_MONSTER_COPY_PLAYER_ITEMS_DESCRIPTION";

                MonsterCopyPlayerItems.smallIconSelectedSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.MonsterCopyPlayerItemsIconSelected);
                MonsterCopyPlayerItems.smallIconDeselectedSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.MonsterCopyPlayerItemsIconDeselected);
            }

            // PlayerItemCurse
            {
                PlayerItemCurse = ScriptableObject.CreateInstance<ArtifactDef>();
                PlayerItemCurse.cachedName = nameof(PlayerItemCurse);

                PlayerItemCurse.nameToken = "ARTIFACT_PLAYER_ITEM_CURSE_NAME";
                PlayerItemCurse.descriptionToken = "ARTIFACT_PLAYER_ITEM_CURSE_DESCRIPTION";

                PlayerItemCurse.smallIconSelectedSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.PlayerItemCurseIconSelected);
                PlayerItemCurse.smallIconDeselectedSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.PlayerItemCurseIconDeselected);
            }

            // PillarsEveryStage
            {
                PillarsEveryStage = ScriptableObject.CreateInstance<ArtifactDef>();
                PillarsEveryStage.cachedName = nameof(PillarsEveryStage);

                PillarsEveryStage.nameToken = "ARTIFACT_PILLARS_EVERY_STAGE_NAME";
                PillarsEveryStage.descriptionToken = "ARTIFACT_PILLARS_EVERY_STAGE_DESCRIPTION";

                PillarsEveryStage.smallIconSelectedSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.PillarsEveryStageIconSelected);
                PillarsEveryStage.smallIconDeselectedSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.PillarsEveryStageIconDeselected);
            }

            // MovingInteractables
            {
                MovingInteractables = ScriptableObject.CreateInstance<ArtifactDef>();
                MovingInteractables.cachedName = nameof(MovingInteractables);

                MovingInteractables.nameToken = "ARTIFACT_MOVING_INTERACTABLES_NAME";
                MovingInteractables.descriptionToken = "ARTIFACT_MOVING_INTERACTABLES_DESCRIPTION";

                MovingInteractables.smallIconSelectedSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.MovingInteractablesIconSelected);
                MovingInteractables.smallIconDeselectedSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.MovingInteractablesIconDeselected);
            }

            // AllItemsBreakable
            {
                AllItemsBreakable = ScriptableObject.CreateInstance<ArtifactDef>();
                AllItemsBreakable.cachedName = nameof(AllItemsBreakable);

                AllItemsBreakable.nameToken = "ARTIFACT_ALL_ITEMS_BREAKABLE_NAME";
                AllItemsBreakable.descriptionToken = "ARTIFACT_ALL_ITEMS_BREAKABLE_DESCRIPTION";

                AllItemsBreakable.smallIconSelectedSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.AllItemsBreakableIconSelected);
                AllItemsBreakable.smallIconDeselectedSprite = IconLoader.LoadSpriteFromBytes(Properties.Resources.AllItemsBreakableIconDeselected);
            }
        }

        internal static void AddArtifactDefsTo(NamedAssetCollection<ArtifactDef> collection)
        {
            collection.Add(new ArtifactDef[]
            {
                MonsterCopyPlayerItems,
                PlayerItemCurse,
                PillarsEveryStage,
                MovingInteractables,
                AllItemsBreakable
            });
        }

#if DEBUG
        [ConCommand(commandName = "dump_artifact_icons")]
        static void CCDumpArtifactIcons(ConCommandArgs args)
        {
            static void dumpArtifactIcons(ArtifactDef artifact)
            {
                string path = System.IO.Path.Combine(Main.PluginDirectory, "icons_dump", artifact.cachedName);
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

                writeSpriteToFile(artifact.smallIconSelectedSprite, "selected.png");
                writeSpriteToFile(artifact.smallIconDeselectedSprite, "deselected.png");
            }

            foreach (ArtifactDef artifact in ArtifactCatalog.artifactDefs)
            {
                dumpArtifactIcons(artifact);
            }
        }
#endif
    }
}
