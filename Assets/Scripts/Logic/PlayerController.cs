using FixedMathSharp;
using System.Collections.Generic;
using UnityEngine;
using Xiangsoft.Game.Network;
using Xiangsoft.Game.Skill;
using Xiangsoft.Lib.ECS;
using Xiangsoft.Lib.ECS.Attribute;
using Xiangsoft.Lib.Pathfinding;

namespace Xiangsoft.Game.Logic
{
    [RequireComponent(typeof(EntityStats), typeof(SkillController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public EntityStats Stats;
        [HideInInspector]
        public Vector3d NoMove = new Vector3d(99999, 0, 99999);

        private SkillController skillController;

        private List<Vector3> currentPath = new List<Vector3>(100);
        private int currentWaypointIndex = 0;
        private int lastGridIndex = -1;
        private List<int> enemiesNearMe = new List<int>(64);
        private bool isInitialized = false;

        private void Awake()
        {
            Instance = this;
            Stats = GetComponent<EntityStats>();
            skillController = GetComponent<SkillController>();
        }

        private void Start()
        {
            Invoke(nameof(initPlayer), 0.1f);
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            if (LockstepClient.Instance == null || !LockstepClient.Instance.IsConnected)
                return;

            Vector3d worldPos = handleMouseInput();
            followPath();
            checkGridChangeAndUpdate(false);
            bool isCastSkill = checkSkills();

            PlayerCommand cmd = new PlayerCommand
            {
                PlayerID = ECSEngine.Instance.PlayerEntityID,
                MoveDirX = worldPos.x.m_rawValue,
                MoveDirZ = worldPos.z.m_rawValue,
                IsCastSkill = isCastSkill
            };

            LockstepClient.Instance.SendLocalCommand(cmd);
        }

        private void initPlayer()
        {
            BaseGrid.Instance.AStarGrid.Init();
            checkGridChangeAndUpdate(true);
            Stats.ResetStats();
            isInitialized = true;
        }

        private Vector3d handleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f);
                Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);
                return worldMousePos.ToVector3d();
            }

            return NoMove;
        }

        private void followPath()
        {
            // 如果路径走完了，或者没路径，就不动
            if (currentPath.Count == 0 || currentWaypointIndex >= currentPath.Count)
                return;

            Vector3 targetWaypoint = currentPath[currentWaypointIndex];
            Vector3 dir = targetWaypoint - transform.position;
            dir.y = 0;

            // 如果距离当前路点非常近了，就切换到下一个路点
            if (dir.sqrMagnitude < 0.05f)
            {
                currentWaypointIndex++;
            }
            else
            {
                // 向着当前路点移动
                Vector3 moveDir = dir.normalized;
                transform.position = Vector3.MoveTowards(transform.position, targetWaypoint, ((float)Stats.Get(FloatStat.MoveSpeed)) * Time.deltaTime);

                // 平滑转向
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveDir), ((float)Stats.Get(FloatStat.RotationSpeed)) * Time.deltaTime);
            }
        }

        private void checkGridChangeAndUpdate(bool forceUpdate)
        {
            if (BaseGrid.Instance == null || BaseGrid.Instance.Cells == null)
                return;

            int currentIndex = BaseGrid.Instance.GetCellIndexFromWorldPos(transform.position);

            // 当主角走到了一个全新的格子里，才会刷新怪物流场
            if (currentIndex != lastGridIndex || forceUpdate)
            {
                lastGridIndex = currentIndex;

                BaseGrid.Instance.FlowGrid.GenerateIntegrationField(currentIndex);
                BaseGrid.Instance.FlowGrid.GenerateVectorField();
            }
        }

        private bool checkSkills()
        {
            if (skillController == null || ECSEngine.Instance == null || ECSEngine.Instance.SpatialGrid == null)
                return false;

            enemiesNearMe.Clear();
            ECSEngine.Instance.SpatialGrid.FindNeighbors(transform.position.ToVector3d(), enemiesNearMe);

            if (enemiesNearMe.Count == 0)
                return false;

            EntityStats targetStats = null;
            Vector3d targetPos = Vector3d.Zero;

            foreach (int id in enemiesNearMe)
            {
                EntityStats stats = ECSEngine.Instance.World.StatsBridge[id];

                if (stats == null || stats.IsDead || stats == Stats)
                    continue;

                targetStats = stats;
                targetPos = ECSEngine.Instance.World.Transforms[id].Position;
                break;
            }

            if (targetStats == null)
                return false;

            return skillController.CheckCanCastAny();
        }

        public void TryCastSkill()
        {
            if (skillController == null || ECSEngine.Instance == null || ECSEngine.Instance.SpatialGrid == null)
                return;

            enemiesNearMe.Clear();
            ECSEngine.Instance.SpatialGrid.FindNeighbors(transform.position.ToVector3d(), enemiesNearMe);

            if (enemiesNearMe.Count == 0)
                return;

            EntityStats targetStats = null;
            Vector3d targetPos = Vector3d.Zero;

            foreach (int id in enemiesNearMe)
            {
                EntityStats stats = ECSEngine.Instance.World.StatsBridge[id];

                if (stats == null || stats.IsDead || stats == Stats)
                    continue;

                targetStats = stats;
                targetPos = ECSEngine.Instance.World.Transforms[id].Position;
                break;
            }

            if (targetStats == null)
                return;

            skillController.TryCastAll(targetStats, targetPos);
        }

        public void Move(Vector3d worldPos)
        {
            if (BaseGrid.Instance.AStarGrid.FindPath(transform.position, worldPos.ToVector3(), currentPath))
                currentWaypointIndex = 0; // 重置路点索引，准备出发
            else
                Debug.LogWarning("Wrong Pos");
        }

#if UNITY_EDITOR
        // 画出 A* 的路线，方便 Debug
        private void OnDrawGizmos()
        {
            if (currentPath == null || currentPath.Count == 0)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentPath[0]);
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
        }
#endif
    }
}