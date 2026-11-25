using UnityEngine;

namespace BattleClubs.Player
{
    /// <summary>
    /// Keeps the CharacterController above walkable map colliders and restores
    /// the last confirmed grounded position if collision is ever missed.
    /// </summary>
    [DefaultExecutionOrder(20000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerGroundSafety : MonoBehaviour
    {
        [SerializeField] private CharacterController controller;
        [SerializeField, Min(0.1f)] private float probeStartHeight = 2.5f;
        [SerializeField, Min(1f)] private float probeDistance = 12f;
        [SerializeField, Range(0.05f, 0.95f)] private float probeRadiusFactor = 0.75f;
        [SerializeField, Min(0f)] private float groundClearance = 0.04f;
        [SerializeField, Min(0.5f)] private float recoveryDepth = 2f;
        [SerializeField, Min(0.05f)] private float groundedTolerance = 0.25f;
        [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;

        private readonly RaycastHit[] groundHits = new RaycastHit[96];
        private Vector3 lastSafePosition;
        private bool hasSafePosition;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            RememberSafePosition();
        }

        private void LateUpdate()
        {
            if (controller == null || !controller.enabled || !gameObject.activeInHierarchy)
                return;

            if (TryFindGround(out RaycastHit groundHit))
            {
                float capsuleBottom = GetCapsuleBottom();
                float penetration = groundHit.point.y + groundClearance - capsuleBottom;

                // Correct only a small real penetration. Large gaps are handled
                // by the fall recovery instead of teleporting the player upward.
                if (penetration > 0.015f && penetration <= controller.height * 0.5f)
                {
                    BattleAIDiagnostic.Report("PLAYER_GROUND_PENETRATION",
                        "CharacterController penetrou a superfície caminhável; correção aplicada.",
                        transform.position);

                    controller.enabled = false;
                    transform.position += Vector3.up * penetration;
                    Physics.SyncTransforms();
                    controller.enabled = true;
                }

                float footGap = Mathf.Abs(GetCapsuleBottom() - groundHit.point.y);
                if (controller.isGrounded || footGap <= groundedTolerance)
                    RememberSafePosition();
            }

            if (transform.position.y < -recoveryDepth)
            {
                BattleAIDiagnostic.Report("PLAYER_FELL_BELOW_MAP",
                    "CharacterController caiu abaixo do mapa; retornando à última posição segura.",
                    transform.position);
                RecoverPlayer();
            }
        }

        private float GetCapsuleBottom()
        {
            return transform.position.y + controller.center.y - controller.height * 0.5f;
        }

        private bool TryFindGround(out RaycastHit bestHit)
        {
            bestHit = default;
            Vector3 origin = transform.position + Vector3.up * probeStartHeight;
            float radius = Mathf.Max(0.05f, controller.radius * probeRadiusFactor);
            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits,
                probeDistance, groundLayers, QueryTriggerInteraction.Ignore);

            float capsuleBottom = GetCapsuleBottom();
            float minUp = Mathf.Cos(Mathf.Clamp(controller.slopeLimit + 2f, 0f, 89f) * Mathf.Deg2Rad);
            float nearestDistance = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = groundHits[i].collider;
                if (hitCollider == null)
                    continue;

                Transform hitTransform = hitCollider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                    continue;

                RaycastHit hit = groundHits[i];
                if (hit.normal.y < minUp)
                    continue;

                // Never use a balcony, roof or other collider above the player's feet
                // as the ground beneath the character.
                if (hit.point.y > capsuleBottom + 0.2f)
                    continue;

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    bestHit = hit;
                    found = true;
                }
            }

            return found;
        }

        private void RememberSafePosition()
        {
            lastSafePosition = transform.position;
            hasSafePosition = true;
        }

        private void RecoverPlayer()
        {
            Vector3 recovery = hasSafePosition ? lastSafePosition : new Vector3(4f, 1f, 4f);
            controller.enabled = false;
            transform.position = recovery;
            Physics.SyncTransforms();
            controller.enabled = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            probeStartHeight = Mathf.Max(0.1f, probeStartHeight);
            probeDistance = Mathf.Max(1f, probeDistance);
            recoveryDepth = Mathf.Max(0.5f, recoveryDepth);
            groundedTolerance = Mathf.Max(0.05f, groundedTolerance);
        }
#endif
    }
}