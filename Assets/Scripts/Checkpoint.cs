using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("If true, Abe and all thieves will stop pursuing the player when this checkpoint is reached.")]
    public bool stopAbeThief = true;

    [Tooltip("Optional custom spawn position offset so the player respawns safely on ground level.")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("If checked, the checkpoint will disappear after the player touches it.")]
    public bool hideOnCollect = false;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is the Player
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            
            if (health != null)
            {
                // Calculate spawn position
                Vector3 spawnPos = transform.position + spawnOffset;
                
                // If this object has a collider, use bottom-center of the collider as the ground spawn
                Collider col = GetComponent<Collider>();
                if (col != null && spawnOffset == Vector3.zero)
                {
                    spawnPos = new Vector3(col.bounds.center.x, col.bounds.min.y + 0.1f, col.bounds.center.z);
                }

                health.UpdateCheckpoint(spawnPos);
                Debug.Log("<color=green><b>CHECKPOINT SAVED!</b></color> Position: " + spawnPos + " on " + gameObject.name);
                
                if (stopAbeThief)
                {
                    CoinThief.StopAllThieves();
                }
                
                // Turn off the collider so it can't be triggered again in this life
                if (col != null) col.enabled = false;
                
                if (hideOnCollect)
                {
                    MeshRenderer mesh = GetComponent<MeshRenderer>();
                    if (mesh != null) mesh.enabled = false;
                }
            }
        }
    }
}
