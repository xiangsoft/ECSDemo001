using FixedMathSharp;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Xiangsoft.Lib.LockStep;

namespace Xiangsoft.Game.UI
{
    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        [Header("配置")]
        public TextMeshPro textPrefab;
        public Fixed64 floatSpeed = Fixed64.Two;    // 向上漂移的速度
        public Fixed64 lifetime = Fixed64.One;      // 显示时长

        // 预先分配好内存，这辈子都不产生 GC
        private static char[] numberBuffer = new char[128];

        /// <summary>
        /// 纯数据驱动的结构体（紧凑内存，极速遍历）
        /// </summary>
        private struct DamageTextData
        {
            public TextMeshPro TextComponent;
            public Vector3d CurrentPos;
            public Fixed64 LifeTimer;
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

            Fixed64 dt = (Fixed64)Time.deltaTime;
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
                data.TextComponent.transform.position = data.CurrentPos.ToVector3();

                // 始终面向摄像机 (Billboard 效果)
                data.TextComponent.transform.rotation = camRotation;

                // 渐渐透明
                Fixed64 alpha = Fixed64.One - (data.LifeTimer / lifetime);
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

        public void ShowDamage(int damage, Vector3d position, bool isCrit = false)
        {
            if (activeCount >= activeTexts.Length)
                return; // 超过上限直接丢弃（玩家根本看不清那么多）

            TextMeshPro tmp = pool.Count > 0 ? pool.Dequeue() : createNewText();
            tmp.gameObject.SetActive(true);

            // 稍微随机偏移一点，防止数字完全叠在一起
            Vector3d randomOffset = new Vector3d(RandomManager.Instance.Range(new Fixed64(-0.5), new Fixed64(0.5)), Fixed64.One, RandomManager.Instance.Range(new Fixed64(-0.5), new Fixed64(0.5)));
            Vector3d startPos = position + randomOffset;

            int charLength = intToCharArray(damage, numberBuffer);
            tmp.SetCharArray(numberBuffer, 0, charLength); // 直接使用 char[]，避免 string 产生 GC
            tmp.color = isCrit ? Color.red : Color.white; // 暴击红字，普通白字
            tmp.fontSize = isCrit ? 6 : 4;               // 暴击字体变大

            // 录入纯数据数组
            activeTexts[activeCount] = new DamageTextData
            {
                TextComponent = tmp,
                CurrentPos = startPos,
                LifeTimer = Fixed64.Zero,
                CurrentColor = tmp.color
            };
            activeCount++;
        }

        // 手搓一个：将整数塞进 char[] 的 0 GC 方法
        private int intToCharArray(int value, char[] buffer)
        {
            if (value == 0)
            {
                buffer[0] = '0';
                return 1;
            }

            int length = 0;
            int temp = value;

            // 1. 算一下这个数字有几位
            while (temp > 0)
            {
                length++;
                temp /= 10;
            }

            // 2. 从后往前，把每一位数字转成字符塞进数组
            temp = value;
            for (int i = length - 1; i >= 0; i--)
            {
                buffer[i] = (char)('0' + (temp % 10)); // '0' 的 ASCII 码加上偏移
                temp /= 10;
            }

            return length; // 返回实际长度
        }
    }
}