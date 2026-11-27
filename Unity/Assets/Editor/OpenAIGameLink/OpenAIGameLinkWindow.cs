// OpenAI GameLink - Unity Editor bridge (safe, opt-in command execution).
// This is an editor plugin, not an in-game runtime component.
// Remote AI requires a separately hosted relay; never place an OpenAI API key in Unity or an APK.
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class OpenAIGameLinkWindow : EditorWindow
{
    [Serializable] private class SceneReport
    {
        public string project;
        public string scene;
        public string unityVersion;
        public string capturedAtUtc;
        public List<ObjectReport> objects = new List<ObjectReport>();
    }
    [Serializable] private class ObjectReport
    {
        public string path;
        public string name;
        public string tag;
        public string layer;
        public bool active;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public string[] components;
        public string[] rendererMaterials;
        public float[] boundsCenter;
        public float[] boundsSize;
    }
    [Serializable] private class RelayCommand
    {
        public string id;
        public string action;
        public string targetPath;
        public float[] position;
        public float[] rotationEuler;
        public float[] scale;
        public string note;
    }
    [Serializable] private class CommandEnvelope { public RelayCommand[] commands; }
    [Serializable] private class RelayResponse { public bool ok; public string message; public string analysis; public string error; }

    private string relayUrl = "https://YOUR-RELAY.example";
    private string accessToken = "";
    private bool allowRemoteTransformCommands;
    private bool polling;
    private double nextPoll;
    private string status = "Desconectado";
    private Vector2 scroll;

    [MenuItem("Vilarejo/OLHO DA IA - GameLink")]
    public static void Open() { GetWindow<OpenAIGameLinkWindow>("OLHO DA IA"); }

    private void OnEnable()
    {
        EditorApplication.update += UpdatePolling;
        relayUrl = EditorPrefs.GetString("OpenAIGameLink.Url", relayUrl);
        accessToken = SessionState.GetString("OpenAIGameLink.Token", "");
        allowRemoteTransformCommands = EditorPrefs.GetBool("OpenAIGameLink.AllowTransforms", false);
        polling = EditorPrefs.GetBool("OpenAIGameLink.Polling", false);
        nextPoll = EditorApplication.timeSinceStartup + 1;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePolling;
        EditorPrefs.SetString("OpenAIGameLink.Url", relayUrl);
        SessionState.SetString("OpenAIGameLink.Token", accessToken);
        EditorPrefs.SetBool("OpenAIGameLink.AllowTransforms", allowRemoteTransformCommands);
        EditorPrefs.SetBool("OpenAIGameLink.Polling", polling);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox("Conecta o Editor Unity a um serviço intermediário autorizado. A chave da OpenAI fica no servidor, nunca no projeto ou APK. O token do relay permanece apenas nesta sessão do Editor.", MessageType.Info);
        EditorGUILayout.LabelField("Conexão", EditorStyles.boldLabel);
        relayUrl = EditorGUILayout.TextField("URL do relay", relayUrl);
        accessToken = EditorGUILayout.PasswordField("Token de acesso", accessToken);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Testar conexão")) TestConnection();
        if (GUILayout.Button("Enviar diagnóstico da cena")) SendSceneReport();
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Capturar Scene View e enviar")) CaptureAndSend();
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Controle remoto", EditorStyles.boldLabel);
        allowRemoteTransformCommands = EditorGUILayout.ToggleLeft("Permitir comandos de mover/rotacionar/redimensionar objetos", allowRemoteTransformCommands);
        EditorGUILayout.HelpBox("Por segurança, comandos remotos ficam desativados por padrão. Mesmo ativados, só aceitam Transform em objetos existentes, sem executar código, apagar objetos ou importar assets.", MessageType.Warning);
        polling = EditorGUILayout.ToggleLeft("Manter consulta de comandos ativa enquanto esta janela estiver aberta", polling);
        EditorGUILayout.LabelField("Estado: " + status, EditorStyles.miniBoldLabel);
        if (GUILayout.Button("Abrir pasta de relatórios")) EditorUtility.RevealInFinder(ReportFolder());
        EditorGUILayout.EndScrollView();
    }

    private string BaseUrl { get { return (relayUrl ?? "").Trim().TrimEnd('/'); } }
    private string ReportFolder()
    {
        string p = Path.Combine(Application.dataPath, "../GameLinkReports");
        if (!Directory.Exists(p)) Directory.CreateDirectory(p);
        return Path.GetFullPath(p);
    }

    private void TestConnection()
    {
        if (!ValidateConfig()) return;
        StartCoroutine(SendJson("/v1/unity/ping", "{\"unityVersion\":\"" + Escape(Application.unityVersion) + "\"}", r =>
        { status = "Relay respondeu: " + r; Repaint(); }));
    }

    private bool ValidateConfig()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl) || BaseUrl.Contains("YOUR-RELAY") || !BaseUrl.StartsWith("https://"))
        { status = "Configure uma URL HTTPS válida do relay."; Repaint(); return false; }
        if (string.IsNullOrWhiteSpace(accessToken))
        { status = "Informe o token de acesso fornecido pelo relay."; Repaint(); return false; }
        return true;
    }

    private SceneReport BuildReport()
    {
        SceneReport report = new SceneReport {
            project = Application.productName,
            scene = SceneManager.GetActiveScene().path,
            unityVersion = Application.unityVersion,
            capturedAtUtc = DateTime.UtcNow.ToString("o")
        };
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (EditorUtility.IsPersistent(go) || go.hideFlags != HideFlags.None) continue;
            Scene s = go.scene;
            if (!s.IsValid() || s != SceneManager.GetActiveScene()) continue;
            if (report.objects.Count >= 350) break; // Mantém o JSON dentro do limite aceito pelo relay.
            Renderer renderer = go.GetComponent<Renderer>();
            Bounds b = renderer ? renderer.bounds : new Bounds(go.transform.position, Vector3.zero);
            report.objects.Add(new ObjectReport {
                path = GetPath(go.transform), name = go.name, tag = go.tag, layer = LayerMask.LayerToName(go.layer),
                active = go.activeInHierarchy, position = V3(go.transform.position), rotation = V3(go.transform.eulerAngles),
                scale = V3(go.transform.localScale), components = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray(),
                rendererMaterials = renderer ? renderer.sharedMaterials.Where(m => m != null).Select(m => m.name).ToArray() : new string[0],
                boundsCenter = V3(b.center), boundsSize = V3(b.size)
            });
        }
        return report;
    }

    private void SendSceneReport()
    {
        SceneReport report = BuildReport();
        string json = JsonUtility.ToJson(report, true);
        string path = Path.Combine(ReportFolder(), "scene-report-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json");
        File.WriteAllText(path, json, Encoding.UTF8);
        if (!ValidateConfig()) { status = "Relatório salvo localmente: " + path; Repaint(); return; }
        StartCoroutine(SendJson("/v1/unity/scene-report", json, r => { status = "Diagnóstico enviado. " + r; Repaint(); }));
    }

    private void CaptureAndSend()
    {
        if (!ValidateConfig()) return;
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null || view.camera == null) { status = "Abra uma Scene View antes de capturar."; Repaint(); return; }

        string path = Path.Combine(ReportFolder(), "scene-view-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".png");
        Camera sceneCamera = view.camera;
        int w = Mathf.Max(1, sceneCamera.pixelWidth), h = Mathf.Max(1, sceneCamera.pixelHeight);
        RenderTexture previousTarget = sceneCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture temp = null;
        Texture2D image = null;

        try
        {
            temp = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            sceneCamera.targetTexture = temp;
            sceneCamera.Render();
            RenderTexture.active = temp;
            image = new Texture2D(w, h, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        catch (Exception e)
        {
            status = "Falha ao capturar a Scene View: " + e.Message;
            Repaint();
            return;
        }
        finally
        {
            sceneCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (temp != null) RenderTexture.ReleaseTemporary(temp);
            if (image != null) DestroyImmediate(image);
        }

        StartCoroutine(SendImage(path));
    }

    private IEnumerator SendImage(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", bytes, Path.GetFileName(path), "image/png");
        using (UnityWebRequest req = UnityWebRequest.Post(BaseUrl + "/v1/unity/scene-image", form))
        {
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success) status = "Análise da imagem: " + FormatRelayResponse(req.downloadHandler.text);
            else status = "Falha ao enviar imagem: " + req.error;
            Repaint();
        }
    }

    private void UpdatePolling()
    {
        if (!polling || !isActiveAndEnabled || EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 5.0;
        if (!ValidateConfig()) { polling = false; return; }
        StartCoroutine(PollCommands());
    }

    private IEnumerator PollCommands()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(BaseUrl + "/v1/unity/commands"))
        {
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { status = "Consulta: " + req.error; Repaint(); yield break; }
            CommandEnvelope envelope = null;
            try { envelope = JsonUtility.FromJson<CommandEnvelope>(req.downloadHandler.text); }
            catch (Exception e) { status = "Resposta inválida: " + e.Message; Repaint(); yield break; }
            if (envelope == null || envelope.commands == null) yield break;
            foreach (RelayCommand command in envelope.commands) ApplyCommand(command);
        }
    }

    private void ApplyCommand(RelayCommand c)
    {
        if (!allowRemoteTransformCommands) { status = "Comando recebido, mas execução remota está desativada."; Repaint(); return; }
        if (c == null || string.IsNullOrEmpty(c.targetPath) || c.action != "set_transform") return;
        Transform target = FindByPath(c.targetPath);
        if (target == null) { status = "Alvo não encontrado: " + c.targetPath; Repaint(); return; }
        Undo.RecordObject(target, "OLHO DA IA - ajustar objeto");
        if (c.position != null && c.position.Length == 3) target.localPosition = new Vector3(c.position[0], c.position[1], c.position[2]);
        if (c.rotationEuler != null && c.rotationEuler.Length == 3) target.localEulerAngles = new Vector3(c.rotationEuler[0], c.rotationEuler[1], c.rotationEuler[2]);
        if (c.scale != null && c.scale.Length == 3) target.localScale = new Vector3(Mathf.Clamp(c.scale[0], .01f, 100f), Mathf.Clamp(c.scale[1], .01f, 100f), Mathf.Clamp(c.scale[2], .01f, 100f));
        EditorUtility.SetDirty(target);
        status = "Transform ajustado: " + c.targetPath + (string.IsNullOrEmpty(c.note) ? "" : " — " + c.note);
        Repaint();
    }

    private IEnumerator SendJson(string endpoint, string json, Action<string> done)
    {
        using (UnityWebRequest req = new UnityWebRequest(BaseUrl + endpoint, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success) done(FormatRelayResponse(req.downloadHandler.text));
            else { status = "Erro: " + req.error; Repaint(); }
        }
    }

    private static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
    private static Transform FindByPath(string path)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform found = root.transform.Find(path == root.name ? "" : path.StartsWith(root.name + "/") ? path.Substring(root.name.Length + 1) : path);
            if (found != null) return found;
            if (root.name == path) return root.transform;
        }
        return null;
    }
    private static string FormatRelayResponse(string raw)
    {
        try {
            RelayResponse response = JsonUtility.FromJson<RelayResponse>(raw);
            if (response == null) return raw;
            if (!string.IsNullOrEmpty(response.error)) return response.error;
            if (!string.IsNullOrEmpty(response.analysis)) return response.analysis;
            if (!string.IsNullOrEmpty(response.message)) return response.message;
            return raw;
        } catch { return raw; }
    }
    private static float[] V3(Vector3 v) { return new[] { v.x, v.y, v.z }; }
    private static string Escape(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }
}
#endif
