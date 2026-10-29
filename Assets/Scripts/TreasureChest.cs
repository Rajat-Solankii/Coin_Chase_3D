using UnityEngine;
using System.Collections;

public class TreasureChest : MonoBehaviour
{
    [Header("Chest Components")]
    public Transform chestLid;
    public Light chestLight;
    public ParticleSystem openParticles;
    public AudioSource audioSource;
    public AudioClip openSound;

    [Header("Reward Settings")]
    public int rewardCoins = 500;
    public float openAngle = -70f;
    public float openSpeed = 2f;

    private bool hasOpened = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasOpened) return;

        if (other.CompareTag("Player"))
        {
            OpenChest();
        }
    }

    public void OpenChest()
    {
        if (hasOpened) return;
        hasOpened = true;

        StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        // 1. Play sound
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }
        else if (CollectableCounter.instance != null && CollectableCounter.instance.collectionSound != null)
        {
            AudioSource.PlayClipAtPoint(CollectableCounter.instance.collectionSound, transform.position, 1f);
        }

        // 2. Play particles
        if (openParticles != null)
        {
            openParticles.Play();
        }

        // 3. Turn on / intensify glowing light
        if (chestLight != null)
        {
            chestLight.enabled = true;
        }

        // 4. Smoothly rotate the lid open
        if (chestLid != null)
        {
            Quaternion startRot = chestLid.localRotation;
            Quaternion targetRot = Quaternion.Euler(openAngle, 0, 0) * startRot;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * openSpeed;
                chestLid.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
        }

        // 5. Add reward coins
        if (CollectableCounter.instance != null)
        {
            CollectableCounter.instance.currentCount += rewardCoins;
        }

        yield return new WaitForSeconds(1.2f);

        // 6. Show Victory screen and finish the game!
        if (UIManager.instance != null)
        {
            UIManager.instance.ShowVictoryScreen(rewardCoins);
        }
    }
}
