using UnityEngine;

public class cubeMov : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        transform.position += new Vector3(Input.GetAxis("Horizontal") * speed * Time.deltaTime, 0.0f, Input.GetAxis("Vertical") * speed * Time.deltaTime);
    }
}
