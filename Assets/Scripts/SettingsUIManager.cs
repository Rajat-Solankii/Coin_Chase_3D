using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Universal UI Modal for Game Settings.
/// Provides real-time sliders and toggles for:
/// - Android/Mobile: Touch Look Sensitivity, Joystick Size, Action Buttons Size, Invert Touch Y.
/// - PC: Mouse Sensitivity, Invert Mouse Y, Key Mappings Reference.
/// - Audio & General: Master Volume, Device Mode Selection.
/// Works cleanly on both MainMenu and in-game PauseMenu.
/// </summary>
public class SettingsUIManager : MonoBehaviour
{
    public static SettingsUIManager instance { get; private set; }

    private GameObject modalRoot;
    private GameObject mobileTabContent;
    private GameObject pcTabContent;
    private GameObject audioTabContent;

    private Button mobileTabBtn;
    private Button pcTabBtn;
    private Button audioTabBtn;

    private Action onCloseCallback;

    public static void Open(Canvas parentCanvas, Action onClose = null)
    {
        if (parentCanvas == null) parentCanvas = FindObjectOfType<Canvas>();
        if (parentCanvas == null) return;

        // If already open, bring to front
        Transform existing = parentCanvas.transform.Find("[SettingsModalRoot]");
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return;
        }

