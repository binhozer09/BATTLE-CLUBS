using System.Collections.Generic;
using UnityEngine;

namespace BattleClubs.World
{
    /// <summary>
    /// Reuses runtime materials for repeated procedural street props. This avoids
    /// allocating a unique Material for every stair, cable support and facade detail.
    /// </summary>
    public static class CommunityMaterialLibrary
    {
        private static readonly Dictionary<Color32, Material> Materials =
            new Dictionary<Color32, Material>();

        private static Shader cachedShader;

        public static Material Get(Color color)
        {
            Color32 key = color;
            if (Materials.TryGetValue(key, out Material material) && material != null)
                return material;

            if (cachedShader == null)
            {
                cachedShader = Shader.Find("Universal Render Pipeline/Lit");
                if (cachedShader == null) cachedShader = Shader.Find("Standard");
            }

            if (cachedShader == null)
                return null;

            material = new Material(cachedShader)
            {
                name = "BC_Community_" + key.r + "_" + key.g + "_" + key.b,
                color = color
            };
            Materials[key] = material;
            return material;
        }
    }
}
