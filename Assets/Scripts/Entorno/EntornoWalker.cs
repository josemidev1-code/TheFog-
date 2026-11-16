using TheFog.Puesto;
using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// Recorrer el entorno en primera persona mientras se construye: mismo paso, mirada y balanceo que Daniel,
    /// sin depender del sistema de partida (reloj, radio, HUD). Esc suelta el ratón; clic lo vuelve a capturar.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EntornoWalker : MonoBehaviour
    {
        public Transform head;
        public Camera cam;
        public float walkSpeed = 1.7f, sprintSpeed = 4.5f, gravity = 18f;
        public float bobAmount = 0.035f, bobFrequency = 1.9f;

        CharacterController cc;
        float yaw, pitch, verticalVelocity, bobPhase;
        Vector3 camRest;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (head == null) head = transform.GetChild(0);
            if (cam == null) cam = GetComponentInChildren<Camera>();
            yaw = transform.eulerAngles.y;
            pitch = head.localEulerAngles.x > 180 ? head.localEulerAngles.x - 360 : head.localEulerAngles.x;
            camRest = cam.transform.localPosition;
        }

        void OnEnable() => Lock(true);
        void OnDisable() => Lock(false);

        static void Lock(bool on)
        {
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }

        void Update()
        {
            if (PInput.Pause) Lock(false);
            if (Cursor.lockState != CursorLockMode.Locked && UnityEngine.InputSystem.Mouse.current != null
                && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) Lock(true);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = PInput.Look();
                yaw += look.x; pitch = Mathf.Clamp(pitch - look.y, -82f, 82f);
                transform.rotation = Quaternion.Euler(0, yaw, 0);
                head.localRotation = Quaternion.Euler(pitch, 0, 0);
            }

            Vector2 input = PInput.Move();
            float speed = PInput.Sprint ? sprintSpeed : walkSpeed;
            Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;
            verticalVelocity = cc.isGrounded ? -1f : verticalVelocity - gravity * Time.deltaTime;
            Vector3 before = transform.position;
            cc.Move((move + Vector3.up * verticalVelocity) * Time.deltaTime);

            float moved = Vector3.ProjectOnPlane(transform.position - before, Vector3.up).magnitude;
            if (moved > 0.0005f) bobPhase += moved * bobFrequency * Mathf.PI;
            float amount = Mathf.Clamp01(move.magnitude / walkSpeed);
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition,
                camRest + new Vector3(Mathf.Cos(bobPhase * 0.5f) * bobAmount * 0.5f, Mathf.Abs(Mathf.Sin(bobPhase * 0.5f)) * bobAmount, 0) * amount,
                Time.deltaTime * 10f);
        }

        public void Teleport(Vector3 position)
        {
            cc.enabled = false; transform.position = position; cc.enabled = true;
        }

        public void LookAtPoint(Vector3 point)
        {
            Vector3 d = point - head.position;
            yaw = Quaternion.LookRotation(Vector3.ProjectOnPlane(d, Vector3.up)).eulerAngles.y;
            pitch = -Mathf.Asin(d.normalized.y) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, yaw, 0); head.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
    }
}
