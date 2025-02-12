using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float moveSpeed;
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        moveSpeed = 5f;
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // Forward and backward movement
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.W))
        {
            moveInput = 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveInput = -1f;
        }

        // Since this is a tank, left/right doesnt move but turns the body
        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.A))
        {
            rotationInput = -100f; // Increasing/decreasing these values makes turning faster or slower
        }
        if (Input.GetKey(KeyCode.D))
        {
            rotationInput = 100f;
        }

        // Offset rotation 90 degrees, otherwise the tank moves sideways
        Vector3 forwardOffset = Quaternion.Euler(0, 90, 0) * transform.forward;
        Vector3 moveDirection = forwardOffset * moveInput * moveSpeed;


        rb.velocity = new Vector3(moveDirection.x, rb.velocity.y, moveDirection.z);

        // Rotation of tank chassis, seperate from turret
        float rotationAmount = rotationInput * Time.deltaTime;
        transform.Rotate(0, rotationAmount, 0);
    }
}
