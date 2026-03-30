using System.Collections.Generic;
using TMPro;
using TrueSync;
using UnityEngine;

namespace Xiangsoft.Game.UI
{
    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        [Header("配置")]
        public TextMeshPro textPrefab;
        public FP floatSpeed = 2f;    // 向上漂移的速度
        public FP lifetime = 1f;      // 显示时长

        /// <summary>
        /// 纯数据驱动的结构体（紧凑内存，极速遍历）
        /// </summary>
        private struct DamageTextData
        {
            public TextMeshPro TextComponent;
            public TSVector CurrentPos;
            public FP LifeTimer;
            public Color CurrentColor;
        }

        // 大数组存储正在显示的文字（拒绝 List，拥抱 Array）
        private DamageTextData[] activeTexts;
        private int activeCount = 0;

        // 皮囊对象池
        private Queue<TextMeshPro> pool = new Queue<TextMeshPro>();
        private Camera mainCamera;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            mainCamera = Camera.main;
            activeTexts = new DamageTextData[2000]; // 限制同屏最多 2000 个数字，保护性能

            // 预热池子
            for (int i = 0; i < 100; i++)
            {
                pool.Enqueue(createNewText());
            }
        }

        private void Update()
        {
            if (activeCount == 0)
                return;

            float dt = Time.deltaTime;
            Quaternion camRotation = mainCamera.transform.rotation;

            // 倒序遍历，方便删除
            for (int i = activeCount - 1; i >= 0; i--)
            {
                // 使用 ref 直接修改数组内部的数据
                ref DamageTextData data = ref activeTexts[i];
                data.LifeTimer += dt;

                if (data.LifeTimer >= lifetime)
                {
                    // 1. 寿命耗尽，回收皮囊
                    data.TextComponent.gameObject.SetActive(false);
                    pool.Enqueue(data.TextComponent);

                    // 2. ★ 数组填补空缺 (把数组最后一位的数据移到当前位置，达成 O(1) 极速删除)
                    activeTexts[i] = activeTexts[activeCount - 1];
                    activeCount--;
                    continue;
                }

                // 向上漂移
                data.CurrentPos.y += floatSpeed * dt;
                data.TextComponent.transform.position = data.CurrentPos.ToVector();

                // 始终面向摄像机 (Billboard 效果)
                data.TextComponent.transform.rotation = camRotation;

                // 渐渐透明
                FP alpha = 1f - (data.LifeTimer / lifetime);
                data.CurrentColor.a = (float)alpha;
                data.TextComponent.color = data.CurrentColor;
            }
        }

        private TextMeshPro createNewText()
        {
            TextMeshPro tmp = Instantiate(textPrefab, transform);
            tmp.gameObject.SetActive(false);

            return tmp;
        }

        public void ShowDamage(int damage, TSVector position, bool isCrit = false)
        {
            if (activeCount >= activeTexts.Length)
                return; // 超过上限直接丢弃（玩家根本看不清那么多）

            TextMeshPro tmp = pool.Count > 0 ? pool.Dequeue() : createNewText();
            tmp.gameObject.SetActive(true);

            // 稍微随机偏移一点，防止数字完全叠在一起
            TSVector randomOffset = new TSVector(TSRandom.Range(-0.5f, 0.5f), 1f, TSRandom.Range(-0.5f, 0.5f));
            TSVector startPos = position + randomOffset;

            tmp.text = damage.ToString();
            tmp.color = isCrit ? Color.red : Color.white; // 暴击红字，普通白字
            tmp.fontSize = isCrit ? 6 : 4;               // 暴击字体变大

            // 录入纯数据数组
            activeTexts[activeCount] = new DamageTextData
            {
                TextComponent = tmp,
                CurrentPos = startPos,
                LifeTimer = 0f,
                CurrentColor = tmp.color
            };
            activeCount++;
        }
    }
}