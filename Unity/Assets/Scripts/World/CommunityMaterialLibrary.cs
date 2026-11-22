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
            public int normalId;
            public int roughnessId;

            public MaterialKey(Color tint, Texture texture, Texture normal, Texture roughness)
            {
                color = tint;
                textureId = texture != null ? texture.GetInstanceID() : 0;
                normalId = normal != null ? normal.GetInstanceID() : 0;
                roughnessId = roughness != null ? roughness.GetInstanceID() : 0;
            }

            public override int GetHashCode()
            {
                unchecked { return (((color.GetHashCode() * 397) ^ textureId) * 397 ^ normalId) * 397 ^ roughnessId; }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is MaterialKey)) return false;
                MaterialKey other = (MaterialKey)obj;
                return color.Equals(other.color) && textureId == other.textureId &&
                    normalId == other.normalId && roughnessId == other.roughnessId;
            }
        }

        private static readonly Dictionary<MaterialKey, Material> Materials =
            new Dictionary<MaterialKey, Material>();
        private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();
        private static Shader cachedShader;

        public static Material Get(Color color, Texture texture = null, Texture normal = null, Texture roughness = null)
        {
            MaterialKey key = new MaterialKey(color, texture, normal, roughness);
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
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (roughness != null && material.HasProperty("_MetallicGlossMap"))
            {
                material.SetTexture("_MetallicGlossMap", roughness);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 1f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f);
                material.EnableKeyword("_METALLICGLOSSMAP");
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            Materials[key] = material;
            return material;
        }

        public static void ApplySurface(Renderer renderer, Color tint,
            ProceduralSurfaceTextures.Surface surface, Vector2 tiling)
        {
            if (renderer == null) return;
            Texture texture = ProceduralSurfaceTextures.Get(surface);
            Texture normal = ProceduralSurfaceTextures.GetNormal(surface);
            Texture roughness = ProceduralSurfaceTextures.GetRoughness(surface);
            renderer.sharedMaterial = Get(tint, texture, normal, roughness);
            SetTextureScale(renderer, tiling);
        }

        public static void SetTextureScale(Renderer renderer, Vector2 scale)
        {
            if (renderer == null) return;
            renderer.GetPropertyBlock(PropertyBlock);
            Vector4 tiling = new Vector4(scale.x, scale.y, 0f, 0f);
            PropertyBlock.SetVector("_BaseMap_ST", tiling);
            PropertyBlock.SetVector("_MainTex_ST", tiling);
            PropertyBlock.SetVector("_BumpMap_ST", tiling);
            PropertyBlock.SetVector("_MetallicGlossMap_ST", tiling);
            renderer.SetPropertyBlock(PropertyBlock);
        }
    }
}
