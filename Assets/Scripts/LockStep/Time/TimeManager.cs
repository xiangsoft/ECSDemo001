using FixedMathSharp;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Lib.LockStep
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("网络帧同步设置")]
        public int LogicFrameRate = 30; // 逻辑帧率：每秒跑30次 ECS Update
        private Fixed64 logicTickTime;    // 每帧间隔 (1.0 / 30 = 0.0333f)
        private Fixed64 accumulator = Fixed64.Zero; // 时间累加器
        public int CurrentLogicFrame { get; private set; } // 当前跑到了第几帧
        private List<Action<Fixed64>> logicUpdates = null;
        public bool StartLogic { get; set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            Application.targetFrameRate = LogicFrameRate; // 固定渲染帧率，防止过高或过低
            CurrentLogicFrame = 0;
            logicTickTime = Fixed64.One / (Fixed64)LogicFrameRate;
            logicUpdates = new List<Action<Fixed64>>();
        }

        private void Update()
        {
            if(!StartLogic)
                return;
            
            accumulator += (Fixed64)Time.deltaTime;

            while (accumulator >= logicTickTime)
            {
                foreach (Action<Fixed64> logicUpdate in logicUpdates)
                {
                    logicUpdate.Invoke(logicTickTime);
                }

                accumulator -= logicTickTime;
                CurrentLogicFrame++;
            }
        }

        public void RegisterLogicUpdate(Action<Fixed64> logicUpdate)
        {
            if (logicUpdates.Contains(logicUpdate))
                return;

            logicUpdates.Add(logicUpdate);
        }

        public void UnregisterLogicUpdate(Action<Fixed64> logicUpdate)
        {
            if (!logicUpdates.Contains(logicUpdate))
                return;

            logicUpdates.Remove(logicUpdate);
        }
    }
}