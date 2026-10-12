using UnityEngine;
using UnityEngine.InputSystem;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Restarts the game when R is pressed — input only, nothing else.
    /// </summary>
    public class RestartOnRKey : MonoBehaviour
    {
        private CrossRoadEvents events;
        private bool inited;   // guards against play-exit/recompile races clearing injected refs

        public void Init(CrossRoadEvents events)
        {
            this.events = events;
            inited = true;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (inited && kb != null && kb.rKey.wasPressedThisFrame) events.Restarted.Raise();
        }
    }
}
