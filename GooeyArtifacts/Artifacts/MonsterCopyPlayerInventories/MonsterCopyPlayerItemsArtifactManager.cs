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
            RunArtifactManager.onArtifactEnabledGlobal += RunArtifactManager_onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += RunArtifactManager_onArtifactDisabledGlobal;

            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
        }

        static void RunArtifactManager_onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (!NetworkServer.active)
                return;

            if (artifactDef == ArtifactDefs.MonsterCopyPlayerItems)
            {
                if (_monsterCopyPlayerItemsControllerInstance)
                {
                    Log.Warning("Item copy controller already instantiated");
                }
                else
                {
                    _monsterCopyPlayerItemsControllerInstance = Object.Instantiate(Prefabs.MonsterCopyPlayerInventoriesControllerPrefab);
                    NetworkServer.Spawn(_monsterCopyPlayerItemsControllerInstance);
                }
            }
        }

        static void RunArtifactManager_onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (!NetworkServer.active)
                return;

            if (artifactDef == ArtifactDefs.MonsterCopyPlayerItems)
            {
                if (_monsterCopyPlayerItemsControllerInstance)
                {
                    NetworkServer.Destroy(_monsterCopyPlayerItemsControllerInstance);
                }

                _monsterCopyPlayerItemsControllerInstance = null;
            }
        }

        static void Run_onRunDestroyGlobal(Run _)
        {
            if (!NetworkServer.active)
                return;

            if (_monsterCopyPlayerItemsControllerInstance)
            {
                NetworkServer.Destroy(_monsterCopyPlayerItemsControllerInstance);
            }

            _monsterCopyPlayerItemsControllerInstance = null;
        }
    }
}
