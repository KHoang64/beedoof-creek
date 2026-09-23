using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BeaverCreek
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CreekFirstPerson : MonoBehaviour
    {
        public Transform view;
        public float walkSpeed = 3.1f;
        public float sensitivity = .085f;
        public float initialPitch = 6f;
        public bool UseTouchControls { get; set; }
        public bool InputEnabled { get; set; } = true;
        public bool SprintHeld { get; set; }
        public Vector2 TouchMove { get; set; }
        public Vector2 TouchLook { get; set; }
        CharacterController controller;
        Vector3 spawn;
        Quaternion spawnRotation;
        float pitch, fallSpeed;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawn = transform.position;
            spawnRotation = transform.rotation;
            pitch = initialPitch;
        }
        void Start() { if (!UseTouchControls) ResumeLook(); }
        public void ResumeLook() { SetCursor(!UseTouchControls); }
        public void ReleaseLook() { SetCursor(false); ClearTouch(); }
        public void ClearTouch() { TouchMove = TouchLook = Vector2.zero; SprintHeld = false; }
        public void ResetPosition()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawn, spawnRotation);
            controller.enabled = true;
            pitch = initialPitch;
            view.localRotation = Quaternion.Euler(pitch, 0, 0);
            fallSpeed = 0;
            ClearTouch();
        }
        void Update()
        {
            if (!InputEnabled) { ClearTouch(); return; }
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (!UseTouchControls && mouse != null && mouse.leftButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())) ResumeLook();
            bool canMove = UseTouchControls || Cursor.lockState == CursorLockMode.Locked;
            Vector2 delta = UseTouchControls ? TouchLook * .15f :
                canMove && mouse != null ? mouse.delta.ReadValue() * sensitivity : Vector2.zero;
            TouchLook = Vector2.zero;
            transform.Rotate(0, delta.x, 0);
            pitch = Mathf.Clamp(pitch - delta.y, -78, 78);
            view.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector2 input = UseTouchControls ? TouchMove : Vector2.zero;
            if (canMove && keyboard != null)
                input += new Vector2((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                    (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) -
                    (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0));
            input = Vector2.ClampMagnitude(input, 1);
            if (controller.isGrounded) fallSpeed = -2;
            fallSpeed += Physics.gravity.y * Time.deltaTime;
            bool sprint = SprintHeld || (keyboard != null && keyboard.leftShiftKey.isPressed);
            controller.Move(((transform.right * input.x + transform.forward * input.y) * (sprint ? walkSpeed * 1.65f : walkSpeed)
                + Vector3.up * fallSpeed) * Time.deltaTime);
            if (transform.position.y < -8 || (keyboard != null && keyboard.rKey.wasPressedThisFrame)) ResetPosition();
        }
        void SetCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnApplicationFocus(bool focus) { if (!focus) ReleaseLook(); }
        void OnDisable() { ReleaseLook(); }
    }
}
