using FixedMathSharp;
using FixedMathSharp.Utility;
using UnityEngine;

namespace Xiangsoft.Lib.LockStep
{
    public class RandomManager : MonoBehaviour
    {
        public static RandomManager Instance { get; private set; }

        private DeterministicRandom random;

        public Fixed64 Value { get { return random.NextFixed64(Fixed64.One); } }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            random = new DeterministicRandom(19890817);
        }

        public int Range(int min, int max)
        {
            return random.Next(min, max);
        }

        public Fixed64 Range(Fixed64 min, Fixed64 max)
        {
            return random.NextFixed64(min, max);
        }
    }
}