using System.IO;

namespace GooeyArtifacts
{
    static class LanguageFolderHandler
    {
        public static void Register(string searchFolder, string langFolderName = "lang")
        {
            string langFolderPath = Path.Combine(searchFolder, langFolderName);
            if (Directory.Exists(langFolderPath))
            {
#if DEBUG
                Log.Debug($"Found lang folder: {langFolderPath}");
#endif

                RoR2.Language.collectLanguageRootFolders += folders =>
                {
                    folders.Add(langFolderPath);
                };
            }
            else
            {
                Log.Error($"Lang folder not found: {langFolderPath}");
            }
        }
    }
}
