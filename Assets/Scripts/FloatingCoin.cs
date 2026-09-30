using UnityEngine;
using SUPERCharacter;

public class FloatingCoin : MonoBehaviour, ICollectable
{
    [Header("Rotation Settings")]
    [Tooltip("How fast the coin spins (degrees per second)")]
    public float rotationSpeed = 100f;

    [Header("Bobbing Settings")]
    [Tooltip("How high and low the coin bobs")]
    public float bobAmplitude = 0.25f;
    
    [Tooltip("How fast the coin bobs up and down")]
    public float bobFrequency = 2f;

    [Header("Collection Settings")]
    [Tooltip("Optional: Sound to play when the player collects the coin")]
    public AudioClip collectSound;
    
    [Tooltip("Optional: Particle effect to spawn when collected")]
    public GameObject collectEffect;

    private Vector3 startPosition;
    public static System.Collections.Generic.List<FloatingCoin> allCoins = new System.Collections.Generic.List<FloatingCoin>();

    private void Awake()
    {
        // Add this coin to the master list when the game starts
        if (!allCoins.Contains(this))
        {
            allCoins.Add(this);
        }
    }

    private void OnDestroy()
    {
        // Clean up the list if the scene changes
        if (allCoins.Contains(this))
        {
            allCoins.Remove(this);
        }
    }

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        float newY = startPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    // The SUPER Character Controller automatically calls Collect() on anything with ICollectable!
    public void Collect()
    {
        if (CollectableCounter.instance != null)
        {
            CollectableCounter.instance.AddToCount();
        }

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Instead of Destroy(gameObject), we hide it so we can bring it back!
        gameObject.SetActive(false);
    }
}
