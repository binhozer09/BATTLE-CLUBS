using UnityEngine;

namespace BattleClubs.Player
{
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.4f, -4.2f);
        [SerializeField] private float followSharpness = 12f;
        [SerializeField] private float lookHeight = 1.4f;
        [SerializeField] private float sensitivity = 2.2f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 65f;
        [Header("Attack camera movement")]
        [SerializeField] private float attackOrbitDegrees = 90f;
        [SerializeField] private float attackOrbitSpeed = 300f;

        private float yaw;
        private float pitch = 12f;
        private float attackYawOffset;
        private float attackYawTarget;

        private void LateUpdate()
        {
            if (target == null) return;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            yaw += Input.GetAxis("Mouse X") * sensitivity * 3f;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity * 3f;
#endif
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            attackYawOffset = Mathf.MoveTowardsAngle(
                attackYawOffset, attackYawTarget, attackOrbitSpeed * Time.deltaTime);

            Quaternion rotation = Quaternion.Euler(pitch, yaw + attackYawOffset, 0f);
            Vector3 desired = target.position + rotation * offset;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(target.position + Vector3.up * lookHeight - transform.position, Vector3.up);
        }

        public void SetTarget(Transform value)
        {
            target = value;
            if (target != null) yaw = target.eulerAngles.y;
        }

        // Hook a swipe/drag look control to this on Android.
        public void AddLookInput(Vector2 delta)
        {
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
        }

        // Each attack smoothly turns the camera around the player by the configured angle.
        public void AddAttackOrbit()
        {
            attackYawTarget += attackOrbitDegrees;
        }
    }
}