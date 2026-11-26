using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BattleClubs.Player
{
    /// <summary>
    /// Captures ground/contact telemetry around the local CharacterController.
    /// Keeps a short pre-incident buffer and writes it when ground contact is lost.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(25000)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerGroundDiagnostics : MonoBehaviour
    {
        [SerializeField] private CharacterController controller;
        [SerializeField] private ThirdPersonMotor motor;
        [SerializeField, Min(0.1f)] private float sampleInterval = 0.25f;
        [SerializeField, Min(0.5f)] private float fallReportDepth = 1.5f;
        [SerializeField, Min(0.5f)] private float noGroundTimeout = 2.0f;
        [SerializeField, Min(2f)] private float groundProbeDistance = 30f;

        private const int BufferCapacity = 64;
        private readonly string[] samples = new string[BufferCapacity];
        private int sampleHead;
        private int sampleCount;
        private float nextSample;
        private float lastGroundedTime;
        private float lastIncidentTime = -999f;
        private float lastGroundY = float.NaN;
        private string lastContact = "none";
        private string evidenceDirectory;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (motor == null) motor = GetComponent<ThirdPersonMotor>();
            evidenceDirectory = Path.Combine(Application.persistentDataPath, "BattleAIDiagnostic");
            try { Directory.CreateDirectory(evidenceDirectory); } catch (Exception e) { Debug.LogWarning("[GROUND_DIAG] " + e.Message); }
            lastGroundedTime = Time.unscaledTime;
            Sample(true);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextSample)
            {
                nextSample = Time.unscaledTime + sampleInterval;
                Sample(false);
            }

            if (controller == null || !controller.enabled) return;
            float bottom = transform.position.y + controller.center.y - controller.height * 0.5f;
            bool deepBelowGround = !float.IsNaN(lastGroundY) && bottom < lastGroundY - fallReportDepth;
            bool longUngrounded = !controller.isGrounded &&
                Time.unscaledTime - lastGroundedTime > noGroundTimeout &&
                (float.IsNaN(lastGroundY) || transform.position.y < lastGroundY - fallReportDepth);
            if ((deepBelowGround || longUngrounded) && Time.unscaledTime - lastIncidentTime > 5f)
            {
                lastIncidentTime = Time.unscaledTime;
                string details = BuildIncidentDetails(deepBelowGround ? "CAPSULE_BELOW_GROUND" : "GROUND_CONTACT_LOST");
                AppendIncident(details);
                BattleAIDiagnostic.Report(deepBelowGround ? "CAPSULE_BELOW_GROUND" : "GROUND_CONTACT_LOST",
                    details, transform.position);
            }
        }

        private void Sample(bool force)
        {
            if (controller == null) return;
            if (!force && controller.isGrounded) lastGroundedTime = Time.unscaledTime;
            RaycastHit hit;
            bool found = Physics.Raycast(
                transform.position + Vector3.up * Mathf.Max(1f, controller.height),
                Vector3.down, out hit, groundProbeDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (found && hit.collider != null && !hit.collider.transform.IsChildOf(transform))
                lastGroundY = hit.point.y;

            string line = BuildSample(found, hit);
            samples[sampleHead] = line;
            sampleHead = (sampleHead + 1) % BufferCapacity;
            sampleCount = Mathf.Min(sampleCount + 1, BufferCapacity);
        }

        private string BuildSample(bool found, RaycastHit hit)
        {
            Vector3 p = transform.position;
            Vector3 bottom = p + Vector3.up * (controller.center.y - controller.height * 0.5f);
            Vector3 velocity = motor != null
                ? new Vector3(0f, motor.VerticalSpeed, 0f)
                : Vector3.zero;
            string groundName = found && hit.collider != null ? hit.collider.name : "NO_GROUND_HIT";
            string groundPath = found && hit.collider != null ? GetPath(hit.collider.transform) : "";
            return "{\"utc\":\"" + DateTime.UtcNow.ToString("o") +
                "\",\"frame\":" + Time.frameCount +
                ",\"position\":\"" + V3(p) +
                "\",\"capsuleBottom\":\"" + V3(bottom) +
                "\",\"controllerEnabled\":" + controller.enabled.ToString().ToLowerInvariant() +
                ",\"isGrounded\":" + controller.isGrounded.ToString().ToLowerInvariant() +
                ",\"detectCollisions\":" + controller.detectCollisions.ToString().ToLowerInvariant() +
                ",\"height\":" + F(controller.height) +
                ",\"radius\":" + F(controller.radius) +
                ",\"center\":\"" + V3(controller.center) +
                "\",\"skinWidth\":" + F(controller.skinWidth) +
                ",\"slopeLimit\":" + F(controller.slopeLimit) +
                ",\"stepOffset\":" + F(controller.stepOffset) +
                ",\"verticalSpeed\":" + F(motor != null ? motor.VerticalSpeed : 0f) +
                ",\"groundFound\":" + found.ToString().ToLowerInvariant() +
                ",\"groundName\":\"" + Escape(groundName) +
                "\",\"groundPath\":\"" + Escape(groundPath) +
                "\",\"groundLayer\":" + (found && hit.collider != null ? hit.collider.gameObject.layer : -1) +
                ",\"groundIsTrigger\":" + (found && hit.collider != null && hit.collider.isTrigger).ToString().ToLowerInvariant() +
                ",\"groundPoint\":\"" + (found ? V3(hit.point) : "") +
                "\",\"groundNormal\":\"" + (found ? V3(hit.normal) : "") +
                "\",\"groundDistance\":" + (found ? F(hit.distance) : "-1") +
                ",\"lastContact\":\"" + Escape(lastContact) + "\"}";
        }

        private string BuildIncidentDetails(string reason)
        {
            StringBuilder b = new StringBuilder(8192);
            b.AppendLine("reason=" + reason);
            b.AppendLine("scene=" + Escape(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
            b.AppendLine("device=" + Escape(SystemInfo.deviceModel));
            b.AppendLine("utc=" + DateTime.UtcNow.ToString("o"));
            b.AppendLine("lastGroundY=" + (float.IsNaN(lastGroundY) ? "unknown" : F(lastGroundY)));
            b.AppendLine("secondsSinceGrounded=" + F(Time.unscaledTime - lastGroundedTime));
            b.AppendLine("recentSamplesNewestLast:");
            int start = (sampleHead - sampleCount + BufferCapacity) % BufferCapacity;
            for (int i = 0; i < sampleCount; i++)
                b.AppendLine(samples[(start + i) % BufferCapacity]);
            b.AppendLine("nearbyColliders:");
            Collider[] nearby = Physics.OverlapSphere(transform.position, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < nearby.Length && i < 40; i++)
            {
                Collider c = nearby[i];
                if (c == null) continue;
                b.AppendLine("collider=" + GetPath(c.transform) +
                    "|type=" + c.GetType().Name +
                    "|enabled=" + c.enabled +
                    "|trigger=" + c.isTrigger +
                    "|layer=" + c.gameObject.layer +
                    "|boundsMin=" + V3(c.bounds.min) +
                    "|boundsMax=" + V3(c.bounds.max));
            }
            return b.ToString();
        }

        private void AppendIncident(string details)
        {
            try
            {
                File.AppendAllText(Path.Combine(evidenceDirectory, "ground-incidents.log"),
                    "\n===== GROUND INCIDENT =====\n" + details + "\n");
            }
            catch (Exception e) { Debug.LogWarning("[GROUND_DIAG] Could not save incident: " + e.Message); }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit == null || hit.collider == null) return;
            lastContact = hit.collider.name + "@" + GetPath(hit.collider.transform) +
                " point=" + V3(hit.point) + " normal=" + V3(hit.normal) +
                " move=" + V3(hit.moveDirection) + " length=" + F(hit.moveLength);
            if (hit.normal.y > 0.55f) lastGroundedTime = Time.unscaledTime;
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "null";
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }
        private static string V3(Vector3 v) => F(v.x) + "," + F(v.y) + "," + F(v.z);
        private static string F(float v) => v.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
        private static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
    }
}