using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    private Text deviceButtonText;
    private Text controlsInfoText;
    private float lastToggleRealtime = -1f;

    private void Awake()
    {
        Application.runInBackground = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        EnsureEventSystem();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        GameSettings.InitializeIfNeeded();
        GameSettings.Apply();

        BindUI();
        UpdateDeviceUI();
    }

    private void EnsureEventSystem()
    {
        var es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            es = esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        var inputModule = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        if (inputModule == null)
        {
            if (standalone != null) Destroy(standalone);
            inputModule = es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
        else if (standalone != null)
        {
            Destroy(standalone);
        }
#endif
    }

    private void BindUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 2.5f;
            }

            Transform menuCard = canvas.transform.Find("MenuCard");
            if (menuCard != null)
            {
                Transform playBtn = menuCard.Find("PlayButton");
                if (playBtn != null)
                {
                    Button btn = playBtn.GetComponent<Button>();
                    BindButton(btn, PlayGame);
                }

                Transform settingsBtn = menuCard.Find("SettingsButton");
                if (settingsBtn != null)
                {
                    Button btn = settingsBtn.GetComponent<Button>();
                    BindButton(btn, OpenSettings);
                }

                Transform deviceBtn = menuCard.Find("DeviceButton");
                if (deviceBtn != null)
                {
                    Button btn = deviceBtn.GetComponent<Button>();
                    BindButton(btn, OnDeviceButtonClicked);
                    deviceButtonText = deviceBtn.GetComponentInChildren<Text>();
                }

                Transform quitBtn = menuCard.Find("QuitButton");
                if (quitBtn != null)
                {
                    Button btn = quitBtn.GetComponent<Button>();
                    BindButton(btn, QuitGame);
                }
            }

            Transform infoTrans = canvas.transform.Find("ControlsInfo");
            if (infoTrans != null)
            {
                controlsInfoText = infoTrans.GetComponentInChildren<Text>();
            }
        }
    }

    private void BindButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null || action == null) return;

        // If a persistent listener in the scene already targets this method, do not add a duplicate
        for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
        {
            if (btn.onClick.GetPersistentMethodName(i) == action.Method.Name)
            {
                return;
            }
        }

        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    public void OnDeviceButtonClicked()
    {
        // Debounce: prevent rapid multi-clicks or duplicate listeners within 200ms
        float now = Time.realtimeSinceStartup;
        if (now - lastToggleRealtime < 0.2f) return;
        lastToggleRealtime = now;

        DeviceSettings.ToggleDevice();
        Debug.Log($"<color=cyan><b>[MainMenu] Switched Device to: {DeviceSettings.CurrentDevice}</b></color>");
        UpdateDeviceUI();
    }

    private void UpdateDeviceUI()
    {
        bool isMobile = DeviceSettings.IsMobile;

        if (deviceButtonText != null)
        {
            deviceButtonText.text = isMobile
                ? "DEVICE: <color=#FFE040><b>MOBILE (TOUCH)</b></color>"
                : "DEVICE: <color=#55DDFF><b>PC (KEYBOARD)</b></color>";
        }

        if (controlsInfoText != null)
        {
            controlsInfoText.text = isMobile
                ? "<b>MOBILE CONTROLS:</b> Left Joystick to Move  |  Right Swipe to Look  |  JUMP & RUN Buttons\n<color=#40FF70><b>MAZE CHEAT:</b> Tap top-right CHEAT button in maze to show guide arrows!</color>"
                : "<b>PC CONTROLS:</b> WASD to Move  |  Mouse to Look  |  SPACE Jump  |  SHIFT Sprint  |  ESC Pause\n<color=#40FF70><b>MAZE CHEAT:</b> Press <b>[M]</b> in the maze to reveal guide arrows to victory!</color>";
        }
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        CoinThief.hasReachedCheckpoint1 = false;
        SceneManager.LoadScene("SampleScene");
    }

    public void OpenSettings()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            SettingsUIManager.Open(canvas);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game Requested");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
