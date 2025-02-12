using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shell : MonoBehaviour
{
    public GameObject Explode1;
    private Rigidbody rb;
    public float shellSpeed = 10f;
    public PhysicMaterial bounceMaterial;
    public PhysicMaterial nonBounceMaterial;
    private Collider objectCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        objectCollider = GetComponent<Collider>();
        
        // Start with the bounce material to ensure it bounces correctly on impact
        objectCollider.material = bounceMaterial;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy") || collision.collider.CompareTag("Player") || collision.collider.CompareTag("Bullet"))
        {
            Destroy(gameObject); // Destroy the shell on impact with an enemy
            Destroy(collision.collider.gameObject, 1f); // Destroy the enemy after 1 second
            GameObject explosion = Instantiate(Explode1, collision.collider.transform.position, Quaternion.identity); // Create explosion effect
            Destroy(explosion, 2f); // Destroy the explosion effect after 2 seconds
        }
        else if (collision.collider.CompareTag("Obs"))
        {
            Destroy(gameObject); 
            Destroy(collision.collider.gameObject);
        }
        else if (collision.collider.CompareTag("Bounce"))
        {
            // Already using bounce material, so just continue bouncing
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Bounce"))
        {
            // Reset to the bounce material to ensure it’s ready for the next bounce
            objectCollider.material = bounceMaterial;
        }
    }
}
