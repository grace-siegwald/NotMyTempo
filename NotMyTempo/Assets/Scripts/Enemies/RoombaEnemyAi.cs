using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small, fast disk that rushes the player and explodes.
/// Always dies in one hit. It explodes whenever it dies, whether it reached the
/// player or got shot, and the explosion damages anything nearby, including other enemies.
/// </summary>
public class RoombaEnemyAi : GroundEnemyAi
{
    [Header("Explosion")]
    [Tooltip("How far the explosion reaches, in meters")]
    [SerializeField] private float _explosionRadius = 3f;

    [Tooltip("Damage dealt to everything inside the explosion radius")]
    [SerializeField] private float _explosionDamage = 25f;

    [Tooltip("Layers the explosion can hit. Include the player's and the enemies' layers")]
    [SerializeField] private LayerMask _explosionLayers = ~0;

    [Tooltip("Optional effect spawned where the Roomba explodes")]
    [SerializeField] private GameObject _explosionEffectPrefab;

    public override void TakeDamage(float amount)
    {
        if (amount <= 0f)
            return;

        // One shot always kills, however much damage the shot actually did
        base.TakeDamage(Mathf.Max(amount, CurrentHealth));
    }

    /// <summary>Reaching the player means blowing up. It doesn't count as a player kill.</summary>
    protected override void PerformAttack()
    {
        Die(countAsKill: false);
    }

    protected override void Die(bool countAsKill = true)
    {
        if (IsDead)
            return;

        // base.Die turns our colliders off first, so the explosion can't hit us
        base.Die(countAsKill);
        Explode();
    }

    private void Explode()
    {
        if (_explosionEffectPrefab != null)
        {
            Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, _explosionRadius,
            _explosionLayers, QueryTriggerInteraction.Ignore);

        // One target can have several colliders, so make sure each only gets hit once
        var damagedTargets = new HashSet<IDamageable>();

        foreach (Collider hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            bool isNewTarget = target != null && damagedTargets.Add(target);

            if (isNewTarget)
            {
                target.TakeDamage(_explosionDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
