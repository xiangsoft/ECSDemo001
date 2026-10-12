using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Death animation: squash / sink / shrink, driven purely by the DeathRequested event.
    /// Also restores the rig on restart.
    /// </summary>
    public class PlayerDeathFx : MonoBehaviour
    {
        private CrossRoadEvents events;
        private DeathKind kind;
        private bool dying;
        private float deathT;
        private Vector3 deathPos;

        public void Init(CrossRoadEvents events)
        {
            this.events = events;
            events.DeathRequested.AddListener(OnDeathRequested);
            events.Restarted.AddListener(OnRestarted);
        }

        private void OnDeathRequested(string reason, DeathKind kind)
        {
            if (dying) return;   // idempotent: the first death wins
            dying = true;
            this.kind = kind;
            deathT = 0f;
            deathPos = transform.position;
        }

        private void OnRestarted()
        {
            dying = false;
            deathT = 0f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (!dying) return;

            deathT += Time.deltaTime;
            float t = Mathf.Clamp01(deathT / 0.35f);
            switch (kind)
            {
                case DeathKind.Squash:   // squashed flat
                    transform.localScale = new Vector3(1f + 0.7f * t, Mathf.Max(0.10f, 1f - 0.92f * t), 1f + 0.7f * t);
                    break;
                case DeathKind.Drown:    // sank under the water
                    transform.position = new Vector3(deathPos.x, Mathf.Lerp(deathPos.y, -0.55f, t), deathPos.z);
                    break;
                default:                 // swept off screen
                    transform.localScale = Vector3.one * Mathf.Max(0.01f, 1f - t);
                    break;
            }
        }
    }
}
