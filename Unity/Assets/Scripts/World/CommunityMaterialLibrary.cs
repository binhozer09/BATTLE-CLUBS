using System.Collections.Generic;
using UnityEngine;
using BattleClubs.Art;

namespace BattleClubs.World
{
    /// <summary>
    /// Shares runtime materials across procedural environment props. Per-renderer
    /// texture tiling is applied with MaterialPropertyBlock so different-sized
    /// buildings can share one material without losing their surface scale.
    /// </summary>
    public static class CommunityMaterialLibrary
    {
        private struct MaterialKey
        {
            public Color32 color;
            public int textureId;

            public MaterialKey(Color tint, Texture texture)
            {
                color = tint;
                textureId = texture != null ? texture.GetInstanceID() : 0;
            }

            public override int GetHashCode()
            {
                unchecked { return (color.GetHashCode() * 397) ^ textureId; }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is MaterialKey)) return false;
                MaterialKey other = (MaterialKey)obj;
                return color.Equals(other.color) && textureId == other.textureId;
            }
        }

        private static readonly Dictionary<MaterialKey, Material> Materials =
            new Dictionary<MaterialKey, Material>();
        private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();
        private static Shader cachedShader;

        public static Material Get(Color color, Texture texture = null)
        {
            MaterialKey key = new MaterialKey(color, texture);
            if (Materials.TryGetValue(key, out Material material) && material != null)
                return material;

            if (cachedShader == null)
            {
                cachedShader = Shader.Find("Universal Render Pipeline/Lit");
                if (cachedShader == null) cachedShader = Shader.Find("Standard");
            }

            if (cachedShader == null) return null;

            material = new Material(cachedShader)
            {
                name = "BC_Community_" + key.color.r + "_" + key.color.g + "_" +
                    key.color.b + "_" + key.textureId,
                color = color,
                mainTexture = texture
            };
            Materials[key] = material;
            return material;
        }

        public static void ApplySurface(Renderer renderer, Color tint,
            ProceduralSurfaceTextures.Surface surface, Vector2 tiling)
        {
            if (renderer == null) return;
            Texture texture = ProceduralSurfaceTextures.Get(surface);
            renderer.sharedMaterial = Get(tint, texture);
            SetTextureScale(renderer, tiling);
        }

        public static void SetTextureScale(Renderer renderer, Vector2 scale)
        {
            if (renderer == null) return;
            renderer.GetPropertyBlock(PropertyBlock);
            Vector4 tiling = new Vector4(scale.x, scale.y, 0f, 0f);
            PropertyBlock.SetVector("_BaseMap_ST", tiling);
            PropertyBlock.SetVector("_MainTex_ST", tiling);
            renderer.SetPropertyBlock(PropertyBlock);
        }
    }
}
