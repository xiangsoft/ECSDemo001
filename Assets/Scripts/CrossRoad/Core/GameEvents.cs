using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>How the chicken dies — picks the death animation.</summary>
    public enum DeathKind { Squash, Drown, Fade }

    /// <summary>
    /// Runtime-only ScriptableObject event channel (no asset on disk — created via
    /// CreateInstance, so the NO-ASSETS brief still holds). Systems talk through
    /// these instead of singletons or direct references.
    /// Note: Unity cannot CreateInstance generic ScriptableObjects — hence the
    /// concrete channel types below instead of GameEvent&lt;T&gt;.
    /// </summary>
    public class GameEvent : ScriptableObject
    {
        private readonly List<Action> listeners = new List<Action>();

        public void AddListener(Action listener)
        {
            if (listener != null && !listeners.Contains(listener)) listeners.Add(listener);
        }

        public void RemoveListener(Action listener) => listeners.Remove(listener);

        public void Raise()
        {
            for (int i = listeners.Count - 1; i >= 0; i--) listeners[i]();
        }
    }

    /// <summary>Event channel carrying an int (score).</summary>
    public class IntGameEvent : ScriptableObject
    {
        private readonly List<Action<int>> listeners = new List<Action<int>>();

        public void AddListener(Action<int> listener)
        {
            if (listener != null && !listeners.Contains(listener)) listeners.Add(listener);
        }

        public void RemoveListener(Action<int> listener) => listeners.Remove(listener);

        public void Raise(int value)
        {
            for (int i = listeners.Count - 1; i >= 0; i--) listeners[i](value);
        }
    }

    /// <summary>Event channel carrying a death (reason + kind).</summary>
    public class DeathGameEvent : ScriptableObject
    {
        private readonly List<Action<string, DeathKind>> listeners = new List<Action<string, DeathKind>>();

        public void AddListener(Action<string, DeathKind> listener)
        {
            if (listener != null && !listeners.Contains(listener)) listeners.Add(listener);
        }

        public void RemoveListener(Action<string, DeathKind> listener) => listeners.Remove(listener);

        public void Raise(string reason, DeathKind kind)
        {
            for (int i = listeners.Count - 1; i >= 0; i--) listeners[i](reason, kind);
        }
    }

    /// <summary>
    /// The wires of this game. Owned by GameBootstrap, injected into every system.
    /// GameStarted: menus -> gameplay (raised by the host screen today, by the net layer tomorrow).
    /// DeathRequested: anyone may raise it (lane, player, camera); HUD, death FX, hub and
    /// camera react independently. Restarted: raised by the R key (or tests); every system
    /// resets itself. ScoreChanged: raised by the player on reaching a new furthest lane.
    /// </summary>
    public class CrossRoadEvents
    {
        public readonly GameEvent GameStarted =
            ScriptableObject.CreateInstance<GameEvent>();

        public readonly DeathGameEvent DeathRequested =
            ScriptableObject.CreateInstance<DeathGameEvent>();

        public readonly GameEvent Restarted =
            ScriptableObject.CreateInstance<GameEvent>();

        public readonly IntGameEvent ScoreChanged =
            ScriptableObject.CreateInstance<IntGameEvent>();

        /// Play-mode teardown so nothing survives between sessions (domain-reload-off safe).
        public void Dispose()
        {
            if (GameStarted != null) UnityEngine.Object.Destroy(GameStarted);
            if (DeathRequested != null) UnityEngine.Object.Destroy(DeathRequested);
            if (Restarted != null) UnityEngine.Object.Destroy(Restarted);
            if (ScoreChanged != null) UnityEngine.Object.Destroy(ScoreChanged);
        }
    }
}
