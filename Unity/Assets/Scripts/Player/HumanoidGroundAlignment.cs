using UnityEngine;

namespace BattleClubs.Player
{
    /// <summary>
    /// Aligns the visible humanoid soles to the actual walkable surface below the
    /// CharacterController. The controller's lower bound is NOT treated as ground:
    /// its capsule can be offset or still falling when the visual is evaluated.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class HumanoidGroundAlignment : MonoBehaviour
    {
        [SerializeField] private CharacterController owner;
        [SerializeField, Min(0f)] private float soleClearance = 0.015f;
        [SerializeField, Min(0.1f)] private float rayStartHeight = 2.5f;
        [SerializeField, Min(0.1f)] private float rayDistance = 20f;
        [SerializeField, Min(0f)] private float maxCorrectionPerFrame = 0.5f;
        [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;

        private Renderer[] bodyRenderers;
        private readonly RaycastHit[] groundHits = new RaycastHit[32];

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<CharacterController>();
            bodyRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void LateUpdate()
        {
            if (owner == null || !owner.enabled || bodyRenderers == null || bodyRenderers.Length == 0)
                return;

            if (!TryGetGroundHeight(out float groundY))
                return;

            if (!TryGetBodyBounds(out Bounds bodyBounds))
                return;

            float correction = groundY + soleClearance - bodyBounds.min.y;
            if (float.IsNaN(correction) || float.IsInfinity(correction))
                return;

            correction = Mathf.Clamp(correction, -maxCorrectionPerFrame, maxCorrectionPerFrame);
            if (Mathf.Abs(correction) > 0.0005f)
                transform.position += Vector3.up * correction;
        }

        private bool TryGetGroundHeight(out float groundY)
        {
            groundY = 0f;

            // Start above the capsule so the ray finds the map/court collider,
            // not the player. This works even if the visual mesh is partly underground.
            Vector3 origin = owner.bounds.center + Vector3.up * rayStartHeight;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits,
                rayDistance, groundLayers, QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = groundHits[i].collider;
                if (hitCollider == null) continue;

                Transform hitTransform = hitCollider.transform;
                if (hitTransform == owner.transform || hitTransform.IsChildOf(owner.transform))
                    continue;

                // Reject walls/ceilings and surfaces above the humanoid's feet.
                float minUp = Mathf.Cos(Mathf.Clamp(owner.slopeLimit + 5f, 0f, 89f) * Mathf.Deg2Rad);
                if (groundHits[i].normal.y < minUp) continue;
                float hitY = groundHits[i].point.y;
                if (hitY > owner.bounds.min.y + 1.25f) continue;

                float distance = groundHits[i].distance;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    groundY = hitY;
                    found = true;
                }
            }
            return found;
        }

        private bool TryGetBodyBounds(out Bounds bounds)
        {
            bounds = default;
            bool found = false;

            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                Renderer renderer = bodyRenderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            soleClearance = Mathf.Max(0f, soleClearance);
            rayStartHeight = Mathf.Max(0.1f, rayStartHeight);
            rayDistance = Mathf.Max(0.1f, rayDistance);
            maxCorrectionPerFrame = Mathf.Max(0f, maxCorrectionPerFrame);
        }
#endif
    }
}