using RoR2.ContentManagement;
using System;
using System.Diagnostics;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GooeyArtifacts.Utils
{
    public static class AssetLoadUtils
    {
        public static void LoadAssetTemporary<T>(string assetGuid, Action<T> onLoaded) where T : UnityEngine.Object
        {
            StackTrace stackTrace = new StackTrace();

            AssetReferenceT<T> assetReference = new AssetReferenceT<T>(assetGuid);
            AsyncOperationHandle<T> assetLoadHandle = AssetAsyncReferenceManager<T>.LoadAsset(assetReference, AsyncReferenceHandleUnloadType.Preload);
            assetLoadHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    onLoaded?.Invoke(handle.Result);
                }
                else
                {
                    Log.Error_NoCallerPrefix($"Failed to load asset {assetGuid} ({typeof(T).FullName}) {stackTrace}");
                }

                AssetAsyncReferenceManager<T>.UnloadAsset(assetReference);
            };
        }
    }
}
