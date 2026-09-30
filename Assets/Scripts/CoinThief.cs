using UnityEngine;
using UnityEngine.AI;

public class CoinThief : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("The player character that the thief will chase.")]
    public Transform playerTarget;

    [Header("Movement Settings")]
    [Tooltip("How fast the thief runs. (Used if no NavMeshAgent is attached)")]
    public float moveSpeed = 5f;
    [Tooltip("Minimum distance to stop near the player.")]
    public float stoppingDistance = 1.5f;

    [Header("Stealing Settings")]
    [Tooltip("How many coins the thief steals per hit.")]
    public int coinsToSteal = 1;
    [Tooltip("Time delay before the thief can steal again.")]
    public float stealCooldown = 2f;

    private float lastStealTime = -100f;
    private NavMeshAgent navAgent;
    
    private Vector3 startingPosition;
    public static System.Collections.Generic.List<CoinThief> allThieves = new System.Collections.Generic.List<CoinThief>();

    private void Awake()
    {
        if (!allThieves.Contains(this)) allThieves.Add(this);
    }

    private void OnDestroy()
    {
        if (allThieves.Contains(this)) allThieves.Remove(this);
    }

    void Start()
    {
        startingPosition = transform.position;
        
        // Try to get a NavMeshAgent for smart pathfinding (avoids obstacles on curvy paths)
        navAgent = GetComponent<NavMeshAgent>();

        // If the user didn't assign a player manually, try to find one by tag
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }
    }

    void Update()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // Move towards the player
        if (distanceToPlayer > stoppingDistance)
        {
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                // Use smart Unity NavMesh pathfinding
                navAgent.SetDestination(playerTarget.position);
            }
            else
            {
                // Basic movement straight towards the player, but ignore the player's height (Y) so we don't fly!
                Vector3 targetPosition = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                transform.LookAt(targetPosition);
            }
        }
        else if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.ResetPath(); // Stop moving
        }
    }

    // This handles physics collisions (if the enemy has a Rigidbody + Collider)
    private void OnCollisionEnter(Collision collision)
    {
        CheckForPlayer(collision.gameObject);
    }

    // This handles trigger collisions (if the enemy has a Collider marked as "Is Trigger")
    private void OnTriggerEnter(Collider other)
    {
        CheckForPlayer(other.gameObject);
    }

    private void CheckForPlayer(GameObject hitObj)
    {
        // Ensure we are hitting the player and cooldown is finished
        if (hitObj.CompareTag("Player") && Time.time >= lastStealTime + stealCooldown)
        {
            StealCoins();
        }
    }

    private void StealCoins()
    {
        if (CollectableCounter.instance != null)
        {
            // Only steal if they have coins
            if (CollectableCounter.instance.currentCount > 0)
            {
                CollectableCounter.instance.SubtractFromCount(coinsToSteal);
                lastStealTime = Time.time;
                Debug.Log("Thief stole " + coinsToSteal + " coins! Current total: " + CollectableCounter.instance.currentCount);
                
                // Optional: Play a sound or particle effect here
            }
        }
    }

    public void ResetToStart()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.Warp(startingPosition);
        }
        else
        {
            transform.position = startingPosition;
        }
    }
}
