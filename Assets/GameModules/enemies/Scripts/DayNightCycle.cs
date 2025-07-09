using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Day Settings")]
    [Tooltip("Duration of a complete day-night cycle in seconds.")]
    [SerializeField] private float dayDurationInSeconds = 120f;

    private float rotationSpeed;

    void Start()
    {
        rotationSpeed = 360f / dayDurationInSeconds;
    }

    void Update()
    {
        float rotationThisFrame = rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.right, rotationThisFrame);
    }
}
