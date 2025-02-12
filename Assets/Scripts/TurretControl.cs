using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretController : MonoBehaviour
{
    public Camera mainCamera;  
    public float rotationSpeed = 5f;  // Speed for the turret to rotate
    public GameObject shellPrefab;
    public Transform firePoint; // Where we shoot from
    public float shellSpeed = 10f; // Speed that the bullet travels

    void Update()
    {
        RotateTowardsMouse();
        if (Input.GetMouseButtonDown(0)){ // Fire when left click
            FireTurret();
        }
    }

    void RotateTowardsMouse()
    {
        Vector3 mousePosition = Input.mousePosition;

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);
        
        if (plane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);

            Vector3 direction = targetPoint - transform.position;
            direction.y = 0;  

            // -90 offset for x and y, otherwise the turret points at the wrong spot by 90 degrees and flips on it's side
            Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(-90, -90, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }


    }

    //https://www.youtube.com/watch?v=LqrAbEaDQzc but 3d and altered
    void FireTurret(){
        GameObject shell = Instantiate(shellPrefab, firePoint.position, Quaternion.Euler(0, 0, 90)); // Want the shell to be rotated 90 degrees on Z axis based on how I made the prefab
        shell.GetComponent<Rigidbody>().AddForce(firePoint.right * shellSpeed, ForceMode.Impulse); // firePoint goes out of turret along x axis, so use firePoint.right
    }
}
