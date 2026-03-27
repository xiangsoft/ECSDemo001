using UnityEngine;
using Xiangsoft.Lib.ECS.Authoring;
using Xiangsoft.Lib.ECS.Pool;

namespace Xiangsoft.Lib.ECS.Spawner
{
    public class UnitECSSpawner : MonoBehaviour
    {
        public static UnitECSSpawner Instance { get; private set; }

        public EntityAuthoring ECSPrefab;

        public int maxUnits = 1000;
        public float spawnInterval = 0.05f;
        public Vector2 spawnAreaSize = new Vector2(4f, 4f);

        [HideInInspector]
        public bool IsPlayerInitOK = false;

        private float spawnTimer = 0f;
        private int currentUnitCount = 0;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void Update()
        {
            if (!IsPlayerInitOK || currentUnitCount >= maxUnits)
                return;

            spawnTimer += Time.deltaTime;

            while (spawnTimer >= spawnInterval && currentUnitCount < maxUnits)
            {
                spawnTimer -= spawnInterval;
                spawnUnit();
            }
        }

        private void spawnUnit()
        {
            float randomX = transform.position.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
            float randomZ = transform.position.z + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
            Vector3 spawnPos = new Vector3(randomX, 0f, randomZ);

            UnitPool.Instance.Get(ECSPrefab, spawnPos, Quaternion.identity);

            currentUnitCount++;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawCube(transform.position, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));
            // 画个边框更清晰
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));
        }
#endif
    }
}