using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

/// <summary>
/// Keeps enemies coming. Every few seconds it spawns one at a random spawn point,
/// as long as fewer than the max are alive. Picks which enemy type to spawn
/// by weight. Ground enemies are snapped onto the NavMesh; flying enemies
/// appear above the spawn point.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    /// <summary>One enemy type the spawner can pick, and how often.</summary>
    [Serializable]
    public class SpawnOption
    {
        [Tooltip("An enemy prefab from the Project window")]
        public EnemyAi Prefab;

        [Tooltip("How often this one is picked compared to the others. 3 = three times as often as 1")]
        [Min(0f)]
        public float Weight = 1f;
    }

    [Header("What to spawn")]
    [Tooltip("Every enemy type this spawner can make")]
    [SerializeField] private SpawnOption[] _spawnOptions;

    [Tooltip("Where enemies can appear")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Pacing")]
    [Tooltip("Seconds before the first enemy appears")]
    [SerializeField] private float startDelay = 2f;
    [Tooltip("Seconds between spawns")]
    [SerializeField] private float spawnInterval = 2.5f;
    [Tooltip("Never more than this many enemies alive at once")]
    [SerializeField] private int maxAlive = 8;

    [Header("Ramp up")]
    [Tooltip("Seconds per difficulty stage")]
    [SerializeField] private float rampEvery = 32f;
    [Tooltip("Each stage multiplies the spawn interval by this. 0.85 = 15% faster")]
    [Range(0.5f, 1f)]
    [SerializeField] private float intervalMultiplier = 0.85f;
    [Tooltip("Spawning never gets faster than this, in seconds")]
    [SerializeField] private float minSpawnInterval = 0.6f;
    [Tooltip("Each stage allows this many more enemies alive at once")]
    [SerializeField] private int maxAliveIncrease = 2;
    [Tooltip("Hard cap on enemies alive, however long the run lasts")]
    [SerializeField] private int maxAliveCap = 25;

    [Header("Placement")]
    [Tooltip("Skip spawn points closer than this to the player, in meters")]
    [SerializeField] private float minDistanceFromPlayer = 10f;
    [Tooltip("How far from a spawn point to look for the NavMesh, in meters")]
    [SerializeField] private float navMeshSearchRadius = 3f;
    [Tooltip("How high above the spawn point flying enemies appear, in meters")]
    [SerializeField] private float _flyingSpawnHeight = 4f;

    private readonly List<EnemyAi> alive = new List<EnemyAi>();
    private readonly List<Transform> candidates = new List<Transform>();
    private PlayerHealth player;
    private Transform enemyContainer;
    private float nextSpawnTime;
    private float startTime;

    public int AliveCount => alive.Count; // the HUD can read this later
    public int Stage { get; private set; } // the threshold that's crossed to increase difficulty
    public float CurrentSpawnInterval => Mathf.Max(minSpawnInterval, spawnInterval * Mathf.Pow(intervalMultiplier, Stage));
    public int CurrentMaxAlive => Mathf.Min(maxAliveCap, maxAlive + Stage * maxAliveIncrease);

    private void Start()
    {
        if (!HasAnySpawnOption() || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("EnemySpawner needs at least one enemy prefab (with a weight above 0) and at least one spawn point.", this);
            enabled = false;
            return;
        }

        player = FindFirstObjectByType<PlayerHealth>();
        enemyContainer = new GameObject("Spawned Enemies").transform;
        nextSpawnTime = Time.time + startDelay;
        startTime = Time.time; // sets start time
    }

    private void Update()
    {
        if (player == null || player.IsDead) // if we're dead, don't spawn things
            return;

        UpdateStage();

        // A destroyed enemy counts as "null" in Unity, so this clears out the dead ones
        alive.RemoveAll(enemy => enemy == null);

        if (Time.time < nextSpawnTime || alive.Count >= CurrentMaxAlive)
            return;

        nextSpawnTime = Time.time + CurrentSpawnInterval; // iterate the spawn time
        TrySpawn(); // try to spawn
    }

    private void TrySpawn()
    {
        EnemyAi prefab = PickEnemyPrefab();
        Transform point = PickSpawnPoint();

        // Only ground enemies need to stand on the NavMesh
        bool isGroundEnemy = prefab is GroundEnemyAi;
        Vector3 spawnPosition;

        if (isGroundEnemy)
        {
            // Find the closest spot on the NavMesh to this spawn point
            if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas))
            {
                Debug.LogWarning("Spawn point '" + point.name + "' isn't near the NavMesh. " +
                                 "Move it closer to the floor or raise Nav Mesh Search Radius.", point);
                return;
            }

            spawnPosition = hit.position;
        }
        else
        {
            spawnPosition = point.position + Vector3.up * _flyingSpawnHeight;
        }

        // Face the player so enemies don't spawn looking at a wall
        Vector3 toPlayer = player.transform.position - spawnPosition;
        toPlayer.y = 0f;
        Quaternion facing = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;

        EnemyAi enemy = Instantiate(prefab, spawnPosition, facing, enemyContainer);
        enemy.Player = player;
        alive.Add(enemy);
    }

    private bool HasAnySpawnOption()
    {
        if (_spawnOptions == null)
            return false;

        foreach (SpawnOption option in _spawnOptions)
        {
            if (option != null && option.Prefab != null && option.Weight > 0f)
                return true;
        }

        return false;
    }

    /// <summary>Weighted random pick: an option with weight 2 is twice as likely as one with weight 1.</summary>
    private EnemyAi PickEnemyPrefab()
    {
        float totalWeight = 0f;
        foreach (SpawnOption option in _spawnOptions)
        {
            if (option != null && option.Prefab != null)
                totalWeight += option.Weight;
        }

        float roll = Random.Range(0f, totalWeight);
        EnemyAi lastValidPrefab = null;

        foreach (SpawnOption option in _spawnOptions)
        {
            if (option == null || option.Prefab == null || option.Weight <= 0f)
                continue;

            lastValidPrefab = option.Prefab;
            roll -= option.Weight;
            if (roll <= 0f)
                return option.Prefab;
        }

        return lastValidPrefab; // only reached through float rounding
    }

    private void UpdateStage()
    {
        int stage = Mathf.FloorToInt((Time.time - startTime) / Mathf.Max(1f, rampEvery));
        if (stage == Stage)
            return;

        Stage = stage;
        Debug.Log("Spawner stage " + Stage + ": one enemy every " +
                  CurrentSpawnInterval.ToString("0.00") + "s, up to " + CurrentMaxAlive + " alive");
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