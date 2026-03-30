using System;
using System.Collections.Generic;
using UnityEngine;
using Xiangsoft.Lib.ECS.Authoring;

namespace Xiangsoft.Game.Level
{
    [Serializable]
    public class WaveEvent
    {
        [Header("触发时间 (秒)")]
        public float StartTime;

        [Header("刷什么怪？")]
        public EntityAuthoring EnemyPrefab;

        [Header("总共刷几只？(0代表无限刷)")]
        public int TotalToSpawn = 100;

        [Header("每次刷怪间隔 (秒)")]
        public float SpawnInterval = 0.5f;

        [Header("同屏上限 (防止把电脑卡死)")]
        public int MaxAliveAtSameTime = 50;

        // 运行时状态 (隐藏不给策划看)
        [HideInInspector] 
        public int SpawnedCount = 0;

        [HideInInspector] 
        public float SpawnTimer = 0f;

        [HideInInspector] 
        public bool IsFinished = false;

        [HideInInspector] 
        public List<int> AliveEntities = new List<int>(); // 记录这波怪活着几个
    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "关卡/波次配置")]
    public class WaveData : ScriptableObject
    {
        public string LevelName = "第一关：无尽平原";
        public List<WaveEvent> Waves = new List<WaveEvent>();
    }
}
