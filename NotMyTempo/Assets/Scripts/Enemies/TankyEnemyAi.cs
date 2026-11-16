/// <summary>
/// A bigger, slower Crawler with a lot more health.
/// It inherits all of the Crawler's chasing, climbing and melee behavior.
/// The differences for now are just numbers on the prefab: lower NavMeshAgent
/// speed, higher Max Health, larger model. When the Tanky gets its own move
/// (a ground slam, a charge, ignoring knockback...), override PerformAttack
/// or UpdateBehavior here.
/// </summary>
public class TankyEnemyAi : CrawlerEnemyAi
{
}
