using UnityEngine;

namespace BattleClubs.Player
{
    /// <summary>
    /// Keeps the rendered humanoid feet aligned with the owning CharacterController.
    /// Runs after Animator evaluation so imported avatar pivots and animated bounds
    /// cannot leave the visible body buried below the walkable floor.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class HumanoidGroundAlignment : MonoBehaviour
    {
        [SerializeField] private CharacterController owner;
        [SerializeField, Min(0f)] private float soleClearance = 0.015f;
        [SerializeField, Min(0f)] private float maxCorrectionPerFrame = 0.35f;

        private Renderer[] bodyRenderers;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<CharacterController>();
            bodyRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void LateUpdate()
        {
            if (owner == null || bodyRenderers == null || bodyRenderers.Length == 0)
                return;

            bool hasBounds = false;
            Bounds bounds = default;
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                Renderer renderer = bodyRenderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!hasBounds) return;

            float floorY = owner.bounds.min.y;
            float correction = floorY + soleClearance - bounds.min.y;
            if (float.IsNaN(correction) || float.IsInfinity(correction)) return;
            correction = Mathf.Clamp(correction, -maxCorrectionPerFrame, maxCorrectionPerFrame);
            if (Mathf.Abs(correction) > 0.0005f)
                transform.position += Vector3.up * correction;
        }
    }
}