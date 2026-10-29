using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Universal Mobile Touch Input System for Android and iOS.
/// Provides a Virtual Joystick (Move WASD), Touch Look Swipe Zone (Camera Aim),
/// On-screen Jump, Sprint Toggle, Pause, and Maze Cheat buttons.
/// </summary>
public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager instance { get; private set; }

    [Header("Platform Settings")]
    [Tooltip("Automatically activate on Android and iOS devices.")]
    public bool enableOnMobile = true;

    [Tooltip("Enable mobile touch controls in Unity Editor for easy testing.")]
    public bool simulateInEditor = true;

    [Header("Touch Sensitivities")]
    public float lookSensitivityX = 0.22f;
    public float lookSensitivityY = 0.22f;
    public float joystickRange = 75f;
    public bool invertLookY = false;

    // Static Input States (read directly by SUPERCharacterAIO)
    public static Vector2 MoveInput { get; private set; }
    public static Vector2 LookInput { get; private set; }
    public static bool JumpHeld { get; private set; }
    public static bool JumpTriggered { get; private set; }
    public static bool SprintHeld { get; private set; }
    public static bool SprintTriggered { get; private set; }

    public static Vector2 ConsumeLookInput()
    {
        if (instance == null) return Vector2.zero;
        Vector2 val = instance.lookDeltaAccumulator;
        instance.lookDeltaAccumulator = Vector2.zero;
        LookInput = Vector2.zero;
        return val;
    }

    public static bool ConsumeJumpTriggered()
    {
        bool val = JumpTriggered;
        JumpTriggered = false;
        return val;
    }

    public static bool ConsumeSprintTriggered()
    {
        bool val = SprintTriggered;
        SprintTriggered = false;
        return val;
    }

    public static bool isMobileActive
    {
        get
        {
            if (instance == null) return false;
            return DeviceSettings.IsMobile;
        }
    }

    // UI references
    private GameObject controlsRoot;
    private RectTransform joystickBackground;
    private RectTransform joystickHandle;
    private Vector2 joystickCenter;
    private int joystickPointerId = -999;

    private int lookPointerId = -999;
    private Vector2 lookDeltaAccumulator;

    private bool sprintToggled = false;
    private Image sprintButtonImage;
    private Text cheatButtonText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // On mobile, ensure mouse cursor is not locked/hidden
        if (DeviceSettings.IsMobile)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Start()
    {
        // Build or hook up Mobile UI Canvas
        SetupMobileUI();

        // Initial visibility
        UpdateControlsVisibility();
    }

    private void Update()
    {
        bool anyMenuOpen = IsAnyMenuOpen();
        bool shouldShow = isMobileActive && !anyMenuOpen;

        if (controlsRoot != null && controlsRoot.activeSelf != shouldShow)
        {
            controlsRoot.SetActive(shouldShow);
            if (!shouldShow)
            {
                ResetInputs();
            }
        }

        // Keep cursor unlocked only when mobile is active
        if (isMobileActive && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public static bool IsAnyMenuOpen()
    {
        if (Time.timeScale == 0f) return true;
        if (UIManager.instance != null)
        {
            if (UIManager.instance.pausePanel != null && UIManager.instance.pausePanel.activeSelf) return true;
            if (UIManager.instance.victoryPanel != null && UIManager.instance.victoryPanel.activeSelf) return true;
        }
        var ph = FindObjectOfType<PlayerHealth>();
        if (ph != null && ph.gameOverPanel != null && ph.gameOverPanel.activeSelf) return true;
        return false;
    }

    public static void ResetInputs()
    {
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        JumpHeld = false;
        JumpTriggered = false;
        SprintHeld = false;
        SprintTriggered = false;
        if (instance != null)
        {
            instance.joystickPointerId = -999;
            instance.lookPointerId = -999;
            instance.lookDeltaAccumulator = Vector2.zero;
            if (instance.joystickHandle != null) instance.joystickHandle.anchoredPosition = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        // Reset momentary trigger flags if they weren't consumed in this frame
        JumpTriggered = false;
        SprintTriggered = false;
        if (instance != null)
        {
            LookInput = instance.lookDeltaAccumulator;
            instance.lookDeltaAccumulator = Vector2.zero;
        }
    }

    public void UpdateControlsVisibility()
    {
        if (controlsRoot != null)
        {
            controlsRoot.SetActive(isMobileActive && !IsAnyMenuOpen());
        }
    }

    #region Joystick Input Handling

    public void OnJoystickPointerDown(BaseEventData data)
    {
        PointerEventData ped = data as PointerEventData;
        if (ped == null || joystickPointerId != -999) return;

        joystickPointerId = ped.pointerId;
        UpdateJoystickPosition(ped);
    }

    public void OnJoystickDrag(BaseEventData data)
    {
        PointerEventData ped = data as PointerEventData;
        if (ped == null || ped.pointerId != joystickPointerId) return;

        UpdateJoystickPosition(ped);
    }

    public void OnJoystickPointerUp(BaseEventData data)
    {
        PointerEventData ped = data as PointerEventData;
        if (ped == null || ped.pointerId != joystickPointerId) return;

        joystickPointerId = -999;
        if (joystickHandle != null) joystickHandle.anchoredPosition = Vector2.zero;
        MoveInput = Vector2.zero;
    }

    private void UpdateJoystickPosition(PointerEventData ped)
    {
        if (joystickBackground == null || joystickHandle == null) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBackground, ped.position, ped.pressEventCamera, out localPoint))
        {
            Vector2 offset = Vector2.ClampMagnitude(localPoint, joystickRange);
            joystickHandle.anchoredPosition = offset;
            MoveInput = offset / joystickRange;
        }
    }

    #endregion

    #region Touch Look Handling

    public void OnLookZonePointerDown(BaseEventData data)
    {
        PointerEventData ped = data as PointerEventData;
        if (ped == null || lookPointerId != -999) return;

        lookPointerId = ped.pointerId;
    }

    public void OnLookZoneDrag(BaseEventData data)
    {
        PointerEventData ped = data as PointerEventData;
        if (ped == null || ped.pointerId != lookPointerId) return;

        float deltaY = (invertLookY ? -ped.delta.y : ped.delta.y) * lookSensitivityY;
        float deltaX = ped.delta.x * lookSensitivityX;

        lookDeltaAccumulator += new Vector2(deltaY, deltaX);
    }

    public void OnLookZonePointerUp(BaseEventData data)
    {
        PointerEventData ped = data as PointerEventData;
        if (ped == null || ped.pointerId != lookPointerId) return;

        lookPointerId = -999;
    }

    #endregion

    #region Action Buttons Handling

    public void OnJumpPointerDown(BaseEventData data)
    {
        JumpHeld = true;
        JumpTriggered = true;
    }

    public void OnJumpPointerUp(BaseEventData data)
    {
        JumpHeld = false;
    }

    public void OnSprintButtonClicked()
    {
        sprintToggled = !sprintToggled;
        SprintHeld = sprintToggled;
        SprintTriggered = true;

        if (sprintButtonImage != null)
        {
            // Gold when sprint locked on, cyan when off
            sprintButtonImage.color = sprintToggled
                ? new Color(1f, 0.85f, 0.2f, 0.85f)
                : new Color(0.15f, 0.25f, 0.35f, 0.75f);
        }
    }

    public void OnPauseButtonClicked()
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.TogglePause();
        }
    }

    public void OnMazeCheatButtonClicked()
    {
        if (MazeCheatManager.instance != null)
        {
            MazeCheatManager.instance.ToggleCheat();
            UpdateCheatButtonUI(MazeCheatManager.instance.isCheatActive);
        }
    }

    public void UpdateCheatButtonUI(bool active)
    {
        if (cheatButtonText != null)
        {
            cheatButtonText.text = active ? "<b>CHEAT: ON</b>" : "<b>CHEAT: OFF</b>";
            cheatButtonText.color = active ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.9f, 0.9f);
        }
    }

    #endregion

    #region Procedural Mobile UI Creation

    private void SetupMobileUI()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("MobileControlsCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        Transform existingRoot = canvas.transform.Find("[MobileControlsRoot]");
        if (existingRoot != null)
        {
            controlsRoot = existingRoot.gameObject;
            return;
        }

        controlsRoot = new GameObject("[MobileControlsRoot]");
        controlsRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRT = controlsRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;

        // 1. Touch Look Zone (covers right half of screen)
        CreateTouchLookZone(controlsRoot.transform);

        // 2. Virtual Joystick (Bottom Left)
        CreateVirtualJoystick(controlsRoot.transform);

        // 3. Jump Button (Bottom Right)
        CreateJumpButton(controlsRoot.transform);

        // 4. Sprint Button (Next to Jump)
        CreateSprintButton(controlsRoot.transform);

        // 5. Pause Button (Top Right)
        CreatePauseButton(controlsRoot.transform);

        // 6. Maze Cheat Button (Top Center-Right)
        CreateMazeCheatButton(controlsRoot.transform);
    }

    private void CreateTouchLookZone(Transform parent)
    {
        GameObject lookGO = new GameObject("TouchLookZone");
        lookGO.transform.SetParent(parent, false);

        RectTransform rt = lookGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.35f, 0f);
        rt.anchorMax = new Vector2(1f, 0.85f);
        rt.sizeDelta = Vector2.zero;

        Image img = lookGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.001f); // Transparent raycastable target
        img.raycastTarget = true;

        EventTrigger trigger = lookGO.AddComponent<EventTrigger>();
        AddEventTrigger(trigger, EventTriggerType.PointerDown, OnLookZonePointerDown);
        AddEventTrigger(trigger, EventTriggerType.Drag, OnLookZoneDrag);
        AddEventTrigger(trigger, EventTriggerType.PointerUp, OnLookZonePointerUp);
    }

    private void CreateVirtualJoystick(Transform parent)
    {
        // Joystick Base
        GameObject joyGO = new GameObject("VirtualJoystick");
        joyGO.transform.SetParent(parent, false);

        joystickBackground = joyGO.AddComponent<RectTransform>();
        joystickBackground.anchorMin = new Vector2(0f, 0f);
        joystickBackground.anchorMax = new Vector2(0f, 0f);
        joystickBackground.pivot = new Vector2(0.5f, 0.5f);
        joystickBackground.anchoredPosition = new Vector2(170f, 170f);
        joystickBackground.sizeDelta = new Vector2(180f, 180f);

        Image bgImg = joyGO.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.15f, 0.2f, 0.55f);
        bgImg.raycastTarget = true;

        Outline bgOutline = joyGO.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.4f, 0.8f, 1f, 0.45f);
        bgOutline.effectDistance = new Vector2(2f, -2f);

        // Joystick Handle Knob
        GameObject knobGO = new GameObject("JoystickKnob");
        knobGO.transform.SetParent(joyGO.transform, false);

        joystickHandle = knobGO.AddComponent<RectTransform>();
        joystickHandle.anchorMin = new Vector2(0.5f, 0.5f);
        joystickHandle.anchorMax = new Vector2(0.5f, 0.5f);
        joystickHandle.pivot = new Vector2(0.5f, 0.5f);
        joystickHandle.anchoredPosition = Vector2.zero;
        joystickHandle.sizeDelta = new Vector2(75f, 75f);

        Image knobImg = knobGO.AddComponent<Image>();
        knobImg.color = new Color(0.25f, 0.7f, 1f, 0.85f);
        knobImg.raycastTarget = false;

        EventTrigger trigger = joyGO.AddComponent<EventTrigger>();
        AddEventTrigger(trigger, EventTriggerType.PointerDown, OnJoystickPointerDown);
        AddEventTrigger(trigger, EventTriggerType.Drag, OnJoystickDrag);
        AddEventTrigger(trigger, EventTriggerType.PointerUp, OnJoystickPointerUp);
    }

    private void CreateJumpButton(Transform parent)
    {
        GameObject btnGO = new GameObject("MobileJumpButton");
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-120f, 130f);
        rt.sizeDelta = new Vector2(100f, 100f);

        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.15f, 0.55f, 0.95f, 0.78f);

        Outline outline = btnGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.8f, 1f, 0.6f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

        Text txt = txtGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 22);
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = "<b>▲\nJUMP</b>";

        EventTrigger trigger = btnGO.AddComponent<EventTrigger>();
        AddEventTrigger(trigger, EventTriggerType.PointerDown, OnJumpPointerDown);
        AddEventTrigger(trigger, EventTriggerType.PointerUp, OnJumpPointerUp);
    }

    private void CreateSprintButton(Transform parent)
    {
        GameObject btnGO = new GameObject("MobileSprintButton");
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-235f, 105f);
        rt.sizeDelta = new Vector2(85f, 85f);

        sprintButtonImage = btnGO.AddComponent<Image>();
        sprintButtonImage.color = new Color(0.15f, 0.25f, 0.35f, 0.75f);

        Outline outline = btnGO.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.2f, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(OnSprintButtonClicked);

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

        Text txt = txtGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 20);
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = "<b>⚡\nRUN</b>";
    }

    private void CreatePauseButton(Transform parent)
    {
        GameObject btnGO = new GameObject("MobilePauseButton");
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-70f, -65f);
        rt.sizeDelta = new Vector2(65f, 65f);

        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.1f, 0.12f, 0.18f, 0.8f);

        Outline outline = btnGO.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.4f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(OnPauseButtonClicked);

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

        Text txt = txtGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 22);
        txt.fontSize = 26;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = "<b>⏸</b>";
    }

    private void CreateMazeCheatButton(Transform parent)
    {
        GameObject btnGO = new GameObject("MobileCheatButton");
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-190f, -65f);
        rt.sizeDelta = new Vector2(145f, 50f);

        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.12f, 0.45f, 0.25f, 0.85f);

        Outline outline = btnGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 1f, 0.5f, 0.6f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(OnMazeCheatButtonClicked);

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

        cheatButtonText = txtGO.AddComponent<Text>();
        cheatButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 18);
        cheatButtonText.fontSize = 18;
        cheatButtonText.alignment = TextAnchor.MiddleCenter;
        cheatButtonText.color = Color.white;
        cheatButtonText.supportRichText = true;
        cheatButtonText.text = "<b>CHEAT: OFF</b>";
    }

    private static void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    #endregion
}
