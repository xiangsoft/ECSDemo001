using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Endless lanes: generated ahead of the camera, recycled behind it (O(1) via the
    /// minZ cursor — lane keys are always a contiguous range). Roads come in two-strip
    /// pairs with a dashed centre line between them.
    /// </summary>
    public class LaneManager : MonoBehaviour
    {
        private readonly Dictionary<int, Lane> lanes = new Dictionary<int, Lane>();

        private int frontZ;
        private int minZ;                       // oldest lane still alive (recycle cursor)
        private LaneType lastType = LaneType.Grass;
        private int runLength;
        private int pendingRoad;                // direction of the first strip of an unfinished road pair
        private int railCooldown;

        private CreepingCamera cameraControl;
        private CrossRoadEvents events;
        private bool inited;   // guards against play-exit/recompile races clearing injected refs

        public void Init(CreepingCamera cameraControl, CrossRoadEvents events)
        {
            this.cameraControl = cameraControl;
            inited = true;
            this.events = events;
            events.GameStarted.AddListener(ResetLanes);   // every game start rebuilds the world with the current sim seed
            events.Restarted.AddListener(ResetLanes);
            ResetLanes();   // first generation: a fresh game must have lanes without any restart
        }

        public void ResetLanes()
        {
            foreach (var lane in lanes.Values)
                if (lane != null) lane.Recycle();
            lanes.Clear();

            lastType = LaneType.Grass;
            runLength = 0;
            pendingRoad = 0;
            railCooldown = 0;

            minZ = CrossRoadConfig.StartBackZ;
            for (int z = CrossRoadConfig.StartBackZ; z < CrossRoadConfig.SafeZoneEndZ; z++)
                CreateLane(z, LaneType.Grass, 0, false);
            frontZ = CrossRoadConfig.SafeZoneEndZ;
        }

        public Lane GetLane(int z) => lanes.TryGetValue(z, out var lane) ? lane : null;

        private void Update()
        {
            if (!inited) return;

            // world line: the furthest player drives generation & recycling (not the camera) —
            // identical on every client since the ordered hop stream keeps all players in sync
            float follow = PlayerProgress.FrontZ;

            while (frontZ < follow + CrossRoadConfig.GenAhead) CreateNext();

            // lanes are contiguous: advance the cursor instead of scanning the dictionary
            float cutoff = follow - CrossRoadConfig.RecycleBehind;
            while (minZ < cutoff && lanes.Count > 0)
            {
                if (lanes.TryGetValue(minZ, out var lane))
                {
                    lane.Recycle();
                    lanes.Remove(minZ);
                }
                minZ++;
            }
        }

        private void CreateNext()
        {
            LaneType t;
            int dir = 0;
            bool centre = false;

            if (pendingRoad != 0)
            {
                // second strip of a road pair — opposite direction, dashed line in between
                t = LaneType.Road;
                dir = -pendingRoad;
                centre = true;
                pendingRoad = 0;
            }
            else
            {
                t = PickWeighted();
                if (railCooldown > 0 && t == LaneType.Rail) t = LaneType.Grass;

                int projected = t == lastType ? runLength + 1 : 1;
                bool over = (t == LaneType.River && projected > CrossRoadConfig.MaxRiverRun)
                         || (t == LaneType.Rail && projected > CrossRoadConfig.MaxRailRun)
                         || (t == LaneType.Road && projected > CrossRoadConfig.MaxRoadRun)
                         || (t == LaneType.Grass && projected > CrossRoadConfig.MaxGrassRun);
                if (over) t = LaneType.Grass;

                if (t == LaneType.Road)
                {
                    dir = Sim.Rng.Next(2) == 0 ? 1 : -1;
                    pendingRoad = dir;
                }
            }

            runLength = t == lastType ? runLength + 1 : 1;
            lastType = t;
            railCooldown = t == LaneType.Rail ? CrossRoadConfig.RailCooldown : Mathf.Max(0, railCooldown - 1);

            CreateLane(frontZ++, t, dir, centre);
        }

        private static LaneType PickWeighted()
        {
            double r = Sim.Rng.NextDouble();
            if (r < CrossRoadConfig.WeightGrass) return LaneType.Grass;
            if (r < CrossRoadConfig.WeightGrass + CrossRoadConfig.WeightRoad) return LaneType.Road;
            if (r < CrossRoadConfig.WeightGrass + CrossRoadConfig.WeightRoad + CrossRoadConfig.WeightRiver) return LaneType.River;
            return LaneType.Rail;
        }

        private void CreateLane(int z, LaneType type, int dir, bool centreLine)
        {
            var go = new GameObject("Lane_" + z + "_" + type);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            var lane = go.AddComponent<Lane>();
            lane.Init(type, z, dir, centreLine, events);
            lanes[z] = lane;
        }
    }
}
