using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace BattleClubs.Player
{
    /// <summary>
    /// Secure, allow-listed remote tuning client. It never downloads or executes code.
    /// Pairing is performed by entering the private admin code once on the device.
    /// </summary>
    public sealed class BattleClubsRemoteAgent : MonoBehaviour
    {
        private const string Server = "https://battle-clubs-remote-agent-production.up.railway.app";
        private const string TokenKey = "BATTLE_CLUBS_REMOTE_DEVICE_TOKEN";
        private static BattleClubsRemoteAgent instance;
        private string pairingCode = "";
        private string status = "Agente desconectado";
        private bool showPairing;
        private bool busy;
        private float nextPoll;

        [Serializable] private class PairRequest { public string deviceName; }
        [Serializable] private class PairResponse { public string deviceId; public string deviceToken; public int pollSeconds; }
        [Serializable] private class PollResponse { public RemoteCommand[] commands; }
        [Serializable] private class RemoteCommand { public string id; public string type; public RemoteConfig payload; }
        [Serializable] private class RemoteConfig
        {
            public float cameraFov;
            public int targetFrameRate;
            public float moveSpeed;
            public float sprintSpeed;
            public float cameraSensitivity;
            public float shadowDistance;
            public float masterVolume;
        }
        [Serializable] private class AckRequest { public string commandId; public string result; }

        public static void UploadEvidence(string directory)
        {
            if (instance != null && !string.IsNullOrEmpty(PlayerPrefs.GetString(TokenKey, "")))
                instance.StartCoroutine(instance.UploadFiles(directory));
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(this); return; }
            instance = this;
            status = string.IsNullOrEmpty(PlayerPrefs.GetString(TokenKey, ""))
                ? "Agente: aguardando pareamento" : "Agente conectado; aguardando servidor";
            nextPoll = Time.unscaledTime + 2f;
        }

        private void Update()
        {
            if (busy || Time.unscaledTime < nextPoll) return;
            if (Application.internetReachability == NetworkReachability.NotReachable) return;
            string token = PlayerPrefs.GetString(TokenKey, "");
            if (string.IsNullOrEmpty(token)) return;
            nextPoll = Time.unscaledTime + 8f;
            StartCoroutine(PollCommands(token));
        }

        private IEnumerator Pair()
        {
            if (busy || string.IsNullOrWhiteSpace(pairingCode)) yield break;
            busy = true;
            status = "Pareando com servidor...";
            string json = JsonUtility.ToJson(new PairRequest { deviceName = SystemInfo.deviceModel });
            using (UnityWebRequest req = UnityWebRequest.Post(Server + "/v1/device/pair", json, "application/json"))
            {
                req.SetRequestHeader("X-Pairing-Code", pairingCode.Trim());
                req.timeout = 20;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    PairResponse response = JsonUtility.FromJson<PairResponse>(req.downloadHandler.text);
                    if (response != null && !string.IsNullOrEmpty(response.deviceToken))
                    {
                        PlayerPrefs.SetString(TokenKey, response.deviceToken);
                        PlayerPrefs.Save();
                        status = "Pareado. Controle remoto ativo.";
                        pairingCode = "";
                        showPairing = false;
                    }
                    else status = "Resposta de pareamento inválida";
                }
                else status = "Falha no pareamento: " + req.error;
            }
            busy = false;
        }

        private IEnumerator PollCommands(string token)
        {
            busy = true;
            using (UnityWebRequest req = UnityWebRequest.Get(Server + "/v1/device/poll"))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                req.timeout = 15;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    PollResponse response = JsonUtility.FromJson<PollResponse>(req.downloadHandler.text);
                    if (response != null && response.commands != null)
                    {
                        foreach (RemoteCommand command in response.commands)
                        {
                            string result = ApplyCommand(command);
                            yield return StartCoroutine(Acknowledge(token, command.id, result));
                        }
                        status = response.commands.Length > 0
                            ? "Atualização remota aplicada" : "Conectado ao agente";
                    }
                }
                else if (req.responseCode == 401)
                {
                    PlayerPrefs.DeleteKey(TokenKey);
                    PlayerPrefs.Save();
                    status = "Pareamento expirado; conecte novamente";
                }
                else status = "Servidor indisponível: " + req.error;
            }
            busy = false;
        }

        private string ApplyCommand(RemoteCommand command)
        {
            if (command == null || command.type != "apply_config" || command.payload == null)
                return "comando não permitido";
            RemoteConfig c = command.payload;
            if (c.cameraFov > 0f && Camera.main != null)
                Camera.main.fieldOfView = Mathf.Clamp(c.cameraFov, 40f, 90f);
            if (c.targetFrameRate >= 30)
                Application.targetFrameRate = Mathf.Clamp(c.targetFrameRate, 30, 90);
            if (c.shadowDistance >= 0f)
                QualitySettings.shadowDistance = Mathf.Clamp(c.shadowDistance, 0f, 150f);
            if (c.masterVolume >= 0f)
                AudioListener.volume = Mathf.Clamp01(c.masterVolume);
            ThirdPersonMotor motor = FindObjectOfType<ThirdPersonMotor>();
            if (motor != null && c.moveSpeed > 0f && c.sprintSpeed > 0f)
                motor.ApplyRemoteTuning(c.moveSpeed, c.sprintSpeed);
            ThirdPersonCameraRig rig = FindObjectOfType<ThirdPersonCameraRig>();
            if (rig != null && c.cameraSensitivity > 0f)
                rig.ApplyRemoteSensitivity(c.cameraSensitivity);
            return "configuração permitida aplicada";
        }

        private IEnumerator Acknowledge(string token, string commandId, string result)
        {
            AckRequest ack = new AckRequest { commandId = commandId, result = result };
            using (UnityWebRequest req = UnityWebRequest.Post(Server + "/v1/device/ack",
                JsonUtility.ToJson(ack), "application/json"))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                req.timeout = 15;
                yield return req.SendWebRequest();
            }
        }

        private IEnumerator UploadFiles(string directory)
        {
            // Polling and evidence uploads share one UnityWebRequest slot. Queue the
            // evidence instead of silently dropping it when a poll is in progress.
            float waitDeadline = Time.realtimeSinceStartup + 25f;
            while (busy && Time.realtimeSinceStartup < waitDeadline)
                yield return new WaitForSecondsRealtime(0.25f);
            if (busy || !Directory.Exists(directory)) yield break;

            string token = PlayerPrefs.GetString(TokenKey, "");
            if (string.IsNullOrEmpty(token)) yield break;
            busy = true;
            string[] files = Directory.GetFiles(directory);
            WWWForm form = new WWWForm();
            int count = 0;
            for (int i = 0; i < files.Length && count < 12; i++)
            {
                string ext = Path.GetExtension(files[i]).ToLowerInvariant();
                if (ext != ".png" && ext != ".log" && ext != ".jsonl" && ext != ".txt") continue;
                byte[] bytes;
                try { bytes = File.ReadAllBytes(files[i]); } catch { continue; }
                if (bytes.Length > 3 * 1024 * 1024) continue;
                form.AddBinaryData("evidence", bytes, Path.GetFileName(files[i]),
                    ext == ".png" ? "image/png" : "text/plain");
                count++;
            }
            if (count > 0)
            {
                using (UnityWebRequest req = UnityWebRequest.Post(Server + "/v1/diagnostics", form))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + token);
                    req.timeout = 45;
                    yield return req.SendWebRequest();
                    status = req.result == UnityWebRequest.Result.Success
                        ? "Diagnóstico enviado ao agente" : "Falha ao enviar diagnóstico: " + req.error;
                }
            }
            busy = false;
        }

        private void OnGUI()
        {
            const int width = 300;
            GUI.Box(new Rect(Screen.width - width - 12, 12, width, showPairing ? 150 : 58), "BATTLE CLUBS • AGENTE REMOTO");
            GUI.Label(new Rect(Screen.width - width, 38, width - 20, 24), status);
            if (GUI.Button(new Rect(Screen.width - 112, 62, 92, 24), showPairing ? "Fechar" : "Conectar"))
                showPairing = !showPairing;
            if (!showPairing) return;
            GUI.Label(new Rect(Screen.width - width, 92, width - 20, 20), "Código privado de pareamento:");
            pairingCode = GUI.PasswordField(new Rect(Screen.width - width, 112, width - 20, 24), pairingCode, '*');
            if (GUI.Button(new Rect(Screen.width - 112, 140, 92, 24), "Parear"))
                StartCoroutine(Pair());
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
