using GooeyArtifacts.Utils;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.MovingInteractables
{
    public static class MovingInteractablesArtifactManager
    {
        static readonly InteractableSpawnCard[] _spawnCardBlacklist = new InteractableSpawnCard[]
        {
            Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/iscInfiniteTowerSafeWard.asset").WaitForCompletion(),
            Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/iscInfiniteTowerSafeWardAwaitingInteraction.asset").WaitForCompletion()
        };

        [SystemInitializer]
        static void Init()
        {
            SpawnCard.onSpawnedServerGlobal += SpawnCard_onSpawnedServerGlobal;

            RunArtifactManager.onArtifactEnabledGlobal += onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += onArtifactDisabledGlobal;
        }

        static void SpawnCard_onSpawnedServerGlobal(SpawnCard.SpawnResult spawnResult)
        {
            if (!NetworkServer.active || !spawnResult.success)
                return;

            if (spawnResult.spawnRequest is null || spawnResult.spawnRequest.spawnCard is not InteractableSpawnCard spawnCard)
                return;

            if (Array.IndexOf(_spawnCardBlacklist, spawnCard) != -1)
                return;

            if (!spawnResult.spawnedInstance.GetComponent<NetworkIdentity>())
            {
#if DEBUG
                Log.Debug($"Spawned object {spawnResult.spawnedInstance} is not networked");
#endif
                return;
            }

            MovableInteractable movableInteractable = spawnResult.spawnedInstance.AddComponent<MovableInteractable>();
            movableInteractable.SpawnCard = spawnCard;
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

            GameObject moveControllerObject = GameObject.Instantiate(Prefabs.InteractableMoveControllerPrefab);

            InteractableMoveController moveController = moveControllerObject.GetComponent<InteractableMoveController>();
            moveController.SpawnCardServer = movable.SpawnCard;
            moveController.InteractableObject = movable.gameObject;

            movable.IsClaimed = true;
        }
    }
}
