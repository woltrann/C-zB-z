using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Distance")]
    [SerializeField] private float minDistance = 2.5f;
    [SerializeField] private float maxDistance = 4f;
    [SerializeField] private float height = 1.6f;

    [Header("Mouse")]
    [SerializeField] private float sensitivity = 250f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 75f;

    [Header("Smooth")]
    [SerializeField] private float followSpeed = 15f;
    [SerializeField] private float rotateSpeed = 15f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        // Kameranýn baþlangýç yönünü kendi mevcut rotasyonundan al.
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;

        // 270 -> -90 gibi deðerleri düzelt
        if (pitch > 180f)
            pitch -= 360f;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void LateUpdate()
    {
        if (InventoryUI.IsInputBlocked)
            return;
        if (target == null)
            return;

        // -------------------------
        // MOUSE CAMERA ROTATION
        // -------------------------

        float mouseX =
            Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );


        // -------------------------
        // CAMERA DISTANCE
        // -------------------------

        float t = Mathf.InverseLerp(
            maxPitch,
            minPitch,
            pitch
        );

        float currentDistance =
            Mathf.Lerp(
                maxDistance,
                minDistance,
                t
            );


        // -------------------------
        // CAMERA POSITION
        // -------------------------

        Vector3 targetPosition =
            target.position +
            Vector3.up * height;

        Vector3 desiredPosition =
            targetPosition -
            rotation * Vector3.forward *
            currentDistance;


        // -------------------------
        // FOLLOW
        // -------------------------

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed * Time.deltaTime
            );


        // -------------------------
        // ROTATION
        // -------------------------

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                rotation,
                rotateSpeed * Time.deltaTime
            );
    }

    public float Yaw => yaw;
}