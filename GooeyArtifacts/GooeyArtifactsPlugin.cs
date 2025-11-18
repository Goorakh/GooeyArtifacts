using BepInEx;
using System.Diagnostics;
using System.IO;

namespace GooeyArtifacts
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID)]
    public sealed class GooeyArtifactsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "GooeyArtifacts";
        public const string PluginVersion = "1.1.1";

        ContentPackProvider _contentPackProvider;

        internal static GooeyArtifactsPlugin Instance { get; private set; }

        public static string PluginDirectory { get; private set; }

        void Awake()
        {
            Instance = this;

            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            _contentPackProvider = new ContentPackProvider();
            _contentPackProvider.Register();

            Prefabs.Init();

            PluginDirectory = Path.GetDirectoryName(Info.Location);
            LanguageFolderHandler.Register(PluginDirectory);

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }
    }
}
