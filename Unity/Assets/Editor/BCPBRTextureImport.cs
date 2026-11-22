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
            Debug.Log("BATTLE_CLUBS_PBR_IMPORTED=" + guids.Length);
        }
    }
}
#endif
