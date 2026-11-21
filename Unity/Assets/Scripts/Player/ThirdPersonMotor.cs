using UnityEngine;

namespace BattleClubs.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float jumpHeight = 1.15f;
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private Vector2 mobileMove;
        private bool mobileSprint;
        private bool jumpQueued;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            Vector2 input = mobileMove;
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            if (input.sqrMagnitude < 0.001f)
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            bool sprint = mobileSprint || Input.GetKey(KeyCode.LeftShift);
            bool jump = jumpQueued || Input.GetButtonDown("Jump");
#else
            bool sprint = mobileSprint;
            bool jump = jumpQueued;
#endif
            jumpQueued = false;

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 move = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
            }

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (jump && controller.isGrounded)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalVelocity += gravity * Time.deltaTime;
            float speed = sprint ? sprintSpeed : moveSpeed;
            Vector3 velocity = move * speed;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        // Connect the Android virtual joystick to this method.
        public void SetMoveInput(Vector2 value) => mobileMove = Vector2.ClampMagnitude(value, 1f);
        public void SetSprint(bool value) => mobileSprint = value;
        public void QueueJump() => jumpQueued = true;
    }
}
