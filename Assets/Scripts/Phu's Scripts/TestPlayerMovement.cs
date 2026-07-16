using System;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

public class TestPlayerMovement : MonoBehaviour
{
    public Transform playerBody;
    public Transform cameraBody;
    public float mouseSensitivity = 800f;
    public float moveSpeed = 6f;
    public float gravity = -9.81f;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    CharacterController controller;
    float xRot = 0f;
    Vector3 vel;
    bool isGrounded;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        controller = playerBody.gameObject.GetComponent<CharacterController>();

        Debug.Log("SPAWNED");
    }

    void Update()
    {
        // float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        // float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // xRot -= mouseY;
        // xRot = Mathf.Clamp(xRot, -90f, 90f);

        // cameraBody.localRotation = Quaternion.Euler(xRot, 0f, 0f);
        // playerBody.Rotate(Vector3.up * mouseX);

        //---------------------------------------

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && vel.y < 0)
        {
            vel.y = 0f;
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 moveDir = playerBody.right * moveX + playerBody.forward * moveZ;
        controller.Move(moveDir * moveSpeed * Time.deltaTime);

        vel.y += gravity * Time.deltaTime;
        controller.Move(vel * Time.deltaTime);
    }
}