using UnityEngine;

public class Checkpoint : MonoBehaviour
{
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
                // Tell the player to save this new position!
                health.UpdateCheckpoint(transform.position);
                
                // Turn off the collider so it can't be triggered again
                GetComponent<Collider>().enabled = false;
                
                if (hideOnCollect)
                {
                    // Hides the mesh (like a flag or pad) if you want it to disappear
                    MeshRenderer mesh = GetComponent<MeshRenderer>();
                    if (mesh != null) mesh.enabled = false;
                }
            }
        }
    }
}
