using UnityEngine;
using UnityEngine.SceneManagement; // Needed to restart the game
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHits = 2;
    private int currentHits = 0;
    private bool isInvincible = false;
    public float invincibilityDuration = 1.5f; // Seconds of immunity after a hit

    // Checkpoint system
    private Vector3 currentCheckpointPosition;

    private void Start()
    {
        // Set the initial checkpoint to where the player starts
        currentCheckpointPosition = transform.position;
        CoinThief.hasReachedCheckpoint1 = false;
    }

    public void UpdateCheckpoint(Vector3 newCheckpointPos)
    {
        currentCheckpointPosition = newCheckpointPos;
        Debug.Log("Checkpoint Saved!");
    }

    // This function is called automatically when the player collides with a trigger
    private void OnTriggerEnter(Collider other)
    {
        // Instant death if touching the water
        if (other.gameObject.CompareTag("Water") || other.gameObject.name.ToLower().Contains("water") || other.gameObject.name.Contains("DeathFloor"))
        {
            DieInstantly();
            return;
        }

        // Make sure your hurdles have the Tag "Hurdle" in the top right of the Inspector!
        if (other.gameObject.CompareTag("Hurdle"))
        {
            TakeHit();
        }
    }

    // If your hurdles have solid colliders instead of triggers, this will run instead
    private void OnCollisionEnter(Collision collision)
    {
        // Instant death if touching the water
        if (collision.gameObject.CompareTag("Water") || collision.gameObject.name.ToLower().Contains("water") || collision.gameObject.name.Contains("DeathFloor"))
        {
            DieInstantly();
            return;
        }

        if (collision.gameObject.CompareTag("Hurdle"))
        {
            TakeHit();
        }
    }

    public void DieInstantly()
    {
        currentHits = maxHits;
        GameOver();
    }

    private void TakeHit()
    {
        // Ignore hits if we are currently invincible
        if (isInvincible) return;

        currentHits++;
        Debug.Log("Ouch! Hit a hurdle. Total hits: " + currentHits);

        if (currentHits >= maxHits)
        {
            GameOver();
        }
        else
        {
            // First hit! Give the player temporary invincibility so they don't immediately die to the same hurdle
            StartCoroutine(InvincibilityFrames());
        }
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        // Optional: You could make the player flash red here or play a stumble animation
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    [Header("UI Settings")]
    public GameObject gameOverPanel; // Drag your UI panel here in the Inspector

    private void GameOver()
    {
        Debug.Log("GAME OVER! Showing Retry Screen...");
        
        // Pause the game so everything stops moving
        Time.timeScale = 0f;

        // Show the Game Over UI screen if you assigned one
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            // Unlock the mouse cursor so you can click the Retry button
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // If you forgot to assign a panel, just retry automatically
            RetryFromCheckpoint();
        }
    }

    // You will link this function to your UI "Retry" Button!
    public void RetryFromCheckpoint()
    {
        // Unpause the game
        Time.timeScale = 1f;

        // Hide the Game Over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Reset health
        currentHits = 0;
        
        // Reset coins to 0 in the UI
        if (CollectableCounter.instance != null)
        {
            CollectableCounter.instance.currentCount = 0;
        }

        // Bring back all the physical coins in the world!
        foreach (FloatingCoin coin in FloatingCoin.allCoins)
        {
            if (coin != null)
            {
                coin.gameObject.SetActive(true);
            }
        }
        
        // Reset all thieves to their starting positions
        foreach (CoinThief thief in CoinThief.allThieves)
        {
            if (thief != null)
            {
                thief.ResetToStart();
            }
        }
        
        // Move player to the checkpoint
        // If you are using a CharacterController, you have to turn it off before teleporting
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        
        transform.position = currentCheckpointPosition;
        
        if (cc != null) cc.enabled = true;
        
        // Give some invincibility upon respawn
        StartCoroutine(InvincibilityFrames());
        
        // Re-lock the mouse cursor for the SUPERCharacter controller if not on mobile/touch simulation
        bool keepUnlocked = DeviceSettings.IsMobile;
        Cursor.lockState = keepUnlocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = keepUnlocked;
    }
}
