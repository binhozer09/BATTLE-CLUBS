#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BattleClubs.Editor
{
    /// <summary>
    /// Fetches the CC0 KinemIQ humanoid mannequin + animation pack directly
    /// from its published GitHub release. No manual browser download is needed.
    /// </summary>
    [InitializeOnLoad]
    public static class HumanoidAssetBootstrap
    {
        private const string DownloadUrl = "https://github.com/Kinemiq/free-game-animations/releases/download/v1.0/KinemIQ_Free_Animations_CC0_FBX.zip";
        private const string ExpectedSha256 = "4ecea37e2117a762c8d2be10c7e0f06d9c7c9005af0f9caa64c8ce5d424efb0e";
        private const string PackageRoot = "Assets/ThirdParty/KinemIQ";
        private const string ArchivePath = "Temp/BattleClubs_KinemIQ_CC0_FBX.zip";
        private static UnityWebRequest request;
        private static string temporaryArchive;
        private static bool running;

        static HumanoidAssetBootstrap()
        {
            // Deliberately do not download silently on project open.
            // User/editor can start it from the BATTLE CLUBS menu.
        }

        [MenuItem("BATTLE CLUBS/Characters/Download CC0 Humanoid + Animations")]
        public static void Download()
        {
            if (running) return;
            string mannequin = FindMannequin();
            if (!string.IsNullOrEmpty(mannequin))
            {
                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "O pacote KinemIQ já está presente. Modelo encontrado em:\n" + mannequin,
                    "OK");
                return;
            }

            string tempDir = Path.GetFullPath("Temp");
            Directory.CreateDirectory(tempDir);
            temporaryArchive = Path.Combine(tempDir, "BattleClubs_KinemIQ_CC0_FBX.zip");
            request = UnityWebRequest.Get(DownloadUrl);
            request.downloadHandler = new DownloadHandlerFile(temporaryArchive);
            request.timeout = 180;
            running = true;
            request.SendWebRequest();
            EditorApplication.update += PollDownload;
            Debug.Log("[BATTLE CLUBS] Baixando pacote CC0 de humanoide e animações KinemIQ...");
        }

        private static void PollDownload()
        {
            if (!running || request == null || !request.isDone) return;
            EditorApplication.update -= PollDownload;
            running = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[BATTLE CLUBS] Falha ao baixar pacote humanoide: " + request.error);
                CleanupRequest();
                return;
            }

            try
            {
                string hash = ComputeSha256(temporaryArchive);
                if (!string.Equals(hash, ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA-256 não confere. O ZIP foi rejeitado.");

                string destination = Path.GetFullPath(PackageRoot);
                Directory.CreateDirectory(destination);
                ExtractSafely(temporaryArchive, destination);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                string mannequin = FindMannequin();
                if (string.IsNullOrEmpty(mannequin))
                    Debug.LogWarning("[BATTLE CLUBS] Download concluído, mas o FBX do mannequin não foi localizado automaticamente. Confira Assets/ThirdParty/KinemIQ.");
                else
                    Debug.Log("[BATTLE CLUBS] Pacote importado. Modelo humanoide: " + mannequin +
                              ". Configure Rig = Humanoid no FBX e use o assistente de integração já existente.");

                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "Pacote CC0 baixado e extraído em Assets/ThirdParty/KinemIQ.\n\n" +
                    "Próxima etapa: validar o Avatar Humanoid no importador FBX e integrar o modelo ao Player_Root usando o assistente existente.\n\n" +
                    "Este pacote contém um mannequin 3D rigged e 35 animações; não é um personagem fotorrealista final.",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError("[BATTLE CLUBS] Não foi possível importar o pacote: " + ex);
            }
            finally
            {
                try { if (File.Exists(temporaryArchive)) File.Delete(temporaryArchive); } catch { }
                CleanupRequest();
            }
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
            {
                byte[] digest = sha.ComputeHash(stream);
                return BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string FindMannequin()
        {
            string[] guids = AssetDatabase.FindAssets("KinemIQ_Mannequin t:Model", new[] { PackageRoot });
            if (guids.Length == 0) return null;
            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        private static void CleanupRequest()
        {
            if (request != null)
            {
                request.Dispose();
                request = null;
            }
        }
    }
}
#endif
