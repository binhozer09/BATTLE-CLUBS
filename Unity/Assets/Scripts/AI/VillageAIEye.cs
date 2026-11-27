using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MedievalVillage.AI
{
    /// <summary>
    /// Captures the player's current view and sends one still image to a secure
    /// OpenAI proxy. The OpenAI secret must remain on the server, never in the APK.
    /// </summary>
    public sealed class VillageAIEye : MonoBehaviour
    {
        [Header("Camera and secure server")]
        public Camera targetCamera;
        [Tooltip("HTTPS endpoint of the Village AI proxy: https://your-service/v1/village/inspect")]
        public string inspectionEndpoint = "";

        [Header("Capture")]
        [Range(512, 1536)] public int maxImageDimension = 1024;
        [Range(45, 85)] public int jpegQuality = 68;

        private bool panelOpen;
        private bool busy;
        private string status = "Olho da IA pronto";
        private string report = "Toque em ANALISAR para a IA observar a cena pela câmera do jogador.";
        private Vector2 reportScroll;\n        private string endpointInput = "";

        [Serializable] private sealed class InspectRequest
        {
            public string imageBase64;
            public string mimeType = "image/jpeg";
            public string deviceModel;
            public string sceneName;
            public string instruction;
        }

        [Serializable] private sealed class InspectResponse
        {
            public string analysis;
            public string error;
        }

        private void Awake()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        public void SetInspectionEndpoint(string httpsEndpoint)
        {
            inspectionEndpoint = (httpsEndpoint ?? "").Trim();
        }

        private void OnGUI()
        {
            const float margin = 14f;
            float buttonWidth = Mathf.Clamp(Screen.width * .32f, 150f, 210f);
            const float buttonHeight = 48f;
            GUI.color = new Color(.10f, .12f, .16f, .94f);
            if (GUI.Button(new Rect(margin, margin, buttonWidth, buttonHeight), "◉  OLHO DA IA"))
                panelOpen = !panelOpen;
            if (!panelOpen) { GUI.color = Color.white; return; }

            float panelWidth = Mathf.Min(Screen.width - margin * 2f, 390f);
            float panelHeight = Mathf.Min(Screen.height - 90f, 300f);
            Rect panel = new Rect(margin, margin + buttonHeight + 8f, panelWidth, panelHeight);
            GUI.color = new Color(.08f, .09f, .12f, .97f);
            GUI.Box(panel, "OLHO DA IA  •  VILAREJO MEDIEVAL");
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 12, panel.y + 32, panel.width - 24, 22), status);
            GUI.Label(new Rect(panel.x + 12, panel.y + 54, panel.width - 24, 18), "ENDEREÇO HTTPS DO SERVIDOR");
            endpointInput = GUI.TextField(new Rect(panel.x + 12, panel.y + 73, panel.width - 105, 26), endpointInput);
            if (GUI.Button(new Rect(panel.xMax - 82, panel.y + 73, 70, 26), "SALVAR"))
            {
                if (endpointInput.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    SetInspectionEndpoint(endpointInput);
                    PlayerPrefs.SetString("VILLAGE_AI_ENDPOINT", inspectionEndpoint);
                    PlayerPrefs.Save();
                    status = "Servidor HTTPS salvo neste aparelho.";
                }
                else status = "O endereço precisa começar com https://";
            }

            Rect reportRect = new Rect(panel.x + 12, panel.y + 105, panel.width - 24, panel.height - 158);
            reportScroll = GUI.BeginScrollView(reportRect, reportScroll,
                new Rect(0, 0, reportRect.width - 18, Mathf.Max(reportRect.height, 180)));
            GUI.Label(new Rect(0, 0, reportRect.width - 24, 180), report, WordWrapStyle());
            GUI.EndScrollView();

            float actionY = panel.yMax - 44f;
            GUI.enabled = !busy && !string.IsNullOrWhiteSpace(inspectionEndpoint);
            if (GUI.Button(new Rect(panel.x + 12, actionY, panel.width * .58f, 32f), busy ? "ANALISANDO..." : "ANALISAR CÂMERA"))
                StartCoroutine(CaptureAndInspect());
            GUI.enabled = true;
            if (string.IsNullOrWhiteSpace(inspectionEndpoint))
                GUI.Label(new Rect(panel.x + 12, actionY - 20, panel.width - 24, 18), "Configure o servidor seguro para conectar a OpenAI.");
            if (GUI.Button(new Rect(panel.xMax - 82, actionY, 70, 32f), "FECHAR"))
                panelOpen = false;using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MedievalVillage.AI
{
    /// <summary>
    /// Captures the player's current view and sends one still image to a secure
    /// OpenAI proxy. The OpenAI secret must remain on the server, never in the APK.
    /// </summary>
    public sealed class VillageAIEye : MonoBehaviour
    {
        [Header("Camera and secure server")]
        public Camera targetCamera;
        [Tooltip("HTTPS endpoint of the Village AI proxy: https://your-service/v1/village/inspect")]
        public string inspectionEndpoint = "";

        [Header("Capture")]
        [Range(512, 1536)] public int maxImageDimension = 1024;
        [Range(45, 85)] public int jpegQuality = 68;

        private bool panelOpen;
        private bool busy;
        private string status = "Olho da IA pronto";
        private string report = "Toque em ANALISAR para a IA observar a cena pela câmera do jogador.";
        private Vector2 reportScroll;\n        private string endpointInput = "";

        [Serializable] private sealed class InspectRequest
        {
            public string imageBase64;
            public string mimeType = "image/jpeg";
            public string deviceModel;
            public string sceneName;
            public string instruction;
        }

        [Serializable] private sealed class InspectResponse
        {
            public string analysis;
            public string error;
        }

        private void Awake()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        public void SetInspectionEndpoint(string httpsEndpoint)
        {
            inspectionEndpoint = (httpsEndpoint ?? "").Trim();
        }

        private void OnGUI()
        {
            const float margin = 14f;
            float buttonWidth = Mathf.Clamp(Screen.width * .32f, 150f, 210f);
            const float buttonHeight = 48f;
            GUI.color = new Color(.10f, .12f, .16f, .94f);
            if (GUI.Button(new Rect(margin, margin, buttonWidth, buttonHeight), "◉  OLHO DA IA"))
                panelOpen = !panelOpen;
            if (!panelOpen) { GUI.color = Color.white; return; }

            float panelWidth = Mathf.Min(Screen.width - margin * 2f, 390f);
            float panelHeight = Mathf.Min(Screen.height - 90f, 300f);
            Rect panel = new Rect(margin, margin + buttonHeight + 8f, panelWidth, panelHeight);
            GUI.color = new Color(.08f, .09f, .12f, .97f);
            GUI.Box(panel, "OLHO DA IA  •  VILAREJO MEDIEVAL");
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 12, panel.y + 32, panel.width - 24, 24), status);

            Rect reportRect = new Rect(panel.x + 12, panel.y + 58, panel.width - 24, panel.height - 112);
            reportScroll = GUI.BeginScrollView(reportRect, reportScroll,
                new Rect(0, 0, reportRect.width - 18, Mathf.Max(reportRect.height, 180)));
            GUI.Label(new Rect(0, 0, reportRect.width - 24, 180), report, WordWrapStyle());
            GUI.EndScrollView();

            float actionY = panel.yMax - 44f;
            GUI.enabled = !busy && !string.IsNullOrWhiteSpace(inspectionEndpoint);
            if (GUI.Button(new Rect(panel.x + 12, actionY, panel.width * .58f, 32f), busy ? "ANALISANDO..." : "ANALISAR CÂMERA"))
                StartCoroutine(CaptureAndInspect());
            GUI.enabled = true;
            if (string.IsNullOrWhiteSpace(inspectionEndpoint))
                GUI.Label(new Rect(panel.x + 12, actionY - 20, panel.width - 24, 18), "Servidor seguro ainda não configurado.");
            if (GUI.Button(new Rect(panel.xMax - 82, actionY, 70, 32f), "FECHAR"))
                panelOpen = false;
        }

        private GUIStyle WordWrapStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.wordWrap = true;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.white;
            return style;
        }

        private IEnumerator CaptureAndInspect()
        {
            if (busy) yield break;
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) { status = "Câmera do jogador não encontrada."; yield break; }
            if (!inspectionEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                status = "Configure um endpoint HTTPS seguro no componente VillageAIEye.";
                yield break;
            }
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                status = "Sem conexão com a internet.";
                yield break;
            }

            busy = true;
            status = "Capturando a visão do jogador...";
            yield return new WaitForEndOfFrame();

            Texture2D captured = null;
            Texture2D resized = null;
            RenderTexture rt = null;
            RenderTexture previous = RenderTexture.active;
            string encodedImage = null;
            string captureError = null;

            // Keep capture exceptions separate from the network yield; Unity iterators
            // cannot yield inside a try block that has a catch clause.
            try
            {
                captured = ScreenCapture.CaptureScreenshotAsTexture();
                if (captured == null) throw new Exception("Não foi possível capturar a tela.");
                int largest = Mathf.Max(captured.width, captured.height);
                float scale = Mathf.Min(1f, (float)maxImageDimension / largest);
                int width = Mathf.Max(1, Mathf.RoundToInt(captured.width * scale));
                int height = Mathf.Max(1, Mathf.RoundToInt(captured.height * scale));
                rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(captured, rt);
                RenderTexture.active = rt;
                resized = new Texture2D(width, height, TextureFormat.RGB24, false);
                resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                resized.Apply(false, false);
                encodedImage = Convert.ToBase64String(resized.EncodeToJPG(jpegQuality));
            }
            catch (Exception ex) { captureError = ex.Message; }

            RenderTexture.active = previous;
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
            if (captured != null) Destroy(captured);
            if (resized != null) Destroy(resized);

            if (!string.IsNullOrEmpty(captureError) || string.IsNullOrEmpty(encodedImage))
            {
                status = "Erro ao capturar a câmera.";
                report = captureError ?? "A imagem ficou vazia.";
                busy = false;
                yield break;
            }

            InspectRequest payload = new InspectRequest
            {
                imageBase64 = encodedImage,
                deviceModel = SystemInfo.deviceModel,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                instruction = "Analise esta captura real do jogo medieval. Avalie qualidade visual e montagem da vila: casas completas, alinhamento de paredes, portas, janelas, telhados, colisões visíveis, objetos atravessando outros, escala, iluminação, texturas e composição. Separe problemas observáveis de suposições. Dê correções concretas para Unity; não invente objetos que não aparecem."
            };

            status = "Enviando imagem para a OpenAI...";
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using (UnityWebRequest request = new UnityWebRequest(inspectionEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 75;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    InspectResponse response = JsonUtility.FromJson<InspectResponse>(request.downloadHandler.text);
                    if (response != null && !string.IsNullOrWhiteSpace(response.analysis))
                    {
                        report = response.analysis;
                        status = "Análise concluída • " + DateTime.Now.ToString("HH:mm");
                    }
                    else
                    {
                        status = "Resposta inválida do servidor.";
                        report = response != null ? response.error : request.downloadHandler.text;
                    }
                }
                else
                {
                    status = "Falha na análise: " + request.error + " (" + request.responseCode + ")";
                    report = request.downloadHandler != null ? request.downloadHandler.text : "";
                }
            }

            busy = false;
        }
    }
}
