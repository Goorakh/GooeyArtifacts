using GooeyArtifacts.Artifacts;
using GooeyArtifacts.EntityStates;
using GooeyArtifacts.Items;
using RoR2.ContentManagement;
using System.Collections;
using System.Linq;

namespace GooeyArtifacts
{
    public class ContentPackProvider : IContentPackProvider
    {
        readonly ContentPack _contentPack = new ContentPack();

        public string identifier => Main.PluginGUID;

        internal ContentPackProvider()
        {
        }

        internal void Register()
        {
            ContentManager.collectContentPackProviders += addContentPackProvider =>
            {
                addContentPackProvider(this);
            };
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            _contentPack.identifier = identifier;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            ItemDefs.AddItemDefsTo(_contentPack.itemDefs);
            ArtifactDefs.AddArtifactDefsTo(_contentPack.artifactDefs);
            _contentPack.entityStateTypes.Add(EntityStateTypeAttribute.GetAllEntityStateTypes().ToArray());

            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(_contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}
