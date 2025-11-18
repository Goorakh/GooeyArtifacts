using RoR2.ContentManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GooeyArtifacts.Utils
{
    public static class AssetLoadUtils
    {
        public static AsyncOperationHandle<T> LoadTempAssetAsync<T>(string assetGuid) where T : UnityEngine.Object
        {
            return LoadTempAssetAsync(new AssetReferenceT<T>(assetGuid));
        }

        public static AsyncOperationHandle<T> LoadTempAssetAsync<T>(AssetReferenceT<T> assetReference) where T : UnityEngine.Object
        {
            AsyncOperationHandle<T> assetLoadHandle = AssetAsyncReferenceManager<T>.LoadAsset(assetReference);
            assetLoadHandle.Completed += handle =>
            {
                AssetAsyncReferenceManager<T>.UnloadAsset(assetReference);
            };

            return assetLoadHandle;
        }
    }
}
