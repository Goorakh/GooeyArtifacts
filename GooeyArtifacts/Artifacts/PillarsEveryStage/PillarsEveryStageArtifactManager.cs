using R2API;
using RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.PillarsEveryStage
{
    public static class PillarsEveryStageArtifactManager
    {
        const int PILLAR_SPAWN_COUNT = 6;
        const int REQUIRED_PILLAR_COUNT = 4;

        static InteractableSpawnCard[] _pillarSpawnCards;

        static StagePillarChargeMissionController _pillarChargeMissionController;

        [SystemInitializer]
        static void Init()
        {
            RunArtifactManager.onArtifactEnabledGlobal += RunArtifactManager_onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += RunArtifactManager_onArtifactDisabledGlobal;

            On.RoR2.SceneDirector.PlaceTeleporter += SceneDirector_PlaceTeleporter;

            static InteractableSpawnCard createPillarSpawnCard(string addressablePath)
            {
                InteractableSpawnCard spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();

                GameObject prefab = Addressables.LoadAssetAsync<GameObject>(addressablePath).WaitForCompletion();
                string name = prefab.name + "_StagePillar";

                prefab = prefab.InstantiateClone(Main.PluginGUID + "_" + name);

                CombatDirector combatDirector = prefab.GetComponent<CombatDirector>();
                combatDirector.monsterCredit = 450f;

                spawnCard.name = "isc" + name;

                spawnCard.prefab = prefab;
                spawnCard.sendOverNetwork = true;
                spawnCard.hullSize = HullClassification.BeetleQueen;
                spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
                spawnCard.requiredFlags = NodeFlags.TeleporterOK;
                spawnCard.forbiddenFlags = NodeFlags.NoChestSpawn;
                spawnCard.occupyPosition = true;

                return spawnCard;
            }

            _pillarSpawnCards = [
                createPillarSpawnCard("RoR2/Base/moon2/MoonBatteryBlood.prefab"),
                createPillarSpawnCard("RoR2/Base/moon2/MoonBatteryDesign.prefab"),
                createPillarSpawnCard("RoR2/Base/moon2/MoonBatteryMass.prefab"),
                createPillarSpawnCard("RoR2/Base/moon2/MoonBatterySoul.prefab")
            ];
        }

        static bool isValidStageForPillarSpawns()
        {
            return DirectorCore.instance && DirectorCore.instance.TryGetComponent(out SceneDirector sceneDirector) && sceneDirector.teleporterSpawnCard;
        }

        static void RunArtifactManager_onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (!NetworkServer.active)
                return;

            if (artifactDef == ArtifactDefs.PillarsEveryStage)
            {
                if (isValidStageForPillarSpawns())
                {
                    initializePillars(new Xoroshiro128Plus(Run.instance.stageRng));
                }
            }
        }

        static void RunArtifactManager_onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (!NetworkServer.active)
                return;

            if (artifactDef == ArtifactDefs.PillarsEveryStage)
            {
                destroyPillars();
            }
        }

        static void SceneDirector_PlaceTeleporter(On.RoR2.SceneDirector.orig_PlaceTeleporter orig, SceneDirector self)
        {
            orig(self);

            if (!self.teleporterSpawnCard)
                return;

            if (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(ArtifactDefs.PillarsEveryStage))
                return;

            initializePillars(new Xoroshiro128Plus(self.rng));
        }

        static void initializePillars(Xoroshiro128Plus rng)
        {
            if (_pillarChargeMissionController)
                return;

            List<GameObject> createdPillarObjects = new List<GameObject>(PILLAR_SPAWN_COUNT);

            int pillarTypeCount = _pillarSpawnCards.Length;
            int[] pillarTypeSpawnCount = new int[pillarTypeCount];
            WeightedSelection<int> spawnCardSelection = new WeightedSelection<int>(pillarTypeCount);

            for (int i = 0; i < PILLAR_SPAWN_COUNT; i++)
            {
                spawnCardSelection.Clear();
                for (int j = 0; j < pillarTypeCount; j++)
                {
                    float weight = 1f - (pillarTypeSpawnCount[j] / (float)PILLAR_SPAWN_COUNT);
                    if (weight > 0f)
                    {
                        spawnCardSelection.AddChoice(j, weight);
                    }
                }

                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    placementMode = SceneInfo.instance && SceneInfo.instance.approximateMapBoundMesh ? DirectorPlacementRule.PlacementMode.RandomNormalized : DirectorPlacementRule.PlacementMode.Random
                };

                int pillarIndex = spawnCardSelection.Evaluate(rng.nextNormalizedFloat);
                DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_pillarSpawnCards[pillarIndex], placementRule, rng);

                GameObject pillarObject = DirectorCore.instance.TrySpawnObject(spawnRequest);
                if (pillarObject)
                {
                    createdPillarObjects.Add(pillarObject);
                    pillarTypeSpawnCount[pillarIndex]++;
                }
            }

            if (createdPillarObjects.Count > 0)
            {
                GameObject pillarChargeMissionControllerObject = Object.Instantiate(Prefabs.StagePillarChargeMissionControllerPrefab);
                _pillarChargeMissionController = pillarChargeMissionControllerObject.GetComponent<StagePillarChargeMissionController>();
                _pillarChargeMissionController.PillarObjectsServer = [.. createdPillarObjects];
                _pillarChargeMissionController.RequiredPillarCount = REQUIRED_PILLAR_COUNT;

                NetworkServer.Spawn(pillarChargeMissionControllerObject);
            }
        }

        static void destroyPillars()
        {
            if (_pillarChargeMissionController)
            {
                NetworkServer.Destroy(_pillarChargeMissionController.gameObject);
            }
        }
    }
}
