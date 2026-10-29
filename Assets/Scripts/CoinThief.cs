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
    private Animator animator;
    
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    public static System.Collections.Generic.List<CoinThief> allThieves = new System.Collections.Generic.List<CoinThief>();
    public static bool hasReachedCheckpoint1 = false;
    public bool isStopped = false;

    private void Awake()
    {
        if (!allThieves.Contains(this)) allThieves.Add(this);
        startingPosition = transform.position;
        startingRotation = transform.rotation;
    }

    private void OnDestroy()
    {
        if (allThieves.Contains(this)) allThieves.Remove(this);
    }

    void Start()
    {
        if (startingPosition == Vector3.zero && startingRotation == Quaternion.identity)
        {
            startingPosition = transform.position;
            startingRotation = transform.rotation;
        }

        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (hasReachedCheckpoint1)
        {
            StopFollowing();
            return;
        }

        if (animator != null) animator.SetBool("IsRunning", true);

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
        if (isStopped || hasReachedCheckpoint1)
        {
            if (navAgent != null && navAgent.isOnNavMesh && !navAgent.isStopped)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }
            if (animator != null) animator.SetBool("IsRunning", false);
            return;
        }

        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // Move towards the player
        if (distanceToPlayer > stoppingDistance)
        {
            if (animator != null) animator.SetBool("IsRunning", true);

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
        else
        {
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.ResetPath(); // Stop moving
            }
            if (animator != null) animator.SetBool("IsRunning", false);
        }
    }

    public void StopFollowing()
    {
        isStopped = true;
        playerTarget = null;

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
            navAgent.Warp(startingPosition);
        }
        else
        {
            transform.position = startingPosition;
        }

        transform.rotation = startingRotation;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
        }
    }

    public static void StopAllThieves()
    {
        hasReachedCheckpoint1 = true;
        foreach (CoinThief thief in allThieves)
        {
            if (thief != null)
            {
                thief.StopFollowing();
            }
        }
        Debug.Log("Checkpoint 1 reached: Abe returned to starting position and stopped following!");
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
        if (isStopped || hasReachedCheckpoint1) return;

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
            }
        }
    }

    public void ResetToStart()
    {
        if (hasReachedCheckpoint1)
        {
            StopFollowing();
            return;
        }

        isStopped = false;
        if (animator != null) animator.SetBool("IsRunning", true);

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.Warp(startingPosition);
        }
        else
        {
            transform.position = startingPosition;
        }

        transform.rotation = startingRotation;
    }
}
