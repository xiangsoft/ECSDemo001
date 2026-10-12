using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// The chicken's looks: a stack of flat-coloured primitives. Visuals only.
    /// </summary>
    public class ChickenRig : MonoBehaviour
    {
        public void Build()
        {
            var matBody = CrossRoadKit.Mat(CrossRoadConfig.ChickenBody);
            var matBeak = CrossRoadKit.Mat(CrossRoadConfig.ChickenBeak);
            var matComb = CrossRoadKit.Mat(CrossRoadConfig.ChickenComb);
            var matEye = CrossRoadKit.Mat(CrossRoadConfig.ChickenEye);

            void Part(string nm, Vector3 pos, Vector3 size, Material m) =>
                CrossRoadKit.Block(nm, transform, pos, size, m);

            Part("Body", new Vector3(0f, 0.26f, 0f), new Vector3(0.52f, 0.44f, 0.60f), matBody);
            Part("Head", new Vector3(0f, 0.54f, 0.16f), new Vector3(0.36f, 0.32f, 0.34f), matBody);
            Part("Beak", new Vector3(0f, 0.50f, 0.40f), new Vector3(0.16f, 0.09f, 0.16f), matBeak);
            Part("Comb", new Vector3(0f, 0.76f, 0.10f), new Vector3(0.10f, 0.16f, 0.24f), matComb);
            Part("EyeL", new Vector3(-0.12f, 0.60f, 0.30f), new Vector3(0.06f, 0.06f, 0.06f), matEye);
            Part("EyeR", new Vector3(0.12f, 0.60f, 0.30f), new Vector3(0.06f, 0.06f, 0.06f), matEye);
            Part("WingL", new Vector3(-0.29f, 0.26f, -0.02f), new Vector3(0.07f, 0.22f, 0.34f), matBody);
            Part("WingR", new Vector3(0.29f, 0.26f, -0.02f), new Vector3(0.07f, 0.22f, 0.34f), matBody);
        }
    }
}
