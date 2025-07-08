using UnityEngine;

public class SimpleMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float moveZ = Input.GetAxisRaw("Vertical");   // W/S or Up/Down

        Vector3 movement = new Vector3(moveX, 0f, moveZ).normalized;

        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
    }
}