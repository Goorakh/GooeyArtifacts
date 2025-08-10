using EntityStates.Missions.Moon;
using GooeyArtifacts.Patches;
using GooeyArtifacts.Utils;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.PillarsEveryStage
{
    public class StagePillarChargeMissionController : NetworkBehaviour
    {
        static EffectIndex _teleporterUnlockEffectIndex = EffectIndex.Invalid;

        [SystemInitializer(typeof(EffectCatalog))]
        static void Init()
        {
            AssetLoadUtils.LoadAssetTemporary<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_WeeklyRun.TimeCrystalDeath_prefab, timeCrystalDeathPrefab =>
            {
                _teleporterUnlockEffectIndex = EffectCatalog.FindEffectIndexFromPrefab(timeCrystalDeathPrefab);
                if (_teleporterUnlockEffectIndex == EffectIndex.Invalid)
                {
                    Log.Error("Failed to find teleporter unlock effect index");
                }
            });
        }

        GameObject[] _pillarObjectsServer = [];
        HoldoutZoneController[] _pillarHoldoutZoneControllersServer = [];
        EntityStateMachine[] _pillarStateMachinesServer = [];

        public GameObject[] PillarObjectsServer
        {
            get
            {
                return _pillarObjectsServer;
            }

            [Server]
            set
            {
                if (enabled)
                {
                    unsubscribeFromHoldoutZones(_pillarHoldoutZoneControllersServer);
                }

                _pillarObjectsServer = value ?? [];
                _pillarHoldoutZoneControllersServer = Array.ConvertAll(_pillarObjectsServer, g => g.GetComponent<HoldoutZoneController>());
                _pillarStateMachinesServer = Array.ConvertAll(_pillarObjectsServer, g => g.GetComponent<EntityStateMachine>());

                if (enabled)
                {
                    subscribeToHoldoutZones(_pillarHoldoutZoneControllersServer);
                }

                ChargedPillarCount = _pillarHoldoutZoneControllersServer.Count(h => h.charge >= 1f);
            }
        }

        [SyncVar]
        public int RequiredPillarCount;

        [SyncVar]
        public int ChargedPillarCount;

        bool _wasTeleporterLocked;

        TeleporterInteraction _lastObservedTeleporterInteraction;
        ChildLocator _cachedTeleporterModelChildLocator;

        void OnEnable()
        {
            ObjectivePanelController.collectObjectiveSources += collectObjectiveSources;

            ObjectiveSourceModifierHook.OverrideObjectiveSource += overrideObjectiveSource;

            if (NetworkServer.active)
            {
                subscribeToHoldoutZones(_pillarHoldoutZoneControllersServer);
            }
        }

        void OnDisable()
        {
            ObjectivePanelController.collectObjectiveSources -= collectObjectiveSources;

            ObjectiveSourceModifierHook.OverrideObjectiveSource -= overrideObjectiveSource;

            if (NetworkServer.active)
            {
                unsubscribeFromHoldoutZones(_pillarHoldoutZoneControllersServer);
            }
        }

        void OnDestroy()
        {
            if (NetworkServer.active)
            {
                foreach (GameObject pillarObject in _pillarObjectsServer)
                {
                    if (pillarObject)
                    {
                        NetworkServer.Destroy(pillarObject);
                    }
                }
            }
        }

        void collectObjectiveSources(CharacterMaster viewer, List<ObjectivePanelController.ObjectiveSourceDescriptor> objectiveSourcesList)
        {
            if (ChargedPillarCount < RequiredPillarCount)
            {
                objectiveSourcesList.Add(new ObjectivePanelController.ObjectiveSourceDescriptor
                {
                    master = viewer,
                    objectiveType = typeof(StagePillarChargeMissionObjectiveTracker),
                    source = this
                });
            }
        }

        void overrideObjectiveSource(ref ObjectivePanelController.ObjectiveSourceDescriptor descriptor, ref bool visible)
        {
            if (descriptor.objectiveType == typeof(ObjectivePanelController.FindTeleporterObjectiveTracker) && ChargedPillarCount < RequiredPillarCount)
            {
                visible = false;
            }
        }

        void subscribeToHoldoutZones(HoldoutZoneController[] holdoutZones)
        {
            if (holdoutZones is null)
                return;

            foreach (HoldoutZoneController holdoutZoneController in holdoutZones)
            {
                holdoutZoneController.onCharged.AddListener(onPillarChargedServer);
            }
        }

        void unsubscribeFromHoldoutZones(HoldoutZoneController[] holdoutZones)
        {
            if (holdoutZones is null)
                return;

            foreach (HoldoutZoneController holdoutZoneController in holdoutZones)
            {
                unsubscribeFromHoldoutZone(holdoutZoneController);
            }
        }

        void unsubscribeFromHoldoutZone(HoldoutZoneController holdoutZoneController)
        {
            holdoutZoneController.onCharged.RemoveListener(onPillarChargedServer);
        }

        void onPillarChargedServer(HoldoutZoneController holdoutZoneController)
        {
            ChargedPillarCount++;

            if (ChargedPillarCount >= RequiredPillarCount)
            {
                foreach (HoldoutZoneController pillarHoldoutZone in _pillarHoldoutZoneControllersServer)
                {
                    pillarHoldoutZone.FullyChargeHoldoutZone();
                    unsubscribeFromHoldoutZone(pillarHoldoutZone);
                }

                foreach (EntityStateMachine pillarStateMachine in _pillarStateMachinesServer)
                {
                    if (pillarStateMachine.state is not MoonBatteryComplete)
                    {
                        pillarStateMachine.SetNextState(new MoonBatteryDisabled());
                    }
                }
            }
        }

        void FixedUpdate()
        {
            TeleporterInteraction teleporter = TeleporterInteraction.instance;
            if (teleporter != _lastObservedTeleporterInteraction)
            {
                ChildLocator modelChildLocator = null;

                if (teleporter.TryGetComponent(out ModelLocator teleporterModelLocator))
                {
                    Transform modelTransform = teleporterModelLocator.modelTransform;
                    if (modelTransform && modelTransform.TryGetComponent(out ChildLocator childLocator))
                    {
                        modelChildLocator = childLocator;
                    }
                }

                _cachedTeleporterModelChildLocator = modelChildLocator;
                _lastObservedTeleporterInteraction = teleporter;
            }

            if (teleporter)
            {
                if (NetworkServer.active)
                {
                    teleporter.locked = teleporter.isIdle && ChargedPillarCount < RequiredPillarCount;
                }

                bool isLocked = teleporter.locked;

                if (_cachedTeleporterModelChildLocator)
                {
                    GameObject timeCrystalBeaconBlocker = _cachedTeleporterModelChildLocator.FindChildGameObject("TimeCrystalBeaconBlocker");
                    if (timeCrystalBeaconBlocker)
                    {
                        timeCrystalBeaconBlocker.SetActive(isLocked);

                        if (_wasTeleporterLocked && !isLocked)
                        {
                            if (_teleporterUnlockEffectIndex != EffectIndex.Invalid)
                            {
                                EffectManager.SpawnEffect(_teleporterUnlockEffectIndex, new EffectData
                                {
                                    origin = timeCrystalBeaconBlocker.transform.position
                                }, false);
                            }
                        }
                    }
                }

                _wasTeleporterLocked = isLocked;
            }
        }
    }
}
