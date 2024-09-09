using EntityStates.Missions.Moon;
using GooeyArtifacts.Patches;
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
        GameObject[] _pillarObjectsServer = [];
        HoldoutZoneController[] _pillarHoldoutZoneControllersServer = [];
        EntityStateMachine[] _pillarStateMachinesServer = [];

        public GameObject[] PillarObjectsServer
        {
            get
            {
                return _pillarObjectsServer;
            }
            set
            {
                if (!NetworkServer.active)
                {
                    Log.Warning("Called on client");
                    return;
                }

                if (enabled)
                {
                    unsubscribeFromHoldoutZones(_pillarHoldoutZoneControllersServer);
                }

                value ??= [];

                _pillarObjectsServer = value;
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
            if (teleporter)
            {
                if (NetworkServer.active)
                {
                    teleporter.locked = teleporter.isIdle && ChargedPillarCount < RequiredPillarCount;
                }

                bool isLocked = teleporter.locked;

                if (teleporter.TryGetComponent(out ModelLocator teleporterModelLocator))
                {
                    Transform modelTransform = teleporterModelLocator.modelTransform;
                    if (modelTransform && modelTransform.TryGetComponent(out ChildLocator childLocator))
                    {
                        GameObject timeCrystalBeaconBlocker = childLocator.FindChildGameObject("TimeCrystalBeaconBlocker");
                        if (timeCrystalBeaconBlocker)
                        {
                            timeCrystalBeaconBlocker.SetActive(isLocked);

                            if (_wasTeleporterLocked && !isLocked)
                            {
                                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/TimeCrystalDeath"), new EffectData
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
