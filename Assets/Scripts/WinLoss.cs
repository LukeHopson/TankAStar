using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinLoss : MonoBehaviour
{
    public string nextScene; // This should be set in each scene because the next level should be different
    public string titleScene = "Main Menu";
    // Update is called once per frame
    void Update()
    {
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            // No enemies left, load the next level
            SceneManager.LoadScene(nextScene);
        }

        // Check if there is players left. 
        if (GameObject.FindGameObjectsWithTag("Player").Length == 0)
        {
            // Here means the player died, go back to title
            SceneManager.LoadScene(titleScene);
        }
    }
}
