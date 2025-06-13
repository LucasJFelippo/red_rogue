using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : MonoBehaviour{

    [Header("References")]
    public Rigidbody rb;
    public Transform head;
    public Camera camera;

    private float _speed = 3.5f;
    private float _horizontalInput;
    private float _verticalInput;

    private void Start(){
        
    }

    void Update(){

        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(_horizontalInput, 0, _verticalInput);

        Debug.Log(direction * _speed * Time.deltaTime);

        transform.Translate(direction * _speed * Time.deltaTime);

    }
}