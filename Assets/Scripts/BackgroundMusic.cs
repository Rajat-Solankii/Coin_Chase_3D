using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [Header("Music Settings")]
    [Tooltip("The music file (MP3/WAV) you want to play.")]
    public AudioClip musicClip;

    [Tooltip("Should the music start playing automatically when the game starts?")]
    public bool playOnStart = true;

    [Range(0f, 1f)]
    [Tooltip("Volume of the music from 0 (silent) to 1 (full).")]
    public float volume = 0.5f;

    [Tooltip("How many seconds to wait before the music starts?")]
    public float delayInSeconds = 0f;

    private AudioSource audioSource;

    void Start()
    {
        // Get the AudioSource component (Unity will automatically add one because of the RequireComponent tag at the top)
        audioSource = GetComponent<AudioSource>();
        
        // Configure the AudioSource with our settings
        audioSource.clip = musicClip;
        audioSource.volume = volume;
        audioSource.loop = true; // Background music usually loops forever

        // Play the music if playOnStart is checked and we actually attached an audio file
        if (playOnStart && musicClip != null)
        {
            if (delayInSeconds > 0)
            {
                audioSource.PlayDelayed(delayInSeconds);
            }
            else
            {
                audioSource.Play();
            }
        }
    }
}
