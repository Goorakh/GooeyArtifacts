using EntityStates;
using GooeyArtifacts.Artifacts.MonsterCopyPlayerInventories;
using GooeyArtifacts.Artifacts.MovingInteractables;
using GooeyArtifacts.Artifacts.PillarsEveryStage;
using GooeyArtifacts.EntityStates.MovingInteractables;
using GooeyArtifacts.Utils;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts
{
    public static class Prefabs
    {
        public static GameObject MonsterCopyPlayerInventoriesControllerPrefab { get; private set; }

        public static GameObject StagePillarChargeMissionControllerPrefab { get; private set; }

        public static GameObject SyncExternalNetObjectTransformPrefab { get; private set; }

        public static GameObject InteractableMoveControllerPrefab { get; private set; }

        static GameObject createPrefab(string name, bool networked)
        {
            GameObject tmp = new GameObject();

            if (networked)
            {
                tmp.AddComponent<NetworkIdentity>();
            }

            GameObject prefab = tmp.InstantiateClone(name, networked);
            GameObject.Destroy(tmp);
            return prefab;
        }

        internal static void Init()
        {
            // MonsterCopyPlayerInventoriesControllerPrefab
            {
                MonsterCopyPlayerInventoriesControllerPrefab = createPrefab("MonsterCopyPlayerInventoriesController", true);

                MonsterCopyPlayerInventoriesControllerPrefab.AddComponent<SetDontDestroyOnLoad>();
                MonsterCopyPlayerInventoriesControllerPrefab.AddComponent<Inventory>();
                MonsterCopyPlayerInventoriesControllerPrefab.AddComponent<CopyPlayerInventories>();

                TeamFilter teamFilter = MonsterCopyPlayerInventoriesControllerPrefab.AddComponent<TeamFilter>();
                teamFilter.teamIndex = TeamIndex.None;

                MonsterCopyPlayerInventoriesControllerPrefab.AddComponent<EnemyInfoPanelInventoryProvider>();
            }

            // StagePillarChargeMissionControllerPrefab
            {
                StagePillarChargeMissionControllerPrefab = createPrefab("PillarChargeMissionController", true);

                StagePillarChargeMissionControllerPrefab.AddComponent<StagePillarChargeMissionController>();
            }

            // SyncExternalNetObjectTransformPrefab
            {
                SyncExternalNetObjectTransformPrefab = createPrefab("SyncExternalNetObjectTransform", true);

                NetworkTransform networkTransform = SyncExternalNetObjectTransformPrefab.AddComponent<NetworkTransform>();
                networkTransform.transformSyncMode = NetworkTransform.TransformSyncMode.SyncTransform;
                networkTransform.sendInterval = 1f / 15f;

                SyncExternalNetObjectTransformPrefab.AddComponent<SyncExternalNetworkedObjectTransform>();
            }

            // InteractableMoveControllerPrefab
            {
                InteractableMoveControllerPrefab = createPrefab("InteractableMoveController", false);

                EntityStateMachine stateMachine = InteractableMoveControllerPrefab.AddComponent<EntityStateMachine>();
                stateMachine.initialStateType = new SerializableEntityStateType(typeof(MovingInteractableRestState));
                stateMachine.mainStateType = new SerializableEntityStateType(typeof(MovingInteractableRestState));

                InteractableMoveControllerPrefab.AddComponent<InteractableMoveController>();
            }
        }
    }
}
