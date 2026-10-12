using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    public enum LaneType { Grass, Road, River, Rail }

    /// <summary>
    /// One lane strip: state + queries + hazard checks for the player standing on it.
    /// Building is delegated to the LaneBuilders; only the lane we occupy runs its checks
    /// (SetPlayer/ClearPlayer) — no global polling.
    /// Deterministic: movement + hazard checks run on Sim logic ticks, not frame Update.
    /// </summary>
    public class Lane : MonoBehaviour
    {
        public LaneType Type { get; private set; }
        public int Z { get; private set; }

        private readonly List<int> trees = new List<int>();
        private readonly List<MovingObject> cars = new List<MovingObject>();
        private readonly List<MovingObject> logs = new List<MovingObject>();
        private Train train;
        private CrossRoadEvents events;
        private PlayerController player;   // non-null only while the chicken stands here

        public void Init(LaneType type, int z, int dir, bool centreLine, CrossRoadEvents events)
        {
            Type = type;
            Z = z;
            this.events = events;
            name = "Lane_" + z + "_" + type;

            switch (type)
            {
                case LaneType.Grass: GrassBuilder.Build(transform, z, trees); break;
                case LaneType.Road: RoadBuilder.Build(transform, z, dir, centreLine, cars); break;
                case LaneType.River: RiverBuilder.Build(transform, logs); break;
                case LaneType.Rail: train = RailBuilder.Build(transform); break;
            }

            Sim.Stepped += OnSimTick;   // deterministic world stepping + hazard checks
        }

        private void OnDestroy()
        {
            Sim.Stepped -= OnSimTick;
        }

        /// <summary>
        /// One deterministic logic tick: advance cars/logs, then run hazard checks for
        /// the player standing here.
        /// </summary>
        private void OnSimTick(float dt)
        {
            for (int i = 0; i < cars.Count; i++) cars[i].SimTick(dt);
            for (int i = 0; i < logs.Count; i++) logs[i].SimTick(dt);

            if (player == null || player.Dead) return;

            if (Type == LaneType.Road)
            {
                for (int i = 0; i < cars.Count; i++)
                {
                    if (Mathf.Abs(cars[i].transform.localPosition.x - player.WorldX) < CrossRoadConfig.CarHitRadius)
                    {
                        events.DeathRequested.Raise(LKey.DeathCar, DeathKind.Squash);
                        return;
                    }
                }
            }
            else if (Type == LaneType.Rail && train != null && train.IsPassing)
            {
                if (Mathf.Abs(train.X - player.WorldX) < train.DangerHalfLen + CrossRoadConfig.TrainHitMargin)
                    events.DeathRequested.Raise(LKey.DeathTrain, DeathKind.Squash);
            }
        }

        public bool IsBlocked(int x) => trees.Contains(x);

        public MovingObject LogAt(float x)
        {
            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                if (Mathf.Abs(log.transform.localPosition.x - x) <= log.HalfLen + 0.30f) return log;
            }
            return null;
        }

        public void SetPlayer(PlayerController p) => player = p;

        public void ClearPlayer(PlayerController p)
        {
            if (player == p) player = null;
        }

        public void Recycle()
        {
            player = null;
            Destroy(gameObject);
        }
    }
}