        GameObject host = new GameObject("[SettingsModalHost]");
        host.transform.SetParent(parentCanvas.transform, false);
        var mgr = host.AddComponent<SettingsUIManager>();
        mgr.onCloseCallback = onClose;
        mgr.BuildUI(parentCanvas);
    }

    private void Awake()
    {
        instance = this;
    }

    private void BuildUI(Canvas canvas)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 16);

        // 1. Fullscreen Semi-Transparent Backdrop (blocks background clicks)
        modalRoot = new GameObject("[SettingsModalRoot]");
        modalRoot.transform.SetParent(canvas.transform, false);
        modalRoot.transform.SetAsLastSibling();

        RectTransform backdropRT = modalRoot.AddComponent<RectTransform>();
        backdropRT.anchorMin = Vector2.zero;
        backdropRT.anchorMax = Vector2.one;
        backdropRT.sizeDelta = Vector2.zero;

        Image backdropImg = modalRoot.AddComponent<Image>();
        backdropImg.color = new Color(0.02f, 0.04f, 0.08f, 0.88f); // Dark translucent veil
        backdropImg.raycastTarget = true;

        // 2. Main Settings Card (Centered, 700 x 540)
        GameObject cardGO = new GameObject("SettingsCard");
        cardGO.transform.SetParent(modalRoot.transform, false);

        RectTransform cardRT = cardGO.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(700f, 540f);
        cardRT.anchoredPosition = Vector2.zero;

        Image cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f); // Sleek obsidian slate

        Outline cardOutline = cardGO.AddComponent<Outline>();
        cardOutline.effectColor = new Color(1f, 0.85f, 0.3f, 0.6f); // Soft golden metallic rim
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // 3. Header Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(cardGO.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -16f);
        titleRT.sizeDelta = new Vector2(0f, 44f);

        Text titleText = titleGO.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 26;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.88f, 0.35f, 1f);
        titleText.supportRichText = true;
        titleText.text = "<b>⚙   GAME SETTINGS & CONTROLS</b>";

        // 4. Tab Navigation Bar
        GameObject tabBarGO = new GameObject("TabBar");
        tabBarGO.transform.SetParent(cardGO.transform, false);
        RectTransform tabBarRT = tabBarGO.AddComponent<RectTransform>();
        tabBarRT.anchorMin = new Vector2(0f, 1f);
        tabBarRT.anchorMax = new Vector2(1f, 1f);
        tabBarRT.pivot = new Vector2(0.5f, 1f);
        tabBarRT.anchoredPosition = new Vector2(0f, -68f);
        tabBarRT.sizeDelta = new Vector2(640f, 44f);

        mobileTabBtn = CreateTabButton(tabBarGO.transform, "📱 MOBILE CONTROLS", new Vector2(-215f, 0f), 205f, () => SwitchTab(0));
        pcTabBtn = CreateTabButton(tabBarGO.transform, "🖥️ PC CONTROLS", new Vector2(0f, 0f), 205f, () => SwitchTab(1));
        audioTabBtn = CreateTabButton(tabBarGO.transform, "🔊 AUDIO & GENERAL", new Vector2(215f, 0f), 205f, () => SwitchTab(2));

        // 5. Content Area Panel
        GameObject contentAreaGO = new GameObject("ContentArea");
        contentAreaGO.transform.SetParent(cardGO.transform, false);
        RectTransform contentRT = contentAreaGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.05f, 0.16f);
        contentRT.anchorMax = new Vector2(0.95f, 0.77f);
        contentRT.sizeDelta = Vector2.zero;

        Image contentBg = contentAreaGO.AddComponent<Image>();
        contentBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);

        Outline contentOutline = contentAreaGO.AddComponent<Outline>();
        contentOutline.effectColor = new Color(0.3f, 0.6f, 0.9f, 0.25f);
        contentOutline.effectDistance = new Vector2(1f, -1f);

        // Build individual tab contents
        mobileTabContent = BuildMobileTab(contentAreaGO.transform, font);
        pcTabContent = BuildPcTab(contentAreaGO.transform, font);
        audioTabContent = BuildAudioTab(contentAreaGO.transform, font);

        // 6. Bottom Action Footer (Reset Defaults & Save/Close)
        GameObject footerGO = new GameObject("Footer");
        footerGO.transform.SetParent(cardGO.transform, false);
        RectTransform footerRT = footerGO.AddComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0f, 0f);
        footerRT.anchorMax = new Vector2(1f, 0f);
        footerRT.pivot = new Vector2(0.5f, 0f);
        footerRT.anchoredPosition = new Vector2(0f, 18f);
        footerRT.sizeDelta = new Vector2(640f, 50f);

        CreateFooterButton(footerGO.transform, "↺  RESET DEFAULTS", new Vector2(-150f, 0f), 260f, new Color(0.6f, 0.2f, 0.2f, 0.95f), () =>
        {
            GameSettings.ResetDefaults();
            CloseAndReopen();
        });

        CreateFooterButton(footerGO.transform, "✔  SAVE & CLOSE", new Vector2(150f, 0f), 260f, new Color(0.12f, 0.65f, 0.32f, 0.95f), () =>
        {
            GameSettings.Save();
            Close();
        });

        // Default active tab based on active device
        SwitchTab(DeviceSettings.IsMobile ? 0 : 1);
    }

    private void SwitchTab(int tabIndex)
    {
        mobileTabContent.SetActive(tabIndex == 0);
        pcTabContent.SetActive(tabIndex == 1);
        audioTabContent.SetActive(tabIndex == 2);

        SetTabButtonHighlight(mobileTabBtn, tabIndex == 0);
        SetTabButtonHighlight(pcTabBtn, tabIndex == 1);
        SetTabButtonHighlight(audioTabBtn, tabIndex == 2);
    }

    private void SetTabButtonHighlight(Button btn, bool active)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = active ? new Color(0.18f, 0.45f, 0.75f, 1f) : new Color(0.10f, 0.16f, 0.24f, 0.9f);
        }
        Text txt = btn.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.color = active ? new Color(1f, 0.95f, 0.5f) : new Color(0.8f, 0.85f, 0.9f);
        }
    }

    #region Tab 1: Mobile Controls (Sensitivity + Resizing)

    private GameObject BuildMobileTab(Transform parent, Font font)
    {
        GameObject panel = new GameObject("MobileTab");
        panel.transform.SetParent(parent, false);
        FillRect(panel.AddComponent<RectTransform>());

        float startY = -35f;
        float spacingY = -68f;

        // 1. Touch Look Sensitivity Slider
        CreateSliderRow(panel.transform, font, "TOUCH LOOK SENSITIVITY", startY, 0.05f, 0.60f, GameSettings.TouchSensitivity, "F2", false, val =>
        {
            GameSettings.TouchSensitivity = val;
            GameSettings.Apply();
        });

        // 2. Virtual Joystick Size Slider (Resizing)
        CreateSliderRow(panel.transform, font, "JOYSTICK SIZE (SCALE)", startY + spacingY, 0.70f, 1.50f, GameSettings.JoystickScale, "P0", true, val =>
        {
            GameSettings.JoystickScale = val;
            GameSettings.Apply();
        });

        // 3. Action Buttons Size Slider (Resizing)
        CreateSliderRow(panel.transform, font, "ACTION BUTTONS SIZE", startY + spacingY * 2, 0.70f, 1.50f, GameSettings.ButtonScale, "P0", true, val =>
        {
            GameSettings.ButtonScale = val;
            GameSettings.Apply();
        });

        // 4. Invert Look Y Toggle
        CreateToggleRow(panel.transform, font, "INVERT TOUCH LOOK Y-AXIS", startY + spacingY * 3, GameSettings.InvertTouchY, val =>
        {
            GameSettings.InvertTouchY = val;
            GameSettings.Apply();
        });

        return panel;
    }

    #endregion

    #region Tab 2: PC Controls (Sensitivity + Key Mappings)

    private GameObject BuildPcTab(Transform parent, Font font)
    {
        GameObject panel = new GameObject("PcTab");
        panel.transform.SetParent(parent, false);
        FillRect(panel.AddComponent<RectTransform>());

        float startY = -32f;
        float spacingY = -60f;

        // 1. Mouse Sensitivity Slider
        CreateSliderRow(panel.transform, font, "MOUSE SENSITIVITY", startY, 1.0f, 20.0f, GameSettings.MouseSensitivity, "F1", false, val =>
        {
            GameSettings.MouseSensitivity = val;
            GameSettings.Apply();
        });

        // 2. Invert Mouse Y Toggle
        CreateToggleRow(panel.transform, font, "INVERT MOUSE Y-AXIS", startY + spacingY, GameSettings.InvertMouseY, val =>
        {
            GameSettings.InvertMouseY = val;
            GameSettings.Apply();
        });

        // 3. Key Mapping Display Box
        GameObject keysBox = new GameObject("KeyMappingsBox");
        keysBox.transform.SetParent(panel.transform, false);
        RectTransform kbRT = keysBox.AddComponent<RectTransform>();
        kbRT.anchorMin = new Vector2(0.04f, 0.05f);
        kbRT.anchorMax = new Vector2(0.96f, 0.52f);
        kbRT.sizeDelta = Vector2.zero;

        Image kbBg = keysBox.AddComponent<Image>();
        kbBg.color = new Color(0.03f, 0.05f, 0.08f, 0.9f);

        Outline kbOutline = keysBox.AddComponent<Outline>();
        kbOutline.effectColor = new Color(0.4f, 0.7f, 1f, 0.35f);
        kbOutline.effectDistance = new Vector2(1f, -1f);

        // Header for Key Mapping
        GameObject kbTitle = new GameObject("KeysHeader");
        kbTitle.transform.SetParent(keysBox.transform, false);
        RectTransform ktRT = kbTitle.AddComponent<RectTransform>();
        ktRT.anchorMin = new Vector2(0f, 1f);
        ktRT.anchorMax = new Vector2(1f, 1f);
        ktRT.pivot = new Vector2(0.5f, 1f);
        ktRT.anchoredPosition = new Vector2(0f, -8f);
        ktRT.sizeDelta = new Vector2(0f, 26f);

        Text ktText = kbTitle.AddComponent<Text>();
        ktText.font = font;
        ktText.fontSize = 15;
        ktText.alignment = TextAnchor.MiddleCenter;
        ktText.color = new Color(0.4f, 0.9f, 1f);
        ktText.text = "<b>⌨ KEYBOARD & MOUSE CONTROLS MAPPING</b>";

        // Keys Grid Rows
        string[,] mappings = new string[,]
        {
            { "Move Forward / Left / Back / Right", "W / A / S / D  (or Arrow Keys)" },
            { "Camera Look / Aim", "Mouse Movement" },
            { "Jump / Vault", "SPACEBAR" },
            { "Sprint (Fast Run)", "LEFT SHIFT" },
            { "Maze Guide (Cheat Arrows)", "M Key" },
            { "Pause / Resume Game", "ESCAPE Key" }
        };

        for (int i = 0; i < 6; i++)
        {
            float rowY = -38f - (i * 22f);
            CreateKeyMapRow(keysBox.transform, font, mappings[i, 0], mappings[i, 1], rowY);
        }

        return panel;
    }

    private void CreateKeyMapRow(Transform parent, Font font, string action, string binding, float y)
    {
        GameObject rowGO = new GameObject("KeyRow");
        rowGO.transform.SetParent(parent, false);
        RectTransform rt = rowGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-30f, 20f);

        // Action Text
        GameObject aGO = new GameObject("Action");
        aGO.transform.SetParent(rowGO.transform, false);
        RectTransform art = aGO.AddComponent<RectTransform>();
        art.anchorMin = new Vector2(0f, 0f); art.anchorMax = new Vector2(0.55f, 1f);
        art.sizeDelta = Vector2.zero;
        Text at = aGO.AddComponent<Text>();
        at.font = font; at.fontSize = 13; at.alignment = TextAnchor.MiddleLeft;
        at.color = new Color(0.85f, 0.9f, 0.95f);
        at.text = action;

        // Binding Text
        GameObject bGO = new GameObject("Binding");
        bGO.transform.SetParent(rowGO.transform, false);
        RectTransform brt = bGO.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.55f, 0f); brt.anchorMax = new Vector2(1f, 1f);
        brt.sizeDelta = Vector2.zero;
        Text bt = bGO.AddComponent<Text>();
        bt.font = font; bt.fontSize = 13; bt.alignment = TextAnchor.MiddleRight;
        bt.color = new Color(1f, 0.88f, 0.3f);
        bt.supportRichText = true;
        bt.text = $"<b>[ {binding} ]</b>";
    }

    #endregion

    #region Tab 3: Audio & General

    private GameObject BuildAudioTab(Transform parent, Font font)
    {
        GameObject panel = new GameObject("AudioTab");
        panel.transform.SetParent(parent, false);
        FillRect(panel.AddComponent<RectTransform>());

        float startY = -40f;
        float spacingY = -70f;

        // 1. Master Audio Volume
        CreateSliderRow(panel.transform, font, "MASTER AUDIO VOLUME", startY, 0.0f, 1.0f, GameSettings.MasterVolume, "P0", true, val =>
        {
            GameSettings.MasterVolume = val;
            GameSettings.Apply();
        });

        // 2. Active Device Selection (PC vs Mobile)
        GameObject devRowGO = new GameObject("DeviceRow");
        devRowGO.transform.SetParent(panel.transform, false);
        RectTransform rt = devRowGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, startY + spacingY);
        rt.sizeDelta = new Vector2(-40f, 50f);

        Text label = CreateLabel(devRowGO.transform, font, "ACTIVE CONTROLS MODE", 15, TextAnchor.MiddleLeft);
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(0.5f, 1f);
        lrt.sizeDelta = Vector2.zero;

        // Toggle Button
        GameObject btnGO = new GameObject("DeviceToggleBtn");
        btnGO.transform.SetParent(devRowGO.transform, false);
        RectTransform brt = btnGO.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.52f, 0.1f); brt.anchorMax = new Vector2(1f, 0.9f);
        brt.sizeDelta = Vector2.zero;

        Image bImg = btnGO.AddComponent<Image>();
        bImg.color = DeviceSettings.IsMobile
            ? new Color(0.7f, 0.55f, 0.1f, 0.95f)
            : new Color(0.12f, 0.45f, 0.65f, 0.95f);

        Outline bOutline = btnGO.AddComponent<Outline>();
        bOutline.effectColor = Color.white;
        bOutline.effectDistance = new Vector2(1f, -1f);

        Button btn = btnGO.AddComponent<Button>();
        Text btnTxt = CreateLabel(btnGO.transform, font, "", 15, TextAnchor.MiddleCenter);
        FillRect(btnTxt.GetComponent<RectTransform>());
        Action updateBtnText = () =>
        {
            btnTxt.text = DeviceSettings.IsMobile
                ? "<b>MODE: 📱 MOBILE (TOUCH)</b>"
                : "<b>MODE: 🖥️ PC (KEYBOARD)</b>";
            bImg.color = DeviceSettings.IsMobile
                ? new Color(0.7f, 0.55f, 0.1f, 0.95f)
                : new Color(0.12f, 0.45f, 0.65f, 0.95f);
        };
        updateBtnText();

        btn.onClick.AddListener(() =>
        {
            DeviceSettings.ToggleDevice();
            updateBtnText();
            GameSettings.Apply();
        });

        return panel;
    }

    #endregion

    #region Helper Widgets (Sliders, Toggles, Buttons)

    private void CreateSliderRow(Transform parent, Font font, string labelText, float y, float min, float max, float currentVal, string format, bool isPercent, Action<float> onValChanged)
    {
        GameObject row = new GameObject("SliderRow_" + labelText);
        row.transform.SetParent(parent, false);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-40f, 52f);

        // 1. Label
        Text label = CreateLabel(row.transform, font, $"<b>{labelText}</b>", 14, TextAnchor.MiddleLeft);
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0.5f); lrt.anchorMax = new Vector2(0.42f, 1f);
        lrt.sizeDelta = Vector2.zero;

        // 2. Value Text Display
        Text valText = CreateLabel(row.transform, font, FormatValue(currentVal, format, isPercent), 14, TextAnchor.MiddleRight);
        RectTransform vrt = valText.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0.85f, 0.5f); vrt.anchorMax = new Vector2(1f, 1f);
        vrt.sizeDelta = Vector2.zero;
        valText.color = new Color(1f, 0.88f, 0.3f);

        // 3. Slider Object
        GameObject sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(row.transform, false);
        RectTransform srt = sliderGO.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0.45f);
        srt.sizeDelta = Vector2.zero;

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = currentVal;

        // Slider Background Track
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        RectTransform bgrt = bgGO.AddComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one; bgrt.sizeDelta = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.16f, 0.22f, 1f);

        // Fill Area & Fill
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fart = fillArea.AddComponent<RectTransform>();
        fart.anchorMin = Vector2.zero; fart.anchorMax = Vector2.one; fart.sizeDelta = Vector2.zero;

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillArea.transform, false);
        RectTransform frt = fillGO.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.sizeDelta = Vector2.zero;
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.65f, 0.95f, 1f); // Vibrant cyan-blue fill
        slider.fillRect = frt;

        // Handle Area & Handle Knob
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform hart = handleArea.AddComponent<RectTransform>();
        hart.anchorMin = Vector2.zero; hart.anchorMax = Vector2.one; hart.sizeDelta = Vector2.zero;

        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleArea.transform, false);
        RectTransform hrt = handleGO.AddComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(24f, 24f);
        Image hImg = handleGO.AddComponent<Image>();
        hImg.color = Color.white;
        slider.handleRect = hrt;
        slider.targetGraphic = hImg;

        slider.onValueChanged.AddListener(v =>
        {
            valText.text = FormatValue(v, format, isPercent);
            onValChanged?.Invoke(v);
        });
    }

    private void CreateToggleRow(Transform parent, Font font, string labelText, float y, bool currentVal, Action<bool> onToggleChanged)
    {
        GameObject row = new GameObject("ToggleRow_" + labelText);
        row.transform.SetParent(parent, false);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-40f, 44f);

        Text label = CreateLabel(row.transform, font, $"<b>{labelText}</b>", 14, TextAnchor.MiddleLeft);
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(0.65f, 1f);
        lrt.sizeDelta = Vector2.zero;

        GameObject btnGO = new GameObject("ToggleBtn");
        btnGO.transform.SetParent(row.transform, false);
        RectTransform brt = btnGO.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.68f, 0.1f); brt.anchorMax = new Vector2(1f, 0.9f);
        brt.sizeDelta = Vector2.zero;

        Image bImg = btnGO.AddComponent<Image>();
        Button btn = btnGO.AddComponent<Button>();
        Text btnTxt = CreateLabel(btnGO.transform, font, "", 14, TextAnchor.MiddleCenter);
        FillRect(btnTxt.GetComponent<RectTransform>());

        bool state = currentVal;
        Action updateVisuals = () =>
        {
            bImg.color = state ? new Color(0.6f, 0.25f, 0.15f, 0.9f) : new Color(0.12f, 0.35f, 0.5f, 0.9f);
            btnTxt.text = state ? "<b>INVERTED (ON)</b>" : "<b>NORMAL (OFF)</b>";
            btnTxt.color = state ? new Color(1f, 0.8f, 0.6f) : Color.white;
        };
        updateVisuals();

        btn.onClick.AddListener(() =>
        {
            state = !state;
            updateVisuals();
            onToggleChanged?.Invoke(state);
        });
    }

    private Button CreateTabButton(Transform parent, string title, Vector2 pos, float width, Action onClick)
    {
        GameObject btnGO = new GameObject("Tab_" + title);
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 42f);

        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.10f, 0.16f, 0.24f, 0.9f);

        Outline outline = btnGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.7f, 1f, 0.3f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);
        Text txt = CreateLabel(btnGO.transform, font, $"<b>{title}</b>", 13, TextAnchor.MiddleCenter);
        FillRect(txt.GetComponent<RectTransform>());

        return btn;
    }

    private void CreateFooterButton(Transform parent, string title, Vector2 pos, float width, Color color, Action onClick)
    {
        GameObject btnGO = new GameObject("FooterBtn_" + title);
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 44f);

        Image img = btnGO.AddComponent<Image>();
        img.color = color;

        Outline outline = btnGO.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 16);
        Text txt = CreateLabel(btnGO.transform, font, $"<b>{title}</b>", 16, TextAnchor.MiddleCenter);
        FillRect(txt.GetComponent<RectTransform>());
        txt.color = Color.white;
    }

    private Text CreateLabel(Transform parent, Font font, string text, int fontSize, TextAnchor alignment)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = Color.white;
        t.supportRichText = true;
        t.text = text;
        return t;
    }

    private static void FillRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static string FormatValue(float val, string format, bool isPercent)
    {
        if (isPercent)
        {
            return Mathf.RoundToInt(val * 100f) + "%";
        }
        return val.ToString(format);
    }

    #endregion

    public void Close()
    {
        if (modalRoot != null)
        {
            Destroy(modalRoot);
        }
        onCloseCallback?.Invoke();
        Destroy(gameObject);
    }

    private void CloseAndReopen()
    {
        Canvas c = GetComponentInParent<Canvas>();
        Close();
        if (c != null) Open(c);
    }
}
