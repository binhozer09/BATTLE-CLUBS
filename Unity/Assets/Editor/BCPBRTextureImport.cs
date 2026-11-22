#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BattleClubs.Editor
{
    public static class BCPBRTextureImport
    {
        public static void ConfigureAll()
        {
            const string folder = "Assets/Resources/BC_PBR";
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (file.EndsWith("_NormalGL", StringComparison.OrdinalIgnoreCase))
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.sRGBTexture = false;
                    importer.isReadable = false;
                }
                else if (file.EndsWith("_Roughness", StringComparison.OrdinalIgnoreCase))
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = false;
                    importer.isReadable = true;
                }
                else
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = true;
                    importer.isReadable = false;
                }
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string[] roughnessGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (string guid in roughnessGuids)
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
                string sourceName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
                if (!sourceName.EndsWith("_Roughness", StringComparison.OrdinalIgnoreCase)) continue;

                Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
                if (source == null || !source.isReadable) continue;
                Color32[] pixels = source.GetPixels32();
                var packed = new Color32[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte rough = (byte)((pixels[i].r + pixels[i].g + pixels[i].b) / 3);
                    packed[i] = new Color32(0, 0, 0, (byte)(255 - rough));
                }

                var smoothness = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
                smoothness.SetPixels32(packed);
                smoothness.Apply(false, false);
                string outputPath = System.IO.Path.Combine(folder,
                    sourceName.Substring(0, sourceName.Length - "_Roughness".Length) + "_MetallicSmoothness.png");
                System.IO.File.WriteAllBytes(outputPath, smoothness.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(smoothness);

                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
                TextureImporter packedImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;
                if (packedImporter != null)
                {
                    packedImporter.sRGBTexture = false;
                    packedImporter.isReadable = false;
                    packedImporter.mipmapEnabled = true;
                    packedImporter.textureCompression = TextureImporterCompression.Compressed;
                    packedImporter.SaveAndReimport();
                }
            }
            Debug.Log("BATTLE_CLUBS_PBR_IMPORTED=" + guids.Length + "; smoothness maps generated=" + roughnessGuids.Length);
        }
    }
}
#endif
