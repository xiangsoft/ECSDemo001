using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Game
{
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Warn = 2,
        Info = 3,
        Debug = 4,
    }

    /// <summary>
    /// Central logging facade — one switch to control everything.
    /// Level gate (None silences all) plus optional per-channel toggles
    /// ("net", "game", "ui" ...). Output still lands in the Unity console.
    /// Usage: GameLog.Info("net", "connected") · GameLog.SetChannel("net", false) · GameLog.Level = LogLevel.None
    /// </summary>
    public static class GameLog
    {
        private static readonly Dictionary<string, bool> Channels = new Dictionary<string, bool>();

        /// <summary>Global gate. None silences everything, including errors.</summary>
        public static LogLevel Level { get; set; } = LogLevel.Debug;

        /// <summary>Toggle one channel off/on. Unknown channels default to on.</summary>
        public static void SetChannel(string channel, bool enabled) => Channels[channel] = enabled;

        public static void Error(string channel, string message)
        {
            if (Pass(LogLevel.Error, channel)) UnityEngine.Debug.LogError($"[{channel}] {message}");
        }

        public static void Warn(string channel, string message)
        {
            if (Pass(LogLevel.Warn, channel)) UnityEngine.Debug.LogWarning($"[{channel}] {message}");
        }

        public static void Info(string channel, string message)
        {
            if (Pass(LogLevel.Info, channel)) UnityEngine.Debug.Log($"[{channel}] {message}");
        }

        public static void Debug(string channel, string message)
        {
            if (Pass(LogLevel.Debug, channel)) UnityEngine.Debug.Log($"[{channel}] {message}");
        }

        private static bool Pass(LogLevel level, string channel) =>
            level <= Level && (!Channels.TryGetValue(channel, out var on) || on);
    }
}
