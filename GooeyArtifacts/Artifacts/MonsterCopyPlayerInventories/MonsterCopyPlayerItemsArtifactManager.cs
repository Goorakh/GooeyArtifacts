using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.Artifacts.MonsterCopyPlayerInventories
{
    public static class MonsterCopyPlayerItemsArtifactManager
    {
        static GameObject _monsterCopyPlayerItemsControllerInstance;

        [SystemInitializer]
        static void Init()
        {
            RunArtifactManager.onArtifactEnabledGlobal += onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += onArtifactDisabledGlobal;
        }

        static void onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != ArtifactDefs.MonsterCopyPlayerItems)
                return;

            if (NetworkServer.active)
            {
                if (_monsterCopyPlayerItemsControllerInstance)
                {
                    Log.Warning("Item copy controller already instantiated");
                }
                else
                {
                    _monsterCopyPlayerItemsControllerInstance = GameObject.Instantiate(Prefabs.MonsterCopyPlayerInventoriesControllerPrefab);
                    NetworkServer.Spawn(_monsterCopyPlayerItemsControllerInstance);
                }
            }
        }

        static void onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != ArtifactDefs.MonsterCopyPlayerItems)
                return;

            GameObject.Destroy(_monsterCopyPlayerItemsControllerInstance);
            _monsterCopyPlayerItemsControllerInstance = null;
        }
    }
}
