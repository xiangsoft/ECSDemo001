using System;
using System.Collections.Generic;
using UnityEngine;
using Xiangsoft.Lib.ECS.Attribute;
using Xiangsoft.Lib.ECS.Authoring;

namespace Xiangsoft.Lib.ECS.Pool
{
    // 供策划在面板上配置不同怪物的预热数量
    [Serializable]
    public struct UnitPoolConfig
    {
        public EntityAuthoring Prefab;
        public int InitialSize; // 比如：史莱姆配 500，骷髅兵配 200
    }

    public class UnitPool : MonoBehaviour
    {
        public static UnitPool Instance { get; private set; }

        [Header("多怪物对象池配置 (预热)")]
        public List<UnitPoolConfig> poolConfigs = new List<UnitPoolConfig>();

        // 核心：字典嵌套队列。键是预制体的 ID，值是对应的池子
        private Dictionary<int, Queue<EntityAuthoring>> poolDictionary = new Dictionary<int, Queue<EntityAuthoring>>();
        private Transform poolRoot;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            poolRoot = new GameObject("UnitPool_Root").transform;

            // 游戏启动时，根据配置表进行预热，防止游戏中途生成卡顿
            foreach (var config in poolConfigs)
            {
                if (config.Prefab != null)
                {
                    int key = config.Prefab.gameObject.GetInstanceID();
                    poolDictionary[key] = new Queue<EntityAuthoring>();

                    for (int i = 0; i < config.InitialSize; i++)
                    {
                        CreateNewUnit(config.Prefab, key);
                    }
                }
            }
        }

        private EntityAuthoring CreateNewUnit(EntityAuthoring prefab, int key)
        {
            EntityAuthoring unit = Instantiate(prefab, poolRoot);
            unit.PrefabInstanceID = key;
            unit.gameObject.SetActive(false);
            poolDictionary[key].Enqueue(unit);

            return unit;
        }

        // 外部调用：从池子里拿一个兵
        public EntityAuthoring Get(EntityAuthoring prefab, Vector3 position, Quaternion rotation)
        {
            int key = prefab.gameObject.GetInstanceID();

            // 如果请求了一种没配置过的怪物，临时给它建个池子
            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary[key] = new Queue<EntityAuthoring>();
            }

            EntityAuthoring unit;
            if (poolDictionary[key].Count > 0)
            {
                unit = poolDictionary[key].Dequeue();
            }
            else
            {
                // 池子被抽干了，临时爆兵（会产生极其轻微的 GC，但保证游戏不崩溃）
                unit = CreateNewUnit(prefab, key);
                // 因为 CreateNewUnit 会把它塞进队列，我们要把它再拿出来
                unit = poolDictionary[key].Dequeue();
            }

            unit.transform.position = position;
            unit.transform.rotation = rotation;

            unit.GetComponent<EntityStats>().ResetStats();
            unit.gameObject.SetActive(true);

            // 通知 ECS 分配 ID
            unit.Register();

            return unit;
        }

        // 外部调用：把死掉的兵扔回池子
        public void Release(EntityAuthoring unit)
        {
            unit.Unregister();
            unit.gameObject.SetActive(false);

            if (poolDictionary.ContainsKey(unit.PrefabInstanceID))
                poolDictionary[unit.PrefabInstanceID].Enqueue(unit);
            else
                Destroy(unit.gameObject); // 防御性兜底
        }
    }
}