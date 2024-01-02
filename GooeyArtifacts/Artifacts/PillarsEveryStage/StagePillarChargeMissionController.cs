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
        GameObject[] _pillarObjectsServer = Array.Empty<GameObject>();
        HoldoutZoneController[] _pillarHoldoutZoneControllersServer = Array.Empty<HoldoutZoneController>();
        EntityStateMachine[] _pillarStateMachinesServer = Array.Empty<EntityStateMachine>();

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

                value ??= Array.Empty<GameObject>();

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

        int _requiredPillarCount;
        const uint REQUIRED_PILLAR_COUNT_DIRTY_BIT = 1 << 0;

        public int RequiredPillarCount
        {
            get
            {
                return _requiredPillarCount;
            }
            set
            {
                SetSyncVar(value, ref _requiredPillarCount, REQUIRED_PILLAR_COUNT_DIRTY_BIT);
            }
        }

        int _chargedPillarCount;
        const uint CHARGED_PILLAR_COUNT_DIRTY_BIT = 1 << 1;

        public int ChargedPillarCount
        {
            get
            {
                return _chargedPillarCount;
            }
            set
            {
                SetSyncVar(value, ref _chargedPillarCount, CHARGED_PILLAR_COUNT_DIRTY_BIT);
            }
        }

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
                holdoutZoneController.onCharged.AddListener(onPillarChagedServer);
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
            holdoutZoneController.onCharged.RemoveListener(onPillarChagedServer);
        }

        void onPillarChagedServer(HoldoutZoneController holdoutZoneController)
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

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WritePackedUInt32((uint)_requiredPillarCount);
                writer.WritePackedUInt32((uint)_chargedPillarCount);

                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;

            if ((dirtyBits & REQUIRED_PILLAR_COUNT_DIRTY_BIT) != 0)
            {
                writer.Write((uint)_requiredPillarCount);
                anythingWritten = true;
            }

            if ((dirtyBits & CHARGED_PILLAR_COUNT_DIRTY_BIT) != 0)
            {
                writer.Write((uint)_chargedPillarCount);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _requiredPillarCount = (int)reader.ReadPackedUInt32();
                _chargedPillarCount = (int)reader.ReadPackedUInt32();

                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();

            if ((dirtyBits & REQUIRED_PILLAR_COUNT_DIRTY_BIT) != 0)
            {
                _requiredPillarCount = (int)reader.ReadPackedUInt32();
            }

            if ((dirtyBits & CHARGED_PILLAR_COUNT_DIRTY_BIT) != 0)
            {
                _chargedPillarCount = (int)reader.ReadPackedUInt32();
            }
        }
    }
}
