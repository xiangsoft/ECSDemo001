using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// One builder per lane type: each knows only how to dress a lane.
    /// The returned hazard lists are handed back to the Lane for queries.
    /// Determinism: every random draw comes from Sim.Rng (the seeded game RNG) —
    /// the same lane sequence consumes randoms in the same order on every client.
    /// </summary>

    /// <summary>Grass strip: alternating greens + random trees (start zone kept open).</summary>
    public static class GrassBuilder
    {
        public static void Build(Transform lane, int z, System.Collections.Generic.List<int> trees)
        {
            var c = z % 2 == 0 ? CrossRoadConfig.GrassA : CrossRoadConfig.GrassB;
            CrossRoadKit.Block("Ground", lane, Vector3.zero, new Vector3(CrossRoadConfig.LaneSpan, 0.5f, 1f), CrossRoadKit.Mat(c));

            int count = z < 5 ? Sim.Rng.Next(0, 2) : Sim.Rng.Next(1, 5);
            for (int i = 0; i < count; i++)
            {
                int x = Sim.Rng.Next(-9, 10);
                if (trees.Contains(x)) continue;
                if (z < 5 && Mathf.Abs(x) < 4) continue;   // keep the start open
                trees.Add(x);
                BuildTree(lane, x);
            }
        }

        private static void BuildTree(Transform lane, int x)
        {
            float trunkH = (float)(Sim.Rng.NextDouble() * 0.50) + 0.40f;
            CrossRoadKit.Block("Trunk", lane, new Vector3(x, CrossRoadConfig.SurfY + trunkH * 0.5f, 0f),
                new Vector3(0.30f, trunkH, 0.30f), CrossRoadKit.Mat(CrossRoadConfig.Trunk));

            float y = CrossRoadConfig.SurfY + trunkH;
            int layers = Sim.Rng.Next(2, 4);
            for (int i = 0; i < layers; i++)
            {
                float s = 0.80f - 0.22f * i;
                float h = 0.46f - 0.09f * i;
                CrossRoadKit.Block("Crown" + i, lane, new Vector3(x, y + h * 0.5f, 0f), new Vector3(s, h, s),
                    CrossRoadKit.Mat(CrossRoadConfig.Crown[i]));
                y += h;
            }
        }
    }

    /// <summary>One-way road strip: edge lines, optional dashed centre line, wrapping cars.</summary>
    public static class RoadBuilder
    {
        public static void Build(Transform lane, int z, int dir, bool centreLine,
            System.Collections.Generic.List<MovingObject> cars)
        {
            CrossRoadKit.Block("Ground", lane, Vector3.zero, new Vector3(CrossRoadConfig.LaneSpan, 0.5f, 1f),
                CrossRoadKit.Mat(CrossRoadConfig.Road));

            var white = CrossRoadKit.Mat(CrossRoadConfig.LineWhite);
            CrossRoadKit.Block("EdgeA", lane, new Vector3(0f, 0.253f, -0.42f), new Vector3(CrossRoadConfig.LaneSpan, 0.02f, 0.10f), white);
            CrossRoadKit.Block("EdgeB", lane, new Vector3(0f, 0.253f, 0.42f), new Vector3(CrossRoadConfig.LaneSpan, 0.02f, 0.10f), white);

            // dashed centre line shared by a two-strip road — cars keep to their own strip
            if (centreLine)
                for (float x = -10.5f; x <= 10.5f; x += 1.4f)
                    CrossRoadKit.Block("Dash", lane, new Vector3(x, 0.253f, -0.5f), new Vector3(0.66f, 0.02f, 0.09f), white);

            int count = Mathf.Clamp(1 + z / CrossRoadConfig.CarCountPerLaneSpan, 1, CrossRoadConfig.CarCountMax);
            for (int i = 0; i < count; i++)
            {
                float seg = CrossRoadConfig.LaneSpan / count;
                float x = -CrossRoadConfig.LaneSpan * 0.5f + seg * (i + (float)Sim.Rng.NextDouble() * 0.70f + 0.15f);
                float speed = ((float)(Sim.Rng.NextDouble() * (CrossRoadConfig.CarSpeedMax - CrossRoadConfig.CarSpeedMin)) + CrossRoadConfig.CarSpeedMin
                    + Mathf.Min(CrossRoadConfig.CarSpeedZCap, z * CrossRoadConfig.CarSpeedPerLane)) * dir;
                cars.Add(BuildCar(lane, speed, x));
            }
        }

        private static MovingObject BuildCar(Transform lane, float speed, float x)
        {
            var go = new GameObject("Car");
            go.transform.SetParent(lane, false);
            go.transform.localPosition = new Vector3(x, 0f, 0f);

            var bodyMat = CrossRoadKit.Mat(CrossRoadConfig.CarPalette[Sim.Rng.Next(0, CrossRoadConfig.CarPalette.Length)]);
            var glassMat = CrossRoadKit.Mat(CrossRoadConfig.CarGlass);
            var tyreMat = CrossRoadKit.Mat(CrossRoadConfig.CarTyre);

            CrossRoadKit.Block("Body", go.transform, new Vector3(0f, 0.56f, 0f), new Vector3(0.92f, 0.34f, 0.62f), bodyMat);
            CrossRoadKit.Block("Roof", go.transform, new Vector3(0f, 0.84f, -0.03f), new Vector3(0.54f, 0.20f, 0.48f), glassMat);
            CrossRoadKit.Block("TyreFL", go.transform, new Vector3(-0.28f, 0.31f, 0.28f), Vector3.one * 0.13f, tyreMat);
            CrossRoadKit.Block("TyreFR", go.transform, new Vector3(-0.28f, 0.31f, -0.28f), Vector3.one * 0.13f, tyreMat);
            CrossRoadKit.Block("TyreBL", go.transform, new Vector3(0.30f, 0.31f, 0.28f), Vector3.one * 0.13f, tyreMat);
            CrossRoadKit.Block("TyreBR", go.transform, new Vector3(0.30f, 0.31f, -0.28f), Vector3.one * 0.13f, tyreMat);

            var mo = go.AddComponent<MovingObject>();
            mo.Init(speed, CrossRoadConfig.MoverBound, 0.46f);
            return mo;
        }
    }

    /// <summary>River strip: water plane + drifting logs (all same direction per strip).</summary>
    public static class RiverBuilder
    {
        public static void Build(Transform lane, System.Collections.Generic.List<MovingObject> logs)
        {
            CrossRoadKit.Block("Water", lane, new Vector3(0f, CrossRoadConfig.WaterY - 0.25f, 0f),
                new Vector3(CrossRoadConfig.LaneSpan, 0.5f, 1f), CrossRoadKit.Mat(CrossRoadConfig.River));

            int count = Sim.Rng.Next(CrossRoadConfig.LogCountMin, CrossRoadConfig.LogCountMax);
            int dir = Sim.Rng.Next(2) == 0 ? 1 : -1;
            for (int i = 0; i < count; i++)
            {
                float seg = CrossRoadConfig.LaneSpan / count;
                float x = -CrossRoadConfig.LaneSpan * 0.5f + seg * (i + (float)Sim.Rng.NextDouble() * 0.80f + 0.10f);
                float len = (float)(Sim.Rng.NextDouble() * (CrossRoadConfig.LogLenMax - CrossRoadConfig.LogLenMin)) + CrossRoadConfig.LogLenMin;
                float speed = ((float)(Sim.Rng.NextDouble() * (CrossRoadConfig.LogSpeedMax - CrossRoadConfig.LogSpeedMin)) + CrossRoadConfig.LogSpeedMin) * dir;

                var go = new GameObject("Log");
                go.transform.SetParent(lane, false);
                go.transform.localPosition = new Vector3(x, CrossRoadConfig.LogTopY - 0.11f, 0f);
                CrossRoadKit.Block("Wood", go.transform, Vector3.zero, new Vector3(len, 0.22f, 0.78f),
                    CrossRoadKit.Mat(CrossRoadConfig.LogWood));

                var mo = go.AddComponent<MovingObject>();
                mo.Init(speed, CrossRoadConfig.MoverBound, len * 0.5f);
                logs.Add(mo);
            }
        }
    }

    /// <summary>Rail strip: bed, sleepers, rails + the warn-then-kill train.</summary>
    public static class RailBuilder
    {
        public static Train Build(Transform lane)
        {
            CrossRoadKit.Block("Bed", lane, Vector3.zero, new Vector3(CrossRoadConfig.LaneSpan, 0.5f, 1f),
                CrossRoadKit.Mat(CrossRoadConfig.RailBed));

            var sleeperMat = CrossRoadKit.Mat(CrossRoadConfig.Sleeper);
            for (float x = -11f; x <= 11f; x += 0.8f)
                CrossRoadKit.Block("Sleeper", lane, new Vector3(x, 0.253f, 0f), new Vector3(0.16f, 0.05f, 0.86f), sleeperMat);

            var railMat = CrossRoadKit.Mat(CrossRoadConfig.RailSteel);
            CrossRoadKit.Block("RailA", lane, new Vector3(0f, 0.30f, -0.27f), new Vector3(CrossRoadConfig.LaneSpan, 0.06f, 0.07f), railMat);
            CrossRoadKit.Block("RailB", lane, new Vector3(0f, 0.30f, 0.27f), new Vector3(CrossRoadConfig.LaneSpan, 0.06f, 0.07f), railMat);

            var go = new GameObject("Train");
            go.transform.SetParent(lane, false);
            var train = go.AddComponent<Train>();
            train.Init();
            return train;
        }
    }
}
