#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BattleClubs.Editor
{
    /// <summary>Downloads and imports the CC0 KinemIQ rig for the automated Android build.</summary>
    public static class HumanoidAssetBootstrap
    {
        private const string DownloadUrl = "https://github.com/Kinemiq/free-game-animations/releases/download/v1.0/KinemIQ_Free_Animations_CC0_FBX.zip";
        private const string ExpectedSha256 = "4ecea37e2117a762c8d2be10c7e0f06d9c7c9005af0f9caa64c8ce5d424efb0e";
        private const string PackageRoot = "Assets/ThirdParty/KinemIQ";
        private const string ArchivePath = "Temp/BattleClubs_KinemIQ_CC0_FBX.zip";

        [MenuItem("BATTLE CLUBS/Characters/Download CC0 Humanoid + Animations")]
        public static void DownloadFromMenu()
        {
            try
            {
                string model = EnsureHumanoidModelImported();
                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "Modelo humanoide importado em " + model + ". O rig foi configurado como Humanoid.",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("BATTLE CLUBS", ex.Message, "OK");
            }
        }

        public static string EnsureHumanoidModelImported()
        {
            string modelPath = FindMannequin();
            if (string.IsNullOrEmpty(modelPath))
            {
                string archive = Path.GetFullPath(ArchivePath);
                Directory.CreateDirectory(Path.GetDirectoryName(archive));
                using (UnityWebRequest request = UnityWebRequest.Get(DownloadUrl))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = 180;
                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                    while (!operation.isDone) Thread.Sleep(25);
                    if (request.result != UnityWebRequest.Result.Success)
                        throw new Exception("Falha ao baixar o modelo humanoide KinemIQ: " + request.error);
                    File.WriteAllBytes(archive, request.downloadHandler.data);
                }

                try
                {
                    string hash = ComputeSha256(archive);
                    if (!string.Equals(hash, ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("O pacote humanoide foi rejeitado: SHA-256 não confere.");

                    string destination = Path.GetFullPath(PackageRoot);
                    Directory.CreateDirectory(destination);
                    ExtractSafely(archive, destination);
                }
                finally
                {
                    if (File.Exists(archive)) File.Delete(archive);
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                modelPath = FindMannequin();
            }

            if (string.IsNullOrEmpty(modelPath))
                throw new Exception("O pacote KinemIQ foi baixado, mas KinemIQ_Mannequin.fbx não foi encontrado.");

            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
                throw new Exception("O modelo humanoide não possui um importador FBX válido: " + modelPath);

            bool reimport = importer.animationType != ModelImporterAnimationType.Human ||
                            importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
            if (reimport)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Animator animator = model != null ? model.GetComponentInChildren<Animator>(true) : null;
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.isHuman)
                throw new Exception("O FBX foi importado, mas não gerou um Avatar Humanoid válido: " + modelPath);

            Debug.Log("[BATTLE CLUBS] HUMANOID_MODEL_READY=" + modelPath);
            return modelPath;
        }

        private static string FindMannequin()
        {
            string[] guids = AssetDatabase.FindAssets("KinemIQ_Mannequin t:Model", new[] { PackageRoot });
            return guids.Length == 0 ? null : AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        private static void ExtractSafely(string zipPath, string destination)
        {
            string root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
            using (FileStream file = File.OpenRead(zipPath))
            using (ZipArchive archive = new ZipArchive(file, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    string target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
                    if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Entrada ZIP fora da pasta permitida: " + entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    entry.ExtractToFile(target, true);
                }
            }
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
