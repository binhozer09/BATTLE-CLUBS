using System;
using System.IO;
using UnityEngine;

namespace BattleClubs.Player
{
    /// <summary>
    /// Runtime diagnostic recorder. Captures the game camera when gameplay
    /// safety systems report an anomaly and writes structured evidence to the
    /// persistent data folder. AI interpretation is performed by an external
    /// analyzer; this component itself does not claim to run an AI model.
    /// </summary>
    [DefaultExecutionOrder(30000)]
    public sealed class BattleAIDiagnostic : MonoBehaviour
    {
        private static BattleAIDiagnostic instance;
        [SerializeField] private Camera diagnosticCamera;
        [SerializeField, Min(1f)] private float screenshotCooldown = 8f;
        private float nextCaptureTime;
        private int issueCount;
        private string lastIssue = "Nenhum erro registrado";
        private string evidenceDirectory;

        public static void Report(string code, string message, Vector3 position)
        {
            if (instance != null) instance.RecordIssue(code, message, position);
            else Debug.LogWarning("[BATTLE_AI_DIAGNOSTIC] " + code + ": " + message
                + " | posição=" + position);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;
            if (diagnosticCamera == null) diagnosticCamera = GetComponent<Camera>();
            if (diagnosticCamera == null) diagnosticCamera = Camera.main;
            evidenceDirectory = Path.Combine(Application.persistentDataPath, "BattleAIDiagnostic");
            try { Directory.CreateDirectory(evidenceDirectory); }
            catch (Exception e) { Debug.LogWarning("[BATTLE_AI_DIAGNOSTIC] Pasta indisponível: " + e.Message); }
            Debug.Log("[BATTLE_AI_DIAGNOSTIC] Monitor iniciado. Evidências: " + evidenceDirectory);
        }

        private void RecordIssue(string code, string message, Vector3 position)
        {
            issueCount++;
            lastIssue = code;
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            string line = "{\"timeUtc\":\"" + DateTime.UtcNow.ToString("o")
                + "\",\"code\":\"" + code + "\",\"message\":\"" + message
                + "\",\"position\":\"" + position.ToString("F3")
                + "\",\"camera\":\"" + (diagnosticCamera != null ? diagnosticCamera.transform.position.ToString("F3") : "missing")
                + "\"}";
            Debug.LogError("[BATTLE_AI_DIAGNOSTIC] " + line);

            try
            {
                if (!string.IsNullOrEmpty(evidenceDirectory))
                    File.AppendAllText(Path.Combine(evidenceDirectory, "issues.jsonl"), line + Environment.NewLine);
            }
            catch (Exception e) { Debug.LogWarning("[BATTLE_AI_DIAGNOSTIC] Não salvou relatório: " + e.Message); }

            if (Time.unscaledTime >= nextCaptureTime && diagnosticCamera != null)
            {
                nextCaptureTime = Time.unscaledTime + screenshotCooldown;
                string screenshotPath = Path.Combine(evidenceDirectory, "issue_" + stamp + ".png");
                ScreenCapture.CaptureScreenshot(screenshotPath);
                Debug.Log("[BATTLE_AI_DIAGNOSTIC] Captura solicitada: " + screenshotPath);
            }
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GUI.color = new Color(1f, 1f, 1f, 0.88f);
            GUI.Box(new Rect(10f, 10f, 250f, 54f),
                "BATTLE AI DIAGNOSTIC\nOcorrências: " + issueCount + "\nÚltimo: " + lastIssue);
#endif
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
