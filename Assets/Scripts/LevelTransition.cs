using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    [Header("Next Level Settings")]
    [Tooltip("The exact name of the scene you want to load next (e.g. 'Level2')")]
    public string nextSceneName = "Level2";

    [Tooltip("Check this if you want to load the next level by its Build Index number instead of its name.")]
    public bool useBuildIndex = false;
    public int nextSceneIndex = 1;

    // This function is called when something enters the trigger zone
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is the Player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player reached the end of the level! Loading next scene...");
            
            if (useBuildIndex)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}
