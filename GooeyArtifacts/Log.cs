using BepInEx.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace GooeyArtifacts
{
    internal static class Log
    {
        static readonly StringBuilder _sharedStringBuilder = new StringBuilder(256);
        static readonly object _stringBuilderLock = new object();

        static readonly int _cachedCallerPathPrefixLength;

        static ManualLogSource _logSource;

        static Log()
        {
            _cachedCallerPathPrefixLength = getCallerPathPrefixLength();

            static int getCallerPathPrefixLength([CallerFilePath] string callerPath = null)
            {
                const string MOD_NAME = nameof(GooeyArtifacts) + @"\";

                int modNameLastPathIndex = callerPath.LastIndexOf(MOD_NAME);
                if (modNameLastPathIndex >= 0)
                {
                    return modNameLastPathIndex + MOD_NAME.Length;
                }
                else
                {
                    UnityEngine.Debug.LogError($"[{GooeyArtifactsPlugin.PluginName}] Logger failed to determine caller path prefix length");
                    return 0;
                }
            }
        }

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        static string buildCallerLogString(string callerPath, string callerMemberName, int callerLineNumber, object data)
        {
            lock (_stringBuilderLock)
            {
                return _sharedStringBuilder.Clear()
                                           .Append(callerPath, _cachedCallerPathPrefixLength, callerPath.Length - _cachedCallerPathPrefixLength)
                                           .Append(':').Append(callerLineNumber)
                                           .Append(" (").Append(callerMemberName).Append("): ")
                                           .Append(data)
                                           .ToString();
            }
        }

        [Conditional("DEBUG")]
        internal static void Debug(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogDebug(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEBUG")]
        internal static void Debug_NoCallerPrefix(object data)
        {
            _logSource.LogDebug(data);
        }

        internal static void Error(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogError(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Error_NoCallerPrefix(object data)
        {
            _logSource.LogError(data);
        }

        internal static void Fatal(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogFatal(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Fatal_NoCallerPrefix(object data)
        {
            _logSource.LogFatal(data);
        }

        internal static void Info(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogInfo(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Info_NoCallerPrefix(object data)
        {
            _logSource.LogInfo(data);
        }

        internal static void Message(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogMessage(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Message_NoCallerPrefix(object data)
        {
            _logSource.LogMessage(data);
        }

        internal static void Warning(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogWarning(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Warning_NoCallerPrefix(object data)
        {
            _logSource.LogWarning(data);
        }

        internal static void LogType(LogLevel level, object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
#if !DEBUG
            if ((level & LogLevel.Debug) != 0)
                return;
#endif

            _logSource.Log(level, buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogType_NoCallerPrefix(LogLevel level, object data)
        {
#if !DEBUG
            if ((level & LogLevel.Debug) != 0)
                return;
#endif

            _logSource.Log(level, data);
        }
    }
}