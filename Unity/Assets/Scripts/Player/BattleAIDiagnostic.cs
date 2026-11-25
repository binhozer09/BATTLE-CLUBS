using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace BattleClubs.Player
{
    /// <summary>
    /// Collects runtime errors, structured telemetry and image evidence.
    /// If an HTTPS ingest endpoint is configured, uploads evidence automatically.
    /// An external AI service must analyze the uploaded package; this client does
    /// not pretend to contain or execute an AI model.
    /// </summary>
    [DefaultExecutionOrder(30000)]
    public sealed class BattleAIDiagnostic : MonoBehaviour
    {
        private static BattleAIDiagnostic instance;
        [Header("Capture")]
        [SerializeField] private Camera diagnosticCamera;
        [SerializeField, Min(1f)] private float screenshotCooldown = 8f;
        [SerializeField, Min(0.1f)] private float incidentFrameInterval = 0.5f;
        [SerializeField, Min(1f)] private float incidentCaptureSeconds = 5f;
        [SerializeField, Min(1)] private int maxFramesPerIncident = 10;
        [Header("Remote evidence receiver")]
        [Tooltip("HTTPS endpoint that accepts multipart/form-data: metadata and evidence files.")]
        [SerializeField] private string uploadEndpoint = "";
        [SerializeField, Min(10f)] private float uploadTimeoutSeconds = 45f;
        [SerializeField] private bool uploadOnWifiOrMobileData = true;

        private float nextCaptureTime;
        private int issueCount;
        private string lastIssue = "Nenhum erro registrado";
        private string evidenceDirectory;
        private bool capturingIncident;
        private bool uploading;
        private float nextErrorUploadTime;
        private float lastFrameTime;
        private float incidentEndTime;
        private int incidentFrameCount;
        private string incidentStamp;
        private readonly StringBuilder runtimeLog = new StringBuilder(4096);

        public static void Report(string code, string message, Vector3 position)
        {
            if (instance != null) instance.RecordIssue(code, message, position);
            else Debug.LogWarning("[BATTLE_AI_DIAGNOSTIC] " + code + ": " + message + " | posição=" + position);
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(this); return; }
            instance = this;
            if (diagnosticCamera == null) diagnosticCamera = GetComponent<Camera>();
            if (diagnosticCamera == null) diagnosticCamera = Camera.main;
            evidenceDirectory = Path.Combine(Application.persistentDataPath, "BattleAIDiagnostic");
            try { Directory.CreateDirectory(evidenceDirectory); }
            catch (Exception e) { Debug.LogWarning("[BATTLE_AI_DIAGNOSTIC] Pasta indisponível: " + e.Message); }
            Application.logMessageReceived += HandleUnityLog;
            Debug.Log("[BATTLE_AI_DIAGNOSTIC] Monitor iniciado. Evidências: " + evidenceDirectory);
            if (string.IsNullOrWhiteSpace(uploadEndpoint))
                Debug.LogWarning("[BATTLE_AI_DIAGNOSTIC] Upload remoto aguardando configuração de um endpoint HTTPS.");
            else if (!uploadEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                Debug.LogError("[BATTLE_AI_DIAGNOSTIC] Endpoint rejeitado: use HTTPS.");
        }

        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string entry = DateTime.UtcNow.ToString("o") + " | " + type + " | " + condition
                + (string.IsNullOrEmpty(stackTrace) ? "" : "\n" + stackTrace);
            runtimeLog.AppendLine(entry);
            if (runtimeLog.Length > 48000) runtimeLog.Remove(0, runtimeLog.Length - 36000);
            try { File.AppendAllText(Path.Combine(evidenceDirectory, "unity-errors.log"), entry + Environment.NewLine); }
            catch { }

            // Forward ordinary Unity exceptions/errors too; throttle bursts so a
            // crash loop cannot flood the paired device's evidence endpoint.
            if (Time.unscaledTime >= nextErrorUploadTime)
            {
                nextErrorUploadTime = Time.unscaledTime + 15f;
                BattleClubsRemoteAgent.UploadEvidence(evidenceDirectory);
            }
        }

        private void RecordIssue(string code, string message, Vector3 position)
        {
            issueCount++;
            lastIssue = code;
            incidentStamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            string line = "{\"timeUtc\":\"" + DateTime.UtcNow.ToString("o")
                + "\",\"code\":\"" + Escape(code) + "\",\"message\":\"" + Escape(message)
                + "\",\"position\":\"" + Escape(position.ToString("F3"))
                + "\",\"playerFps\":" + (1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f)).ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                + ",\"camera\":\"" + Escape(diagnosticCamera != null ? diagnosticCamera.transform.position.ToString("F3") : "missing")
                + "\"}";
            Debug.LogError("[BATTLE_AI_DIAGNOSTIC] " + line);
            try { File.AppendAllText(Path.Combine(evidenceDirectory, "issues.jsonl"), line + Environment.NewLine); }
            catch (Exception e) { Debug.LogWarning("[BATTLE_AI_DIAGNOSTIC] Falha ao salvar log: " + e.Message); }

            if (!capturingIncident && diagnosticCamera != null)
            {
                capturingIncident = true;
                incidentFrameCount = 0;
                incidentEndTime = Time.unscaledTime + incidentCaptureSeconds;
                lastFrameTime = -999f;
                StartCoroutine(CaptureIncidentFrames());
            }
            if (Time.unscaledTime >= nextCaptureTime)
                nextCaptureTime = Time.unscaledTime + screenshotCooldown;
        }

        private IEnumerator CaptureIncidentFrames()
        {
            while (incidentFrameCount < maxFramesPerIncident && Time.unscaledTime <= incidentEndTime)
            {
                if (Time.unscaledTime - lastFrameTime >= incidentFrameInterval)
                {
                    lastFrameTime = Time.unscaledTime;
                    string file = Path.Combine(evidenceDirectory,
                        "incident_" + incidentStamp + "_frame_" + incidentFrameCount.ToString("D2") + ".png");
                    ScreenCapture.CaptureScreenshot(file);
                    incidentFrameCount++;
                }
                yield return null;
            }
            // ScreenCapture writes asynchronously; leave time for the final frame.
            yield return new WaitForSecondsRealtime(1.5f);
            WriteSessionSummary();
            capturingIncident = false;
            TryUploadEvidence();
            BattleClubsRemoteAgent.UploadEvidence(evidenceDirectory);
        }

        private void WriteSessionSummary()
        {
            try
            {
                string summary = "BATTLE CLUBS DIAGNOSTIC\nUTC: " + DateTime.UtcNow.ToString("o")
                    + "\nUnity: " + Application.unityVersion + "\nDevice: " + SystemInfo.deviceModel
                    + "\nOS: " + SystemInfo.operatingSystem + "\nGraphics: " + SystemInfo.graphicsDeviceName
                    + "\nScreen: " + Screen.width + "x" + Screen.height + "\nFPS atual: "
                    + (1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f)).ToString("F1")
                    + "\nOcorrências nesta sessão: " + issueCount + "\nÚltima ocorrência: " + lastIssue
                    + "\nFrames capturados no incidente: " + incidentFrameCount + "\n";
                File.WriteAllText(Path.Combine(evidenceDirectory, "session-summary.txt"), summary);
            }
            catch (Exception e) { Debug.LogWarning("[BATTLE_AI_DIAGNOSTIC] Falha no resumo: " + e.Message); }
        }

        private void TryUploadEvidence()
        {
            if (uploading || string.IsNullOrWhiteSpace(uploadEndpoint)
                || !uploadEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return;
            if (!uploadOnWifiOrMobileData || Application.internetReachability == NetworkReachability.NotReachable) return;
            StartCoroutine(UploadEvidence());
        }

        private IEnumerator UploadEvidence()
        {
            uploading = true;
            string[] files;
            try { files = Directory.GetFiles(evidenceDirectory); }
            catch { uploading = false; yield break; }
            WWWForm form = new WWWForm();
            form.AddField("project", "BATTLE_CLUBS");
            form.AddField("device", SystemInfo.deviceModel);
            form.AddField("os", SystemInfo.operatingSystem);
            form.AddField("unityVersion", Application.unityVersion);
            form.AddField("issueCount", issueCount.ToString());
            int attached = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileName(files[i]);
                if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    byte[] bytes;
                    try { bytes = File.ReadAllBytes(files[i]); } catch { continue; }
                    string mime = name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "text/plain";
                    form.AddBinaryData("evidence", bytes, name, mime);
                    attached++;
                }
            }
            if (attached == 0) { uploading = false; yield break; }
            using (UnityWebRequest request = UnityWebRequest.Post(uploadEndpoint, form))
            {
                request.timeout = Mathf.CeilToInt(uploadTimeoutSeconds);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    Debug.Log("[BATTLE_AI_DIAGNOSTIC] Evidências enviadas. HTTP " + request.responseCode);
                else
                    Debug.LogWarning("[BATTLE_AI_DIAGNOSTIC] Upload falhou; evidências mantidas localmente. " + request.error);
            }
            uploading = false;
        }

        private static string Escape(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\r", " ").Replace("\n", " ");
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            GUI.Box(new Rect(10f, 10f, 280f, 66f),
                "BATTLE AI DIAGNOSTIC\nErros: " + issueCount + "\nÚltimo: " + lastIssue
                + "\nUpload: " + (uploading ? "enviando" : (string.IsNullOrWhiteSpace(uploadEndpoint) ? "não configurado" : "pronto")));
#endif
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleUnityLog;
            if (instance == this) instance = null;
        }
    }
}
