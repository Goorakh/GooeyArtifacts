using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.MovingInteractables
{
    public static class MovingInteractablesArtifactManager
    {
        static readonly InteractableSpawnCard[] _spawnCardBlacklist = [
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
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();

                List<GameObject> movableSceneObjects = [
                    .. GameObject.FindObjectsOfType<PortalStatueBehavior>()
                                 .Where(p => p.portalType == PortalStatueBehavior.PortalType.Shop)
                                 .Select(c => c.gameObject)
                ];

                void addObjectsWithComponent<TComponent>(string holderPath) where TComponent : Component
                {
                    GameObject holder = GameObject.Find(holderPath);
                    if (!holder)
                        return;

                    movableSceneObjects.AddRange(holder.GetComponentsInChildren<TComponent>().Select(c => c.gameObject));
                }

                switch (scene.cachedName)
                {
                    case "frozenwall":
                        addObjectsWithComponent<PurchaseInteraction>("PERMUTATION: Human Fan");

                        addObjectsWithComponent<TimedChestController>("HOLDER: Timed Chests");

                        break;
                    case "dampcavesimple":
                        GameObject goldChest = GameObject.Find("HOLDER: Newt Statues and Preplaced Chests/GoldChest");
                        if (goldChest)
                        {
                            movableSceneObjects.Add(goldChest);
                        }

                        break;
                    case "goldshores":
                        addObjectsWithComponent<ChestBehavior>("HOLDER: Preplaced Goodies");

                        break;
                    case "moon2":
                        addObjectsWithComponent<HoldoutZoneController>("HOLDER: Pillars");

                        addObjectsWithComponent<ChestBehavior>("HOLDER: Gameplay Space/HOLDER: STATIC MESH/Quadrant 3: Greenhouse/Q3_OuterRing/Island Q3:Greenhouse/Greenhouse/Bud Holder");

                        addObjectsWithComponent<ShopTerminalBehavior>("HOLDER: Gameplay Space/HOLDER: STATIC MESH/Quadrant 2: Workshop/Q2_OuterRing/Island Q2: WorkshopInteriors/Cauldrons");

                        break;
                    case "rootjungle":
                        addObjectsWithComponent<ChestBehavior>("HOLDER: Randomization/GROUP: Large Treasure Chests");

                        break;
                }

                foreach (GameObject sceneObject in movableSceneObjects)
                {
                    if (!sceneObject || !sceneObject.activeSelf)
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
