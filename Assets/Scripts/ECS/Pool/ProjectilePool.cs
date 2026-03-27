using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Lib.ECS.Pool
{
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        // 核心：字典嵌套队列。键是预制体的 ID，值是这个预制体对应的闲置对象池
        private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();
        private Transform poolRoot;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            poolRoot = new GameObject("ProjectilePool_Root").transform;
            poolRoot.parent = transform; // 让它成为这个对象的子对象，保持层级整洁
        }

        // 获取子弹：传入你需要发射的预制体
        public GameObject Get(GameObject prefab)
        {
            // 获取预制体独一无二的整数 ID 作为 Key
            int key = prefab.GetInstanceID();

            // 如果字典里还没有这个法术的池子，建一个
            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary[key] = new Queue<GameObject>();
            }

            // 如果池子里有存货，拿出来用
            if (poolDictionary[key].Count > 0)
            {
                GameObject obj = poolDictionary[key].Dequeue();
                obj.SetActive(true);

                return obj;
            }

            // 如果池子空了（或者第一次发射），就实例化一个新的
            GameObject newObj = Instantiate(prefab, poolRoot);

            // 给新生成的皮囊挂上身份牌，记住自己的根
            PooledProjectile marker = newObj.AddComponent<PooledProjectile>();
            marker.PrefabInstanceID = key;

            return newObj;
        }

        // 回收子弹：只需要传入实例即可，它自己知道回哪个家
        public void Release(GameObject instance)
        {
            PooledProjectile marker = instance.GetComponent<PooledProjectile>();

            if (marker != null && poolDictionary.ContainsKey(marker.PrefabInstanceID))
            {
                instance.SetActive(false);
                poolDictionary[marker.PrefabInstanceID].Enqueue(instance);
            }
            else
            {
                // 万一是不守规矩的野对象，直接销毁防内存泄漏
                Destroy(instance);
            }
        }
    }
}