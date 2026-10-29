using UnityEngine;

/// <summary>
/// Gently rotates and sways the camera in the Main Menu for a living, cinematic 3D background.
/// </summary>
public class MainMenuCameraPan : MonoBehaviour
{
    [Header("Pan & Sway Settings")]
    public Vector3 lookTarget = new Vector3(0f, 1.2f, 0f);
    public float swaySpeed = 0.25f;
    public float swayAmountX = 1.2f;
    public float swayAmountY = 0.35f;

    private Vector3 initialPosition;

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        float time = Time.time * swaySpeed;
        float offsetX = Mathf.Sin(time) * swayAmountX;
        float offsetY = Mathf.Cos(time * 1.4f) * swayAmountY;

        transform.position = initialPosition + new Vector3(offsetX, offsetY, 0f);
        transform.LookAt(lookTarget);
    }
}
