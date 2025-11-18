using HG;
using HG.Coroutines;
using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GooeyArtifacts.Utils.Extensions
{
    public static class AssetLoadExtensions
    {
        public static void OnSuccess(this in AsyncOperationHandle handle, Action<object> onSuccess)
        {
            StackTrace stackTrace = new StackTrace();

            void handleCompleted(AsyncOperationHandle handle)
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    onSuccess(handle.Result);
                }
                else
                {
                    Log.Error($"Failed to load asset '{handle.LocationName}'. {stackTrace}");
                }
            }

            if (handle.IsDone)
            {
                handleCompleted(handle);
            }
            else
            {
                handle.Completed += handleCompleted;
            }
        }

        public static void OnSuccess<T>(this in AsyncOperationHandle<T> handle, Action<T> onSuccess)
        {
            StackTrace stackTrace = new StackTrace();

            void handleCompleted(AsyncOperationHandle<T> handle)
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    onSuccess(handle.Result);
                }
                else
                {
                    Log.Error($"Failed to load asset '{handle.LocationName}'. {stackTrace}");
                }
            }

            if (handle.IsDone)
            {
                handleCompleted(handle);
            }
            else
            {
                handle.Completed += handleCompleted;
            }
        }

        public static IEnumerator AsProgressCoroutine(this AsyncOperation asyncOperation, IProgress<float> progressReceiver)
        {
            while (!asyncOperation.isDone)
            {
                yield return null;
                progressReceiver.Report(asyncOperation.progress);
            }
        }

        public static IEnumerator AsProgressCoroutine(this AsyncOperationHandle asyncOperation, IProgress<float> progressReceiver)
        {
            while (!asyncOperation.IsDone)
            {
                yield return null;
                progressReceiver.Report(asyncOperation.PercentComplete);
            }
        }

        public static IEnumerator AsProgressCoroutine<T>(this AsyncOperationHandle<T> asyncOperation, IProgress<float> progressReceiver)
        {
            while (!asyncOperation.IsDone)
            {
                yield return null;
                progressReceiver.Report(asyncOperation.PercentComplete);
            }
        }

        public static void Add(this ParallelProgressCoroutine parallelProgressCoroutine, AsyncOperation asyncOperation)
        {
            ReadableProgress<float> progressReceiver = new ReadableProgress<float>();
            parallelProgressCoroutine.Add(asyncOperation.AsProgressCoroutine(progressReceiver), progressReceiver);
        }

        public static void Add(this ParallelProgressCoroutine parallelProgressCoroutine, AsyncOperationHandle asyncOperation)
        {
            ReadableProgress<float> progressReceiver = new ReadableProgress<float>();
            parallelProgressCoroutine.Add(asyncOperation.AsProgressCoroutine(progressReceiver), progressReceiver);
        }

        public static void Add<T>(this ParallelProgressCoroutine parallelProgressCoroutine, AsyncOperationHandle<T> asyncOperation)
        {
            ReadableProgress<float> progressReceiver = new ReadableProgress<float>();
            parallelProgressCoroutine.Add(asyncOperation.AsProgressCoroutine(progressReceiver), progressReceiver);
        }
    }
}
