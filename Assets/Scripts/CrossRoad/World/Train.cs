using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Warn-then-kill train: the signal lamp flashes first, then the train sweeps the lane.
    /// Owns its lamp material instance and destroys it with itself (no leak).
    /// </summary>
    public class Train : MonoBehaviour
    {
        public bool IsPassing { get; private set; }
        public float X => transform.localPosition.x;
        public float DangerHalfLen => CrossRoadConfig.TrainDangerHalfLen;

        private enum State { Idle, Warn, Passing }

        private State state = State.Idle;
        private float timer;
        private float idleWait;
        private float speed;
        private int dir;
        private Material lampMat;

        public void Init()
        {
            lampMat = CrossRoadKit.MatRaw(CrossRoadConfig.LampOff);
            BuildSignal();
            BuildTrain();
            ScheduleIdle();
            Sim.Stepped += OnSimTick;   // deterministic state machine (fixed tick dt)
        }

        private void OnDestroy()
        {
            Sim.Stepped -= OnSimTick;
            if (lampMat != null) Destroy(lampMat);   // MatRaw is uncached — clean it up
        }

        private void OnSimTick(float dt)
        {
            switch (state)
            {
                case State.Idle:
                    timer += dt;
                    if (timer >= idleWait)
                    {
                        state = State.Warn;
                        timer = 0f;
                    }
                    break;

                case State.Warn:
                    timer += dt;
                    bool on = (timer * 3.5f) % 1f < 0.5f;
                    lampMat.color = on ? CrossRoadConfig.LampOn : CrossRoadConfig.LampOff;
                    if (timer >= CrossRoadConfig.TrainWarnTime) StartPassing();
                    break;

                case State.Passing:
                    float x = transform.localPosition.x + speed * dt;
                    transform.localPosition = new Vector3(x, 0f, 0f);
                    if ((dir > 0 && x > 20f) || (dir < 0 && x < -20f)) ScheduleIdle();
                    break;
            }
        }

        private void StartPassing()
        {
            state = State.Passing;
            IsPassing = true;
            dir = Sim.Rng.Next(2) == 0 ? 1 : -1;
            speed = CrossRoadConfig.TrainSpeed * dir;
            transform.localScale = new Vector3(dir, 1f, 1f);
            transform.localPosition = new Vector3(-19f * dir, 0f, 0f);
            lampMat.color = CrossRoadConfig.LampOn;
        }

        private void ScheduleIdle()
        {
            state = State.Idle;
            IsPassing = false;
            timer = 0f;
            idleWait = (float)(Sim.Rng.NextDouble() * (CrossRoadConfig.TrainIdleMax - CrossRoadConfig.TrainIdleMin)) + CrossRoadConfig.TrainIdleMin;
            ParkOffscreen();
        }

        private void ParkOffscreen() => transform.localPosition = new Vector3(45f, 0f, 0f);

        private void BuildSignal()
        {
            var lane = transform.parent;
            CrossRoadKit.Block("SignalPole", lane, new Vector3(-12.3f, 0.70f, 0.34f),
                new Vector3(0.09f, 0.90f, 0.09f), CrossRoadKit.Mat(CrossRoadConfig.SignalPole));
            CrossRoadKit.Block("SignalPlate", lane, new Vector3(-12.3f, 1.26f, 0.40f),
                new Vector3(0.34f, 0.34f, 0.05f), CrossRoadKit.Mat(CrossRoadConfig.SignalPlate));
            CrossRoadKit.Block("SignalLamp", lane, new Vector3(-12.3f, 1.26f, 0.34f),
                new Vector3(0.26f, 0.26f, 0.26f), lampMat);
        }

        private void BuildTrain()
        {
            // engine (front at local +x, mirrored by scale.x when running the other way)
            CrossRoadKit.Block("Engine", transform, new Vector3(0.55f, 0.60f, 0f), new Vector3(1.30f, 0.55f, 0.72f),
                CrossRoadKit.Mat(CrossRoadConfig.TrainEngine));
            CrossRoadKit.Block("EngineRoof", transform, new Vector3(0.55f, 0.95f, 0f), new Vector3(1.10f, 0.16f, 0.58f),
                CrossRoadKit.Mat(CrossRoadConfig.TrainRoof));
            CrossRoadKit.Block("EngineNose", transform, new Vector3(1.30f, 0.42f, 0f), new Vector3(0.28f, 0.22f, 0.60f),
                CrossRoadKit.Mat(CrossRoadConfig.TrainNose));

            for (int i = 0; i < 3; i++)
            {
                float cx = -0.85f - i * 1.35f;
                Color c = i % 2 == 0 ? CrossRoadConfig.TrainCarriageA : CrossRoadConfig.TrainCarriageB;
                CrossRoadKit.Block("Carriage" + i, transform, new Vector3(cx, 0.58f, 0f), new Vector3(1.22f, 0.50f, 0.70f),
                    CrossRoadKit.Mat(c));
                CrossRoadKit.Block("CarriageRoof" + i, transform, new Vector3(cx, 0.89f, 0f), new Vector3(1.05f, 0.12f, 0.56f),
                    CrossRoadKit.Mat(CrossRoadConfig.TrainRoof));
            }
        }
    }
}
