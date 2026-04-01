using FixedMathSharp;
using UnityEngine;
using Xiangsoft.Game.Logic;
using Xiangsoft.Game.Network;
using Xiangsoft.Lib.ECS;

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

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            Application.targetFrameRate = LogicFrameRate; // 固定渲染帧率，防止过高或过低
            CurrentLogicFrame = 0;
            logicTickTime = Fixed64.One / new Fixed64(LogicFrameRate);
        }

        private void Update()
        {
            if (LockstepClient.Instance == null || !LockstepClient.Instance.IsConnected)
                return;

            accumulator += new Fixed64(Time.deltaTime);

            while (accumulator >= logicTickTime)
            {
                if (LockstepClient.Instance.FrameQueue.Count == 0)
                    break;

                FrameData currentFrame = LockstepClient.Instance.FrameQueue.Dequeue();

                foreach (PlayerCommand cmd in currentFrame.Commands)
                {
                    int entityID = cmd.PlayerID;

                    if (ECSEngine.Instance.World.EntityMasks[entityID] == 0)
                        continue;

                    Vector3d worldPos = new Vector3d(Fixed64.FromRaw(cmd.MoveDirX), Fixed64.Zero, Fixed64.FromRaw(cmd.MoveDirZ));
                    if (worldPos != PlayerController.Instance.NoMove)
                        PlayerController.Instance.Move(worldPos);

                    if (cmd.IsCastSkill)
                        PlayerController.Instance.TryCastSkill();
                }

                ECSEngine.Instance.LogicUpdate(logicTickTime);

                accumulator -= logicTickTime;
                CurrentLogicFrame = currentFrame.FrameID;
            }
        }
    }
}