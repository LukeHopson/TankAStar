using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hazard : MonoBehaviour
{
    public GameObject Explode1;
    private Collider objectCollider;

    void Start()
    {
        Physics.IgnoreLayerCollision(14, 12, true);
        objectCollider = GetComponent<Collider>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Destroy(collision.collider.gameObject, 1f);
            GameObject explosion = Instantiate(Explode1, collision.collider.transform.position, Quaternion.identity); // Create explosion effect
            Destroy(explosion, 2f); // Destroy the explosion effect after 2 seconds

        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Destroy(other.gameObject);
        }
    }
}
