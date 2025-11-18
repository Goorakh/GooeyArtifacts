using GooeyArtifacts.Artifacts;
using GooeyArtifacts.EntityStates;
using GooeyArtifacts.Items;
using RoR2.ContentManagement;
using System.Collections;

namespace GooeyArtifacts
{
    public sealed class ContentPackProvider : IContentPackProvider
    {
        readonly ContentPack _contentPack = new ContentPack();

        public string identifier => GooeyArtifactsPlugin.PluginGUID;

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
            _contentPack.identifier = identifier;

            ItemDefs.AddItemDefsTo(_contentPack.itemDefs);
            ArtifactDefs.AddArtifactDefsTo(_contentPack.artifactDefs);
            _contentPack.entityStateTypes.Add([.. EntityStateTypeAttribute.GetAllEntityStateTypes()]);

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
