using UnityEngine;
using Xiangsoft.Lib.ECS;
using Xiangsoft.Lib.ECS.Authoring;
using Xiangsoft.Lib.ECS.Pool;
using Xiangsoft.Lib.LockStep;

namespace Xiangsoft.Game.Level
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("当前关卡配置")]
        public WaveData CurrentLevelData;

        [Header("刷怪范围设置")]
        public Transform PlayerTransform;
        public float MinSpawnRadius = 15f; // 屏幕外刷怪，防止贴脸出怪
        public float MaxSpawnRadius = 25f;

        /// <summary>
        /// 游戏时间
        /// </summary>
        public float GameTime { get; private set; }

        /// <summary>
        /// 是否进行游戏中
        /// </summary>
        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            GameTime = 0;
            IsPlaying = false;
        }

        private void Update()
        {
            if (!IsPlaying || CurrentLevelData == null || PlayerTransform == null || ECSEngine.Instance == null)
                return;

            GameTime += Time.deltaTime;

            foreach (WaveEvent wave in CurrentLevelData.Waves)
            {
                if (wave.IsFinished || GameTime < wave.StartTime)
                    continue;

                // 清理死亡怪物的记录 (防越界，并且腾出同屏上限空间)
                wave.AliveEntities.RemoveAll(id => ECSEngine.Instance.World.EntityMasks[id] == 0);

                // 检查这波怪的同屏上限
                if (wave.AliveEntities.Count >= wave.MaxAliveAtSameTime)
                    continue;

                // 计时器刷怪
                wave.SpawnTimer += Time.deltaTime;
                if (wave.SpawnTimer >= wave.SpawnInterval)
                {
                    wave.SpawnTimer -= wave.SpawnInterval;
                    spawnEnemy(wave);
                }
            }
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            GameTime = 0f;

            if (CurrentLevelData == null)
            {
                IsPlaying = false;
                return;
            }

            DeterministicRandom.SetSeed(19890817);
            IsPlaying = true;

            foreach (var wave in CurrentLevelData.Waves)
            {
                wave.SpawnedCount = 0;
                wave.SpawnTimer = 0f;
                wave.IsFinished = false;
                wave.AliveEntities.Clear();
            }
        }

        private void spawnEnemy(WaveEvent wave)
        {
            // 在主角周围随机生成一个圆环坐标 (屏幕外盲区)
            Vector2 randomCircle = DeterministicRandom.insideUnitCircle.normalized * DeterministicRandom.Range(MinSpawnRadius, MaxSpawnRadius);
            Vector3 spawnPos = PlayerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // 从对象池拿到怪物
            EntityAuthoring enemy = UnitPool.Instance.Get(wave.EnemyPrefab, spawnPos, Quaternion.identity);

            // 记录存活
            wave.AliveEntities.Add(enemy.GetEntityID());
            wave.SpawnedCount++;

            // 检查是否刷满了
            if (wave.TotalToSpawn > 0 && wave.SpawnedCount >= wave.TotalToSpawn)
            {
                wave.IsFinished = true;
            }
        }
    }
}