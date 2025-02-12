using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedTank : MonoBehaviour
{
    public Transform player;
    public GameObject heatseekerPrefab; // Heatseeker missile prefab
    public Transform firePoint;
    public float fireCooldown = 1.5f; // Time between shots
    public float shellSpeed = 20f; // Speed of the shell (if applicable)
    
    private float lastFireTime; // Timer to handle cooldown
    private State currentState = State.Idle;

    private enum State
    {
        Idle,
        Fire
    }

    void Update()
    {
        if (player == null)
        {
            currentState = State.Idle;
            return;
        }
        else
        {
            currentState = State.Fire;
        }

        switch (currentState)
        {
            case State.Fire:
                Fire();
                break;
            case State.Idle:
                break;
        }
    }

    private void Fire()
    {
        if (Time.time > lastFireTime + fireCooldown)
        {
            Vector3 adjustedFirePosition = firePoint.position + new Vector3(0, 1f, 0); 
            GameObject heatseeker = Instantiate(heatseekerPrefab, firePoint.position, firePoint.rotation);
            heatseeker.tag = "Bullet";

            Heatseeker heatseekerScript = heatseeker.GetComponent<Heatseeker>();
            if (heatseekerScript != null)
            {
                heatseekerScript.player = player;
            }
            lastFireTime = Time.time;
        }
    }
}
