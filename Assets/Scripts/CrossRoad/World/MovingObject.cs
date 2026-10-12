using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Shared driver for cars and river logs: constant drift along X, wrapping around.
    /// Deterministic: advances only on Sim logic ticks (dt is the fixed tick delta,
    /// never Time.deltaTime) — identical on every client.
    /// </summary>
    public class MovingObject : MonoBehaviour
    {
        public float HalfLen { get; private set; }

        private float speed;
        private float bound;

        public void Init(float speed, float bound, float halfLen)
        {
            this.speed = speed;
            this.bound = bound;
            HalfLen = halfLen;
        }

        /// <summary>One deterministic logic step (driven by the owning Lane's Sim tick).</summary>
        public void SimTick(float dt)
        {
            float x = transform.localPosition.x + speed * dt;
            float limit = bound + HalfLen;
            if (x > limit) x -= 2f * limit;
            else if (x < -limit) x += 2f * limit;

            var p = transform.localPosition;
            p.x = x;
            transform.localPosition = p;
        }
    }
}
