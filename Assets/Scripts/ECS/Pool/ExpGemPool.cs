using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Lib.ECS.Pool
{
    public class ExpGemPool : MonoBehaviour
    {
        public static ExpGemPool Instance { get; private set; }
        public GameObject gemPrefab;
        
        private Queue<GameObject> pool = new Queue<GameObject>();
        private Transform poolRoot;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            poolRoot = new GameObject("ExpGemPool_Root").transform;
            poolRoot.parent = transform; // 让它成为这个对象的子对象，保持层级整洁
        }

        public GameObject Get()
        {
            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            return Instantiate(gemPrefab, transform);
        }

        public void Release(GameObject obj)
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}