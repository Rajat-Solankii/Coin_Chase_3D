using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance { get; private set; }

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject victoryPanel;

    [Header("Victory Texts")]
    public Text rewardText;
    public Text totalScoreText;

    private void Awake()
    {
        instance = this;

        // Ensure MazeCheatManager exists in the scene
        if (FindObjectOfType<MazeCheatManager>() == null)
        {
            GameObject cheatObj = new GameObject("[MazeCheatManager]");
            cheatObj.AddComponent<MazeCheatManager>();
        }

        // Ensure MobileInputManager exists in the scene
        if (FindObjectOfType<MobileInputManager>() == null)
        {
            GameObject mobObj = new GameObject("[MobileInputManager]");
            mobObj.AddComponent<MobileInputManager>();
        }
    }

    private void Start()
    {
        SetupGameOverMainMenuButton();
    }

    private void Update()
    {
        if (IsPauseKeyPressed())
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (pausePanel != null && !IsGameOver() && !IsVictory())
        {
            bool isPaused = !pausePanel.activeSelf;
            pausePanel.SetActive(isPaused);
            Time.timeScale = isPaused ? 0f : 1f;

            bool keepUnlocked = DeviceSettings.IsMobile;
            Cursor.lockState = (isPaused || keepUnlocked) ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPaused || keepUnlocked;
        }
    }

    private bool IsPauseKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
#endif
        try
        {
            if (Input.GetKeyDown(KeyCode.Escape)) return true;
        }
        catch { }
        return false;
    }
    
    private bool IsGameOver()
    {
        var ph = FindObjectOfType<PlayerHealth>();
        return ph != null && ph.gameOverPanel != null && ph.gameOverPanel.activeSelf;
    }

    private bool IsVictory()
    {
        return victoryPanel != null && victoryPanel.activeSelf;
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;

        bool keepUnlocked = DeviceSettings.IsMobile;
        Cursor.lockState = keepUnlocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = keepUnlocked;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        CoinThief.hasReachedCheckpoint1 = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        CoinThief.hasReachedCheckpoint1 = false;
        SceneManager.LoadScene("SampleScene");
    }

    public void ToggleMazeCheat()
    {
        if (MazeCheatManager.instance != null)
        {
            MazeCheatManager.instance.ToggleCheat();
        }
    }

    public void UpdateCheatButtonUI(bool isCheatActive)
    {
        // Kept for backward compatibility if called
    }

    /// <summary>
    /// Ensures GameOverPanel has a Main Menu button in addition to Retry.
    /// </summary>
    private void SetupGameOverMainMenuButton()
    {
        var ph = FindObjectOfType<PlayerHealth>();
        if (ph == null || ph.gameOverPanel == null) return;

        // Check if Main Menu button already exists in GameOverPanel
        Button[] buttons = ph.gameOverPanel.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            if (b.gameObject.name.Contains("Menu") || b.gameObject.name.Contains("Main")) return;
        }

        // Clone RetryButton if present
        Button retryBtn = null;
        foreach (var b in buttons)
        {
            if (b.gameObject.name.Contains("Retry")) { retryBtn = b; break; }
        }

        if (retryBtn != null)
        {
            GameObject menuBtnGO = Instantiate(retryBtn.gameObject, ph.gameOverPanel.transform);
            menuBtnGO.name = "GameOverMainMenuButton";

            RectTransform retryRT = retryBtn.GetComponent<RectTransform>();
            RectTransform menuRT = menuBtnGO.GetComponent<RectTransform>();

            // Reposition both buttons for clean layout
            if (retryRT != null && menuRT != null)
            {
                retryRT.anchoredPosition = new Vector2(0f, -25f);
                menuRT.anchoredPosition = new Vector2(0f, -100f);
            }

            Text btnText = menuBtnGO.GetComponentInChildren<Text>();
            if (btnText != null) btnText.text = "MAIN MENU";

            Button menuBtn = menuBtnGO.GetComponent<Button>();
            menuBtn.onClick.RemoveAllListeners();
            menuBtn.onClick.AddListener(LoadMainMenu);

            Image img = menuBtnGO.GetComponent<Image>();
            if (img != null) img.color = new Color(0.3f, 0.3f, 0.35f, 0.95f);
        }
    }

    public void ShowVictoryScreen(int rewardCoins)
    {
        int totalCoins = 0;
        if (CollectableCounter.instance != null)
        {
            totalCoins = CollectableCounter.instance.currentCount;
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            
            if (rewardText != null)
            {
                rewardText.text = "REWARD: +" + rewardCoins + " GOLD COINS!";
            }
            if (totalScoreText != null)
            {
                totalScoreText.text = "FINAL SCORE: " + totalCoins + " COINS";
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.Log("Victory! Reward: " + rewardCoins + " Total: " + totalCoins);
        }
    }
}
