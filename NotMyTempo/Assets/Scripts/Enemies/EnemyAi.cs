using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Shared base for every enemy type. Handles health, getting hit, dying,
/// finding the player, line of sight and attack cooldowns.
/// Each enemy type inherits from this and fills in how it moves (UpdateBehavior)
/// and what its attack does (PerformAttack).
/// </summary>
public abstract class EnemyAi : MonoBehaviour, IDamageable
{
    // FormerlySerializedAs lets Unity read the values already saved in the prefabs
    // under the old names, so nothing has to be re-entered by hand.
    [Header("Stats")]
    [Tooltip("How much damage this enemy can take before dying")]
    [FormerlySerializedAs("health")]
    [SerializeField] protected float _maxHealth = 50f;

    [Tooltip("Damage dealt to the player by one attack")]
    [FormerlySerializedAs("damage")]
    [SerializeField] protected float _attackDamage = 10f;

    [Tooltip("How close the player has to be before this enemy attacks, in meters")]
    [FormerlySerializedAs("attackRange")]
    [SerializeField] protected float _attackRange = 1.8f;

    [Tooltip("Seconds between attacks")]
    [FormerlySerializedAs("attackCooldown")]
    [Min(0.1f)]
    [SerializeField] protected float _attackCooldown = 1.2f;

    [Header("World")]
    [Tooltip("Layers that block movement and line of sight (walls, floors, props)")]
    [FormerlySerializedAs("walls")]
    [SerializeField] protected LayerMask _walls;

    private const float DestroyDelay = 0.08f; // long enough for the hit flash to show

    private HitFlash _hitFlash;
    private float _nextAttackTime;

    /// <summary>The player this enemy is after. The spawner sets it; otherwise it's found in Awake.</summary>
    public PlayerHealth Player { get; set; }
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    /// <summary>How high above the pivot this enemy "looks" from for line of sight checks.</summary>
    protected virtual float EyeHeight => 1f;
    protected bool IsAttackReady => Time.time >= _nextAttackTime;

    protected virtual void Awake()
    {
        CurrentHealth = _maxHealth;
        _hitFlash = GetComponent<HitFlash>();

        if (Player == null)
        {
            Player = FindFirstObjectByType<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (IsDead)
            return;

        bool hasLivingTarget = Player != null && !Player.IsDead;
        if (hasLivingTarget)
        {
            UpdateBehavior();
        }
        else
        {
            UpdateWithoutTarget();
        }
    }

    /// <summary>Runs every frame while the player is alive. Move, aim and call TryAttack here.</summary>
    protected abstract void UpdateBehavior();

    /// <summary>What happens to the enemy's attack once TryAttack decides it's allowed.</summary>
    protected abstract void PerformAttack();

    /// <summary>Runs every frame while there's no living player. Does nothing unless a child overrides it.</summary>
    protected virtual void UpdateWithoutTarget()
    {
    }

    /// <summary>Attacks if the cooldown has finished. Returns true if it attacked.</summary>
    protected bool TryAttack()
    {
        if (!IsAttackReady)
            return false;

        PerformAttack();
        _nextAttackTime = Time.time + _attackCooldown;
        return true;
    }

    protected float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, Player.transform.position);
    }

    /// <summary>True if no wall is between this enemy's eyes and the player's chest.</summary>
    protected bool HasLineOfSightToPlayer()
    {
        Vector3 eyes = transform.position + Vector3.up * EyeHeight;
        Vector3 playerChest = Player.transform.position + Vector3.up;
        return !Physics.Linecast(eyes, playerChest, _walls, QueryTriggerInteraction.Ignore);
    }

    public virtual void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        CurrentHealth -= amount;

        if (_hitFlash != null)
        {
            _hitFlash.Flash();
        }

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Kills this enemy. Pass countAsKill = false when the enemy removes itself
    /// (like a Roomba blowing up on the player) so it doesn't count toward the player's kills.
    /// </summary>
    protected virtual void Die(bool countAsKill = true)
    {
        if (IsDead)
            return;

        IsDead = true;

        if (countAsKill)
        {
            EnemyEvents.ReportKill(gameObject);
        }

        foreach (Collider bodyCollider in GetComponentsInChildren<Collider>())
        {
            bodyCollider.enabled = false;
        }

        Destroy(gameObject, DestroyDelay);
    }
}
