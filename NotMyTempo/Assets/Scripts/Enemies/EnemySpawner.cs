using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Keeps ground enemies coming. Every few seconds it spawns one at a random
/// spawn point (snapped onto the NavMesh), as long as fewer than the max are alive.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("What to spawn")]
    [Tooltip("The Enemy prefab from the Project window (needs EnemyAi + NavMeshAgent)")]
    [SerializeField] private EnemyAi enemyPrefab;

    [Tooltip("Where ground enemies can appear")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Pacing")]
    [Tooltip("Seconds before the first enemy appears")]
    [SerializeField] private float startDelay = 2f;
    [Tooltip("Seconds between spawns")]
    [SerializeField] private float spawnInterval = 2.5f;
    [Tooltip("Never more than this many enemies alive at once")]
    [SerializeField] private int maxAlive = 8;

    [Header("Placement")]
    [Tooltip("Skip spawn points closer than this to the player, in meters")]
    [SerializeField] private float minDistanceFromPlayer = 10f;
    [Tooltip("How far from a spawn point to look for the NavMesh, in meters")]
    [SerializeField] private float navMeshSearchRadius = 3f;

    private readonly List<EnemyAi> alive = new List<EnemyAi>();
    private readonly List<Transform> candidates = new List<Transform>();
    private PlayerHealth player;
    private Transform enemyContainer;
    private float nextSpawnTime;

    public int AliveCount => alive.Count; // the HUD can read this later

    private void Start()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("EnemySpawner needs an Enemy prefab and at least one spawn point.", this);
            enabled = false;
            return;
        }

        player = FindFirstObjectByType<PlayerHealth>();
        enemyContainer = new GameObject("Spawned Enemies").transform;
        nextSpawnTime = Time.time + startDelay;
    }

    private void Update()
    {
        if (player == null || player.IsDead) // if we're dead, don't spawn things
            return;

        // A destroyed enemy counts as "null" in Unity, so this clears out the dead ones
        alive.RemoveAll(enemy => enemy == null);

        if (Time.time < nextSpawnTime || alive.Count >= maxAlive)
            return;

        nextSpawnTime = Time.time + spawnInterval; // iterate the spawn time
        TrySpawn(); // try to spawn
    }

    private void TrySpawn()
    {
        Transform point = PickSpawnPoint();

        // Find the closest spot on the NavMesh to this spawn point
        if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning("Spawn point '" + point.name + "' isn't near the NavMesh. " +
                             "Move it closer to the floor or raise Nav Mesh Search Radius.", point);
            return;
        }

        // Face the player so enemies don't spawn looking at a wall
        Vector3 toPlayer = player.transform.position - hit.position;
        toPlayer.y = 0f;
        Quaternion facing = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;

        EnemyAi enemy = Instantiate(enemyPrefab, hit.position, facing, enemyContainer);
        enemy.player = player;
        alive.Add(enemy);
    }

    private Transform PickSpawnPoint()
    {
        // Prefer points that aren't right next to the player
        candidates.Clear();
        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            float distance = Vector3.Distance(point.position, player.transform.position);
            if (distance >= minDistanceFromPlayer)
                candidates.Add(point);
        }

        // If the player is close to every point, any point will do
        if (candidates.Count == 0)
        {
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                    candidates.Add(point);
            }
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    // Draws the spawn points in the Scene view while the spawner is selected
    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null)
            return;

        Gizmos.color = Color.magenta;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                Gizmos.DrawWireSphere(point.position, 0.5f);
        }
    }
}