using BepInEx.Logging;
using System.Runtime.CompilerServices;

namespace GooeyArtifacts
{
    internal static class Log
    {
        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        static string getLogPrefix(string callerPath, string callerMemberName, int callerLineNumber)
        {
            const string MOD_NAME = nameof(GooeyArtifacts);

            int modNameLastPathIndex = callerPath.LastIndexOf(MOD_NAME);
            if (modNameLastPathIndex >= 0)
            {
                callerPath = callerPath.Substring(modNameLastPathIndex + MOD_NAME.Length + 1);
            }

            return $"{callerPath}:{callerLineNumber} ({callerMemberName}) ";
        }

#if DEBUG
        internal static void Debug(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogDebug(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
        internal static void Debug_NoCallerPrefix(object data) => _logSource.LogDebug(data);
#endif

        internal static void Error(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogError(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
        internal static void Error_NoCallerPrefix(object data) => _logSource.LogError(data);

        internal static void Fatal(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogFatal(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
        internal static void Fatal_NoCallerPrefix(object data) => _logSource.LogFatal(data);

        internal static void Info(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogInfo(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
        internal static void Info_NoCallerPrefix(object data) => _logSource.LogInfo(data);

        internal static void Message(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogMessage(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
        internal static void Message_NoCallerPrefix(object data) => _logSource.LogMessage(data);

        internal static void Warning(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogWarning(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
        internal static void Warning_NoCallerPrefix(object data) => _logSource.LogWarning(data);
    }
}