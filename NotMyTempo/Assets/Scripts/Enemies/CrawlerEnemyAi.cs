/// <summary>
/// The most common enemy. Swarms the player at medium speed, climbs walls
/// to reach them, and hits them in melee.
/// Tune speed on the NavMeshAgent and health/damage in the Stats section.
/// </summary>
public class CrawlerEnemyAi : GroundEnemyAi
{
    protected override void PerformAttack()
    {
        Player.TakeDamage(_attackDamage);
    }
}
