using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSController : MonoBehaviour
{
    public float speed = 5.0f;
    public float sensitivity = 2.0f;
    public float jumpHeight = 2.0f;

    private float rotationX = 0.0f;  
    private CharacterController characterController;
    private Vector3 moveDirection;
    private bool isGrounded;

    public string Role { get; private set; } = "RABBIT";

    public Camera playerCamera;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = characterController.isGrounded;

        MovePlayer();

        RotateCamera();
    }

    private void MovePlayer()
    {
        float moveDirectionY = moveDirection.y;

        float moveDirectionX = 0f;
        float moveDirectionZ = 0f;

        if (Input.GetKey("w"))  
            moveDirectionZ = 1f;
        if (Input.GetKey("s"))  
            moveDirectionZ = -1f;
        if (Input.GetKey("d")) 
            moveDirectionX = 1f;
        if (Input.GetKey("a"))
            moveDirectionX = -1f;

        Vector3 forward = transform.TransformDirection(Vector3.forward) * moveDirectionZ;
        Vector3 right = transform.TransformDirection(Vector3.right) * moveDirectionX;

        moveDirection = forward + right;
        moveDirection.y = moveDirectionY;


        characterController.Move(moveDirection * speed * Time.deltaTime);
    }

    private void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        transform.Rotate(0, mouseX, 0);

        rotationX -= Input.GetAxis("Mouse Y") * sensitivity;
        rotationX = Mathf.Clamp(rotationX, -80f, 80f);  
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }
    public void SetRoleFox()
    {
        Role = "FOX";
        speed = 6.0f; 
        gameObject.tag = "Fox";
    }

    public void SetRoleRabbit()
    {
        Role = "RABBIT";
        speed = 4.0f;
        gameObject.tag = "Rabbit";
    }
}
