using UnityEngine;

namespace Xiangsoft.Game.Level
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {

        }
    }
}