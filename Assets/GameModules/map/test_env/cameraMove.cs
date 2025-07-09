using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Target Info")]
    public Transform target;

    public Vector3 offset = new Vector3(-15f, 13f, -15f);
    public float smoothSpeed = 5f;

    public float startSize = 10f;
    public float targetSize = 2.5f;
    public float duration = 5f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        cam.orthographicSize = startSize;

        LeanTween.value(gameObject, startSize, targetSize, duration)
            .setOnUpdate((float val) => cam.orthographicSize = val)
            .setEaseOutCubic();
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            transform.position = smoothedPosition;
        }
    }
}