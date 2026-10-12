using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Every tunable number, colour and text in one place — the single source of truth.
    /// Kept as code constants to honour the NO-ASSETS brief; a CrossRoadConfig SO can
    /// later layer on top of these defaults without touching call sites.
    /// </summary>
    public static class CrossRoadConfig
    {
        // ---- world & grid -------------------------------------------------

        public const int LaneHalfWidth = 11;     // lane strip covers x: -11..11
        public const int PlayHalfWidth = 8;      // chicken walkable |x| <= 8
        public const float SurfY = 0.25f;        // top of ground blocks
        public const float LogTopY = 0.42f;      // top of river logs
        public const float WaterY = 0.12f;       // river surface
        public const float LaneSpan = 23f;       // full lane strip width
        public const float MoverBound = 15f;     // wrap threshold for cars/logs

        // ---- player ---------------------------------------------------------

        public const float HopDuration = 0.13f;
        public const float HopArcHeight = 0.40f;
        public const float RideOffX = 10.5f;      // |x| beyond this on a log kills

        // ---- camera -------------------------------------------------------

        public const float BaseCreep = 0.62f;     // creep speed (units/s)
        public const float CreepScoreScale = 0.004f;
        public const float CreepScoreCap = 0.7f;
        public const float BehindLine = 3.4f;     // player this far behind FollowZ dies
        public const float CamHeight = 9f;
        public const float CamBack = 7f;
        public const float CamPitch = 38f;
        public const float CamFov = 45f;
        public const float CamFollowLerp = 3.5f;

        // ---- lane generation --------------------------------------------

        public const int StartBackZ = -8;
        public const int SafeZoneEndZ = 6;        // lanes z < 6 are safe grass
        public const float GenAhead = 22f;        // generate up to FollowZ + this
        public const float RecycleBehind = 13f;   // recycle lanes below FollowZ - this

        public const float WeightGrass = 0.30f;
        public const float WeightRoad = 0.34f;
        public const float WeightRiver = 0.19f;
        // rail gets the remainder

        public const int MaxRiverRun = 2;
        public const int MaxRailRun = 1;
        public const int MaxRoadRun = 3;
        public const int MaxGrassRun = 3;
        public const int RailCooldown = 2;

        // ---- hazards -------------------------------------------------------

        public const float CarSpeedMin = 1.6f;
        public const float CarSpeedMax = 3.0f;
        public const float CarSpeedPerLane = 0.01f;   // per z, capped
        public const float CarSpeedZCap = 1.5f;
        public const int CarCountPerLaneSpan = 40;   // count = 1 + z / this, max 3
        public const int CarCountMax = 3;
        public const float CarHitRadius = 0.80f;

        public const float LogSpeedMin = 0.8f;
        public const float LogSpeedMax = 1.7f;
        public const float LogLenMin = 1.7f;
        public const float LogLenMax = 2.9f;
        public const int LogCountMin = 2;
        public const int LogCountMax = 4;

        public const float TrainWarnTime = 1.4f;
        public const float TrainSpeed = 14f;
        public const float TrainIdleMin = 3.5f;
        public const float TrainIdleMax = 8f;
        public const float TrainDangerHalfLen = 3.1f;
        public const float TrainHitMargin = 0.30f;

        // ---- palette (flat colours) -------------------------------------

        public static readonly Color GrassA = new Color(0.52f, 0.78f, 0.34f);
        public static readonly Color GrassB = new Color(0.45f, 0.71f, 0.30f);
        public static readonly Color Trunk = new Color(0.55f, 0.40f, 0.26f);
        public static readonly Color[] Crown =
        {
            new Color(0.16f, 0.50f, 0.22f),
            new Color(0.24f, 0.62f, 0.28f),
            new Color(0.36f, 0.74f, 0.36f),
        };
        public static readonly Color Road = new Color(0.30f, 0.30f, 0.35f);
        public static readonly Color LineWhite = new Color(0.92f, 0.92f, 0.90f);
        public static readonly Color River = new Color(0.20f, 0.58f, 0.88f);
        public static readonly Color LogWood = new Color(0.62f, 0.45f, 0.28f);
        public static readonly Color RailBed = new Color(0.46f, 0.43f, 0.38f);
        public static readonly Color Sleeper = new Color(0.38f, 0.30f, 0.24f);
        public static readonly Color RailSteel = new Color(0.62f, 0.63f, 0.66f);
        public static readonly Color Sky = new Color(0.60f, 0.84f, 0.95f);

        public static readonly Color[] CarPalette =
        {
            new Color(0.90f, 0.25f, 0.20f), new Color(0.95f, 0.60f, 0.15f), new Color(0.20f, 0.55f, 0.90f),
            new Color(0.35f, 0.65f, 0.30f), new Color(0.85f, 0.30f, 0.55f), new Color(0.90f, 0.88f, 0.85f),
        };
        public static readonly Color CarGlass = new Color(0.90f, 0.95f, 0.98f);
        public static readonly Color CarTyre = new Color(0.12f, 0.12f, 0.14f);

        public static readonly Color ChickenBody = new Color(0.97f, 0.96f, 0.90f);
        public static readonly Color ChickenBeak = new Color(1.00f, 0.72f, 0.10f);
        public static readonly Color ChickenComb = new Color(0.90f, 0.20f, 0.18f);
        public static readonly Color ChickenEye = new Color(0.10f, 0.10f, 0.12f);

        public static readonly Color TrainEngine = new Color(0.78f, 0.20f, 0.16f);
        public static readonly Color TrainNose = new Color(1.00f, 0.78f, 0.12f);
        public static readonly Color TrainRoof = new Color(0.30f, 0.30f, 0.34f);
        public static readonly Color TrainCarriageA = new Color(0.88f, 0.89f, 0.91f);
        public static readonly Color TrainCarriageB = new Color(0.35f, 0.48f, 0.68f);
        public static readonly Color LampOff = new Color(0.45f, 0.10f, 0.10f);
        public static readonly Color LampOn = new Color(1.00f, 0.20f, 0.12f);
        public static readonly Color SignalPole = new Color(0.22f, 0.22f, 0.25f);
        public static readonly Color SignalPlate = new Color(0.15f, 0.15f, 0.18f);

        // (death reason texts moved to Localization — see LKey.DeathXxx and TranslationTables)
    }
}
