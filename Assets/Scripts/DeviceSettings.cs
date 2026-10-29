using UnityEngine;

public enum GameDeviceType
{
    PC = 0,
    Mobile = 1
}

public static class DeviceSettings
{
    private const string PREF_KEY = "Game_SelectedDevice";

    public static GameDeviceType CurrentDevice
    {
        get
        {
            if (PlayerPrefs.HasKey(PREF_KEY))
            {
                return (GameDeviceType)PlayerPrefs.GetInt(PREF_KEY);
            }
            // Auto default: Mobile on handheld devices, PC on desktop/editor
            return Application.isMobilePlatform ? GameDeviceType.Mobile : GameDeviceType.PC;
        }
        set
        {
            PlayerPrefs.SetInt(PREF_KEY, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static bool IsMobile => Application.isMobilePlatform || CurrentDevice == GameDeviceType.Mobile;
    public static bool IsPC => CurrentDevice == GameDeviceType.PC;

    public static void ToggleDevice()
    {
        CurrentDevice = (CurrentDevice == GameDeviceType.PC) ? GameDeviceType.Mobile : GameDeviceType.PC;
    }
}
