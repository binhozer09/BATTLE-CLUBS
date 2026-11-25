using UnityEngine;

namespace BattleClubs.Player
{
    /// <summary>
    /// Keeps the CharacterController root above real map colliders and recovers
    /// the player if a collision is missed and the character falls below the map.
    /// </summary>
    [DefaultExecutionOrder(20000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerGroundSafety : MonoBehaviour
    {
        [SerializeField] private CharacterController controller;
        [SerializeField, Min(1f)] private float rayStartHeight = 3f;
        [SerializeField, Min(5f)] private float rayDistance = 40f;
        [SerializeField, Min(0f)] private float groundClearance = 0.035f;
        [SerializeField, Min(0.5f)] private float recoveryDepth = 2f;
        [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;

        private readonly RaycastHit[] hits = new RaycastHit[64];
        private Vector3 lastSafePosition;
        private bool hasSafePosition;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            lastSafePosition = transform.position;
            hasSafePosition = true;
        }

        private void LateUpdate()
        {
            if (controller == null || !controller.enabled) return;

            if (TryFindSurface(out float surfaceY))
            {
                float capsuleBottom = transform.position.y + controller.center.y - controller.height * 0.5f;
                float penetration = surfaceY + groundClearance - capsuleBottom;

                // Only correct penetration. Do not pull a jumping/falling player down.
                if (penetration > 0.02f && penetration < 8f)
                {
                    BattleAIDiagnostic.Report("PLAYER_GROUND_PENETRATION",
                        "Cápsula do jogador estava abaixo da superfície detectada.", transform.position);
                    controller.enabled = false;
                    transform.position += Vector3.up * penetration;
                    controller.enabled = true;
                }

                if (controller.isGrounded || Mathf.Abs(capsuleBottom - surfaceY) < 0.12f)
                {
                    lastSafePosition = transform.position;
                    hasSafePosition = true;
                }
            }

            if (transform.position.y < -recoveryDepth)
            {
                BattleAIDiagnostic.Report("PLAYER_FELL_BELOW_MAP",
                    "Player caiu abaixo do limite de recuperação.", transform.position);
                RecoverPlayer();
            }
        }

        private bool TryFindSurface(out float surfaceY)
        {
            surfaceY = 0f;
            Vector3 origin = transform.position + Vector3.up * rayStartHeight;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits, rayDistance,
                groundLayers, QueryTriggerInteraction.Ignore);

            float nearest = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i].collider;
                if (hit == null) continue;
                Transform hitTransform = hit.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;

                // Ignore walls, ceilings and steep props: only a walkable
                // surface close to the capsule feet can correct the player root.
                Vector3 normal = hits[i].normal;
                float minUp = Mathf.Cos(Mathf.Clamp(controller.slopeLimit + 2f, 0f, 89f) * Mathf.Deg2Rad);
                if (normal.y < minUp) continue;

                float capsuleBottom = transform.position.y + controller.center.y - controller.height * 0.5f;
                float hitY = hits[i].point.y;
                if (hitY > capsuleBottom + 0.35f) continue;
                if (capsuleBottom - hitY > 8f) continue;

                float distance = hits[i].distance;
                if (distance < nearest)
                {
                    nearest = distance;
                    surfaceY = hitY;
                    found = true;
                }
            }
            return found;
        }

        private void RecoverPlayer()
        {
            controller.enabled = false;
            Vector3 recovery = hasSafePosition ? lastSafePosition : new Vector3(4f, 1f, 4f);
            recovery.y = Mathf.Max(recovery.y, 1f);
            transform.position = recovery;
            controller.enabled = true;
        }
    }
}