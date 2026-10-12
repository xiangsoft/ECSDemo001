using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Flat-colour primitive toolkit: cached materials + block factory.
    /// Pure static utility — no state, no scene references.
    /// </summary>
    public static class CrossRoadKit
    {
        private static readonly Dictionary<Color, Material> matCache = new Dictionary<Color, Material>();

        /// Cached flat-colour material (URP Lit if present, else Standard).
        public static Material Mat(Color c)
        {
            if (!matCache.TryGetValue(c, out var m))
            {
                m = CreateMat(c);
                matCache[c] = m;
            }
            return m;
        }

        /// Uncached material instance (for things that change colour at runtime, e.g. signal lamps).
        public static Material MatRaw(Color c) => CreateMat(c);

        private static Material CreateMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }

        /// Flat-coloured cube primitive; collider stripped (logic is code-driven).
        public static GameObject Block(string blockName, Transform parent, Vector3 localPos, Vector3 size, Material mat)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = blockName;
            Object.Destroy(cube.GetComponent<Collider>());
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = size;
            cube.GetComponent<Renderer>().sharedMaterial = mat;
            return cube;
        }
    }
}
