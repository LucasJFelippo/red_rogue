using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Target Info")]
    public Transform target;

    public Vector3 offset = new Vector3(-15f, 13f, -15f);
    public float smoothSpeed = 5f;

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