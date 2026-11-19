using UnityEngine;

/// <summary>
/// Hovers above the player and circles them from a medium distance, shooting projectiles.
/// Each attack is either one aimed shot or a spread volley.
/// Doesn't use the NavMesh: it flies directly and turns around when a wall is in the way.
/// </summary>
public class FlyingEnemyAi : EnemyAi
{
    [Header("Hovering")]
    [Tooltip("How fast it flies, in meters per second")]
    [SerializeField] private float _moveSpeed = 6f;

    [Tooltip("How far from the player it circles, in meters")]
    [SerializeField] private float _orbitRadius = 10f;

    [Tooltip("How high above the player it hovers, in meters")]
    [SerializeField] private float _hoverHeight = 4f;

    [Tooltip("How fast it circles the player, in degrees per second")]
    [SerializeField] private float _orbitSpeed = 30f;

    [Tooltip("Roughly the size of the body, used to avoid flying into walls")]
    [SerializeField] private float _bodyRadius = 0.5f;

    [Tooltip("How quickly it turns to face the player")]
    [SerializeField] private float _turnSpeed = 5f;

    [Header("Shooting")]
    [Tooltip("The projectile prefab it fires (needs an EnemyProjectile component)")]
    [SerializeField] private EnemyProjectile _projectilePrefab;

    [Tooltip("Projectile speed, in meters per second")]
    [SerializeField] private float _projectileSpeed = 15f;

    [Tooltip("Chance that an attack is a spread volley instead of a single aimed shot. 0 = never, 1 = always")]
    [Range(0f, 1f)]
    [SerializeField] private float _volleyChance = 0.3f;

    [Tooltip("How many projectiles are in a volley")]
    [Min(1)]
    [SerializeField] private int _volleyProjectileCount = 5;

    [Tooltip("How wide the volley fans out, in degrees")]
    [SerializeField] private float _volleySpreadAngle = 40f;

    private float _orbitAngle;
    private int _orbitDirection;

    protected override float EyeHeight => 0f; // the body is the eye

    protected override void Awake()
    {
        base.Awake();

        // Start at a random spot on the circle, going a random way round, so a group spreads out
        _orbitAngle = Random.Range(0f, 360f);
        _orbitDirection = Random.value < 0.5f ? 1 : -1;

        if (_projectilePrefab == null)
        {
            Debug.LogWarning(name + " has no projectile prefab, so it can't shoot.", this);
        }
    }

    private void OnValidate()
    {
        if (_attackRange < _orbitRadius)
        {
            Debug.LogWarning(name + ": Attack Range (" + _attackRange + ") is shorter than Orbit Radius (" +
                             _orbitRadius + "), so it will circle without ever shooting.", this);
        }
    }

    protected override void UpdateBehavior()
    {
        CirclePlayer();
        FacePlayer();

        bool canShootPlayer = DistanceToPlayer() <= _attackRange && HasLineOfSightToPlayer();
        if (canShootPlayer)
        {
            TryAttack();
        }
    }

    protected override void PerformAttack()
    {
        if (_projectilePrefab == null)
            return;

        bool isVolley = Random.value < _volleyChance;
        if (isVolley)
        {
            FireVolley();
        }
        else
        {
            FireProjectile(DirectionToPlayer());
        }
    }

    private void CirclePlayer()
    {
        _orbitAngle += _orbitSpeed * _orbitDirection * Time.deltaTime;

        Vector3 offsetFromPlayer = Quaternion.Euler(0f, _orbitAngle, 0f) * Vector3.forward * _orbitRadius;
        Vector3 orbitPoint = Player.transform.position + offsetFromPlayer + Vector3.up * _hoverHeight;

        Vector3 toOrbitPoint = orbitPoint - transform.position;
        float distanceToOrbitPoint = toOrbitPoint.magnitude;
        if (distanceToOrbitPoint < 0.01f)
            return;

        float step = Mathf.Min(_moveSpeed * Time.deltaTime, distanceToOrbitPoint);

        // Don't fly through walls. If one is in the way, go round the circle the other way.
        bool isPathBlocked = Physics.SphereCast(transform.position, _bodyRadius, toOrbitPoint / distanceToOrbitPoint,
            out _, step, _walls, QueryTriggerInteraction.Ignore);
        if (isPathBlocked)
        {
            _orbitDirection = -_orbitDirection;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, orbitPoint, step);
    }

    private void FacePlayer()
    {
        Vector3 toPlayer = Player.transform.position - transform.position;
        if (toPlayer.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(toPlayer), _turnSpeed * Time.deltaTime);
    }

    private Vector3 DirectionToPlayer()
    {
        Vector3 playerChest = Player.transform.position + Vector3.up;
        return (playerChest - transform.position).normalized;
    }

    private void FireVolley()
    {
        Vector3 aim = DirectionToPlayer();

        for (int i = 0; i < _volleyProjectileCount; i++)
        {
            // Spread the shots evenly from one edge of the fan to the other
            float fanPosition = _volleyProjectileCount == 1 ? 0.5f : i / (float)(_volleyProjectileCount - 1);
            float angle = Mathf.Lerp(-_volleySpreadAngle * 0.5f, _volleySpreadAngle * 0.5f, fanPosition);
            FireProjectile(Quaternion.AngleAxis(angle, Vector3.up) * aim);
        }
    }

    private void FireProjectile(Vector3 direction)
    {
        // Spawn just outside our own body so the projectile doesn't start inside us
        Vector3 spawnPosition = transform.position + direction * (_bodyRadius + 0.2f);

        EnemyProjectile projectile = Instantiate(_projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
        projectile.Launch(direction * _projectileSpeed, _attackDamage);
    }
}