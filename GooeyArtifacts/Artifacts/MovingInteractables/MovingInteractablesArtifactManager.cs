using GooeyArtifacts.Utils;
using HG;
using RoR2;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.MovingInteractables
{
    public static class MovingInteractablesArtifactManager
    {
        static readonly SpawnCard[] _nonInteractableSpawnCardWhitelist = [
            Addressables.LoadAssetAsync<SpawnCard>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC2.scHalcyonShardA_asset).WaitForCompletion(),
            Addressables.LoadAssetAsync<SpawnCard>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC2.scHalcyonShardB_asset).WaitForCompletion(),
            Addressables.LoadAssetAsync<SpawnCard>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC2.scHalcyonShardC_asset).WaitForCompletion(),
        ];

        static readonly SpawnCard[] _spawnCardBlacklist = [
            Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/iscInfiniteTowerSafeWard.asset").WaitForCompletion(),
            Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/iscInfiniteTowerSafeWardAwaitingInteraction.asset").WaitForCompletion()
        ];

        [SystemInitializer]
        static void Init()
        {
            SpawnCard.onSpawnedServerGlobal += SpawnCard_onSpawnedServerGlobal;

            SceneCatalog.onMostRecentSceneDefChanged += onMostRecentSceneDefChanged;

            RunArtifactManager.onArtifactEnabledGlobal += onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += onArtifactDisabledGlobal;

            void addMovableComponentToPrefab(string assetGuid)
            {
                AssetLoadUtils.LoadAssetTemporary<GameObject>(assetGuid, prefab =>
                {
                    prefab.EnsureComponent<MovableInteractable>();
                });
            }

            addMovableComponentToPrefab(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorPod_prefab);
            addMovableComponentToPrefab(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_SurvivorPod.SurvivorPod_prefab);
        }

        static void SpawnCard_onSpawnedServerGlobal(SpawnCard.SpawnResult spawnResult)
        {
            if (!NetworkServer.active || !spawnResult.success)
                return;

            if (spawnResult.spawnRequest is null)
                return;

            SpawnCard spawnCard = spawnResult.spawnRequest.spawnCard;
            if ((spawnCard is not InteractableSpawnCard && Array.IndexOf(_nonInteractableSpawnCardWhitelist, spawnCard) == -1) ||
                Array.IndexOf(_spawnCardBlacklist, spawnCard) != -1)
            {
                return;
            }

            if (!spawnResult.spawnedInstance.GetComponent<NetworkIdentity>())
            {
                Log.Debug($"Spawned object {spawnResult.spawnedInstance} is not networked");
                return;
            }

            MovableInteractable movableInteractable = spawnResult.spawnedInstance.AddComponent<MovableInteractable>();
            movableInteractable.SpawnCard = spawnCard;
        }

        static void onMostRecentSceneDefChanged(SceneDef scene)
        {
            IEnumerator waitForSceneLoadThenInitSceneMovables()
            {
                yield return new WaitForEndOfFrame();

                // pillars?
                // eggs

                MonoBehaviour[] movableSceneObjectComponents = [
                    .. InstanceTracker.GetInstancesList<PurchaseInteraction>(),
                    .. InstanceTracker.GetInstancesList<TimedChestController>(),
                    .. InstanceTracker.GetInstancesList<GeodeController>(),
                    .. InstanceTracker.GetInstancesList<SceneExitController>(),
                ];

                foreach (MonoBehaviour component in movableSceneObjectComponents)
                {
                    GameObject sceneObject = component ? component.gameObject : null;
                    if (!sceneObject)
                        continue;

                    if (!sceneObject.TryGetComponent(out NetworkIdentity networkIdentity) || networkIdentity.sceneId.IsEmpty())
                        continue;

                    if (sceneObject.GetComponent<MeridianEventTriggerInteraction>())
                        continue;

                    if (sceneObject.GetComponent<MovableInteractable>())
                        continue;

                    sceneObject.AddComponent<MovableInteractable>();

                    Log.Debug($"Added scene movable: {Util.BuildPrefabTransformPath(sceneObject.transform.root, sceneObject.transform, false, true)}");
                }
            }

            Main.Instance.StartCoroutine(waitForSceneLoadThenInitSceneMovables());
        }

        static void onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (!NetworkServer.active)
                return;

            if (artifactDef != ArtifactDefs.MovingInteractables)
                return;

            foreach (MovableInteractable movable in InstanceTracker.GetInstancesList<MovableInteractable>())
            {
                setupMovingInteractable(movable);
            }

            MovableInteractable.OnMovableInteractableCreated += setupMovingInteractable;
        }

        static void onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (!NetworkServer.active)
                return;

            if (artifactDef != ArtifactDefs.MovingInteractables)
                return;

            MovableInteractable.OnMovableInteractableCreated -= setupMovingInteractable;

            foreach (MovableInteractable movable in InstanceTracker.GetInstancesList<MovableInteractable>())
            {
                cleanupMovingInteractable(movable);
            }
        }

        static void setupMovingInteractable(MovableInteractable movable)
        {
            if (!movable.TryGetComponent(out NetworkIdentity networkIdentity) || networkIdentity.netId.IsEmpty())
            {
                Log.Warning($"Cannot set up moving object: {movable}, it has not been spawned on the network");
                return;
            }

            if (movable.IsClaimed)
            {
                Log.Warning($"Attempted to setup movement several times! obj={movable.name}");
                return;
            }

            if (NetworkServer.active)
            {
                if (movable.MoveController)
                {
                    movable.MoveController.UnmarkForCleanup();
                }
                else
                {
                    GameObject syncNetObjectTransformObj = GameObject.Instantiate(Prefabs.SyncExternalNetObjectTransformPrefab);

                    SyncExternalNetworkedObjectTransform interactableTransformSyncController = syncNetObjectTransformObj.GetComponent<SyncExternalNetworkedObjectTransform>();
                    interactableTransformSyncController.TargetObject = movable.gameObject;

                    NetworkServer.Spawn(syncNetObjectTransformObj);

                    GameObject moveControllerObject = GameObject.Instantiate(Prefabs.InteractableMoveControllerPrefab);

                    InteractableMoveController moveController = moveControllerObject.GetComponent<InteractableMoveController>();
                    moveController.SpawnCardServer = movable.SpawnCard;
                    moveController.TransformSyncController = interactableTransformSyncController;

                    NetworkServer.Spawn(moveControllerObject);

                    movable.MoveController = moveController;
                }
            }

            movable.IsClaimed = true;
        }

        static void cleanupMovingInteractable(MovableInteractable movableInteractable)
        {
            if (!movableInteractable.IsClaimed)
                return;

            if (movableInteractable.MoveController)
            {
                movableInteractable.MoveController.MarkForCleanup();
            }

            movableInteractable.IsClaimed = false;
        }
    }
}
