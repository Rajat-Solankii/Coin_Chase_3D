using UnityEngine;
using SUPERCharacter;

/// <summary>
/// Centralized Settings Manager for Coin Chase 3D.
/// Handles PC and Mobile sensitivity, control resizing, key mappings, and audio.
/// Persists all preferences automatically via PlayerPrefs.
/// </summary>
public static class GameSettings
{
    // --- PC Settings ---
    public static float MouseSensitivity = 8.0f; // Range: 1.0 to 20.0
    public static bool InvertMouseY = false;

    // --- Mobile Settings ---
    public static float TouchSensitivity = 0.22f; // Range: 0.05 to 0.60
    public static bool InvertTouchY = false;
    public static float JoystickScale = 1.0f; // Range: 0.7 to 1.5 (70% to 150%)
    public static float ButtonScale = 1.0f;   // Range: 0.7 to 1.5 (70% to 150%)

    // --- Audio Settings ---
    public static float MasterVolume = 1.0f; // Range: 0.0 to 1.0

    // --- PlayerPrefs Keys ---
    private const string KEY_MOUSE_SENSI = "Settings_MouseSensi";
    private const string KEY_INVERT_MOUSE = "Settings_InvertMouseY";
    private const string KEY_TOUCH_SENSI = "Settings_TouchSensi";
    private const string KEY_INVERT_TOUCH = "Settings_InvertTouchY";
    private const string KEY_JOYSTICK_SCALE = "Settings_JoystickScale";
    private const string KEY_BUTTON_SCALE = "Settings_ButtonScale";
    private const string KEY_MASTER_VOLUME = "Settings_MasterVolume";

    private static bool isInitialized = false;

    public static void InitializeIfNeeded()
    {
        if (!isInitialized)
        {
            Load();
            isInitialized = true;
        }
    }

    public static void Load()
    {
        MouseSensitivity = PlayerPrefs.GetFloat(KEY_MOUSE_SENSI, 8.0f);
        InvertMouseY = PlayerPrefs.GetInt(KEY_INVERT_MOUSE, 0) == 1;

        TouchSensitivity = PlayerPrefs.GetFloat(KEY_TOUCH_SENSI, 0.22f);
        InvertTouchY = PlayerPrefs.GetInt(KEY_INVERT_TOUCH, 0) == 1;
        JoystickScale = PlayerPrefs.GetFloat(KEY_JOYSTICK_SCALE, 1.0f);
        ButtonScale = PlayerPrefs.GetFloat(KEY_BUTTON_SCALE, 1.0f);

        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 1.0f);

        Apply();
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(KEY_MOUSE_SENSI, MouseSensitivity);
        PlayerPrefs.SetInt(KEY_INVERT_MOUSE, InvertMouseY ? 1 : 0);

        PlayerPrefs.SetFloat(KEY_TOUCH_SENSI, TouchSensitivity);
        PlayerPrefs.SetInt(KEY_INVERT_TOUCH, InvertTouchY ? 1 : 0);
        PlayerPrefs.SetFloat(KEY_JOYSTICK_SCALE, JoystickScale);
        PlayerPrefs.SetFloat(KEY_BUTTON_SCALE, ButtonScale);

        PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, MasterVolume);
        PlayerPrefs.Save();

        Apply();
    }

    public static void ResetDefaults()
    {
        MouseSensitivity = 8.0f;
        InvertMouseY = false;

        TouchSensitivity = 0.22f;
        InvertTouchY = false;
        JoystickScale = 1.0f;
        ButtonScale = 1.0f;

        MasterVolume = 1.0f;

        Save();
    }

    public static void Apply()
    {
        // 1. Audio volume
        AudioListener.volume = MasterVolume;

        // 2. Mobile controls sensitivity and size
        if (MobileInputManager.instance != null)
        {
            MobileInputManager.instance.ApplySensitivity(TouchSensitivity, InvertTouchY);
            MobileInputManager.instance.ApplyControlScale(JoystickScale, ButtonScale);
        }

        // 3. PC Character mouse sensitivity and inversion
        var saio = Object.FindObjectOfType<SUPERCharacterAIO>();
        if (saio != null)
        {
            saio.Sensitivity = MouseSensitivity;
            saio.mouseInputInversion = InvertMouseY
                ? MouseInputInversionModes.Y
                : MouseInputInversionModes.None;
        }
    }
}
