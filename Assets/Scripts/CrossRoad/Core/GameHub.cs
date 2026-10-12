using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Game state only: alive/dead, death reason, score. Listens to events,
    /// owns nothing, drives nothing. (The old GameManager's one surviving concern.)
    /// </summary>
    public class GameHub : MonoBehaviour
    {
        public bool IsDead { get; private set; }
        public string DeathReason { get; private set; }
        public int Score { get; private set; }

        /// <summary>False while menus are up (the world runs, the chicken waits).</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Exposed read-only wiring: tests and tools can Raise Restarted / subscribe
        /// to DeathRequested without holding scene references to any system.</summary>
        public CrossRoadEvents Events { get; private set; }

        public void Init(CrossRoadEvents events)
        {
            Events = events;
            events.GameStarted.AddListener(() => IsPlaying = true);
            events.DeathRequested.AddListener(OnDeathRequested);
            events.ScoreChanged.AddListener(OnScoreChanged);
            events.Restarted.AddListener(OnRestarted);
        }

        private void OnDeathRequested(string reason, DeathKind kind)
        {
            if (IsDead) return;
            IsDead = true;
            DeathReason = reason;
        }

        private void OnScoreChanged(int score) => Score = score;

        private void OnRestarted()
        {
            IsDead = false;
            DeathReason = null;
            Score = 0;
        }
    }
}
