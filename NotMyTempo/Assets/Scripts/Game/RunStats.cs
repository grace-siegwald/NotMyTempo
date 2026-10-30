using UnityEngine;

/// <summary>
/// Tracks the current run: enemies killed and how long the player has survived.
/// The HUD and death screen read from this.
/// </summary>
public class RunStats : MonoBehaviour
{
    public int Kills { get; private set; }
    public float TimeSurvived { get; private set; }

    private PlayerHealth player;

    private void OnEnable() // on enable, sign HandleEnemyKilled up to the event
    {
        EnemyEvents.EnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable() // on disable, un-sign from the event
    {
        EnemyEvents.EnemyKilled -= HandleEnemyKilled;
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerHealth>(); // get the player
    }

    private void Update()
    {
        // The clock stops when the player dies
        if (player != null && !player.IsDead)
            TimeSurvived += Time.deltaTime;
    }

    private void HandleEnemyKilled(GameObject enemy)
    {
        Kills++;
        Debug.Log("Kills: " + Kills + "  (" + TimeSurvived.ToString("0.0") + "s survived)");
    }
}
