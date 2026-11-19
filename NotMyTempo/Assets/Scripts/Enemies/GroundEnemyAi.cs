using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

/// <summary>
/// Base for enemies that walk on the NavMesh (Crawler, Tanky, Roomba).
/// Chases the player, stops to attack when in range, and can optionally
/// climb over walls and ledges that are in the way.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class GroundEnemyAi : EnemyAi
{
    [Header("Climbing")]
    [Tooltip("Can this enemy climb up walls and ledges to reach the player?")]
    [SerializeField] protected bool _canClimb = true;

    [Tooltip("Tallest wall this enemy can climb, in meters")]
    [FormerlySerializedAs("maxClimbHeight")]
    [Min(0f)]
    [SerializeField] private float _maxClimbHeight = 2.2f;

    [Tooltip("How far ahead the enemy looks for a wall to climb, in meters")]
    [FormerlySerializedAs("climbReach")]
    [Min(0f)]
    [SerializeField] private float _climbReach = 2.2f;

    [Tooltip("How fast the enemy climbs, in meters per second")]
    [FormerlySerializedAs("climbSpeed")]
    [Min(0.1f)]
    [SerializeField] private float _climbSpeed = 4f;

    private const float RepathInterval = 0.25f; // SetDestination is expensive, so only do it 4 times a second
    private const float ClimbClearance = 0.1f;

    private float _nextRepathTime;
    private bool _isClimbing;
    private Vector3 _climbLift;
    private Vector3 _climbAcross;
    private Vector3 _climbLanding;

    protected NavMeshAgent Agent { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Agent = GetComponent<NavMeshAgent>();
        Agent.stoppingDistance = _attackRange * 0.8f;

        // Spread enemies' repath timing out so they don't all path on the same frame
        _nextRepathTime = Time.time + Random.Range(0f, RepathInterval);
    }

    protected override void UpdateBehavior()
    {
        if (!Agent.enabled || !Agent.isOnNavMesh)
            return;

        if (_isClimbing)
        {
            UpdateClimb();
            return;
        }

        if (Time.time < _nextRepathTime)
            return;

        _nextRepathTime = Time.time + RepathInterval;

        bool canReachPlayer = DistanceToPlayer() <= _attackRange && HasLineOfSightToPlayer();
        if (canReachPlayer)
        {
            Agent.isStopped = true;
            TryAttack();
            return;
        }

        bool startedClimbing = _canClimb && TryStartClimb();
        if (!startedClimbing)
        {
            Agent.isStopped = false;
            Agent.SetDestination(Player.transform.position);
        }
    }

    protected override void UpdateWithoutTarget()
    {
        // Finish any climb in progress so we don't freeze halfway up a wall
        if (_isClimbing)
        {
            UpdateClimb();
            return;
        }

        if (Agent.enabled && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
        }
    }

    protected override void Die(bool countAsKill = true)
    {
        if (IsDead)
            return;

        Agent.enabled = false;
        base.Die(countAsKill);
    }

    private void OnDisable()
    {
        if (!_isClimbing)
            return;

        Agent.updatePosition = true;
        _isClimbing = false;
    }

    /// <summary>
    /// Looks for a climbable wall between us and the player. If there is one with
    /// NavMesh on top and room to climb, starts climbing and returns true.
    /// </summary>
    private bool TryStartClimb()
    {
        bool isAbleToClimb = _maxClimbHeight > 0f && _climbReach > 0f &&
                             Agent.updatePosition && !Agent.isOnOffMeshLink;
        if (!isAbleToClimb)
            return false;

        Vector3 feet = transform.position - Vector3.up * Agent.baseOffset;
        Vector3 direction = Player.transform.position - feet;
        direction.y = 0f;

        float distanceToPlayer = direction.magnitude;
        if (distanceToPlayer < 0.01f)
            return false;

        direction /= distanceToPlayer;

        // 1. Is there a wall in front of us?
        if (!Physics.Raycast(feet + Vector3.up * 0.15f, direction, out RaycastHit wallHit,
                Mathf.Min(_climbReach, distanceToPlayer), _walls, QueryTriggerInteraction.Ignore))
            return false;

        // 2. Is the top of that wall low enough and flat enough to stand on?
        Vector3 topProbe = wallHit.point + direction * (Agent.radius + 0.2f);
        topProbe.y = feet.y + _maxClimbHeight + ClimbClearance;

        bool hasStandableTop = Physics.Raycast(topProbe, Vector3.down, out RaycastHit topHit,
                                   _maxClimbHeight, _walls, QueryTriggerInteraction.Ignore) &&
                               topHit.collider == wallHit.collider &&
                               topHit.normal.y >= 0.7f;
        if (!hasStandableTop)
            return false;

        // 3. Is there NavMesh up there to land on?
        var filter = new NavMeshQueryFilter
        {
            agentTypeID = Agent.agentTypeID,
            areaMask = Agent.areaMask
        };

        if (!NavMesh.SamplePosition(topHit.point, out NavMeshHit landing, 0.2f, filter) ||
            Mathf.Abs(landing.position.y - topHit.point.y) > ClimbClearance)
            return false;

        // 4. Is there room for our body along the whole climb (up, across, down)?
        Vector3 raisedFeet = feet;
        raisedFeet.y = Mathf.Max(topHit.point.y, landing.position.y) + ClimbClearance;

        Vector3 acrossFeet = landing.position;
        acrossFeet.y = raisedFeet.y;

        if (!HasClimbClearance(feet, raisedFeet) ||
            !HasClimbClearance(raisedFeet, acrossFeet) ||
            !HasClimbClearance(acrossFeet, landing.position))
            return false;

        Vector3 pivotOffset = Vector3.up * Agent.baseOffset;
        _climbLift = raisedFeet + pivotOffset;
        _climbAcross = acrossFeet + pivotOffset;
        _climbLanding = landing.position;

        // Take movement away from the agent while we climb by hand
        Agent.isStopped = true;
        Agent.updatePosition = false;
        _isClimbing = true;
        return true;
    }

    private bool HasClimbClearance(Vector3 from, Vector3 to)
    {
        float radius = Mathf.Min(Agent.radius, Agent.height * 0.5f);
        Vector3 bottom = from + Vector3.up * (radius + 0.05f);
        Vector3 top = bottom + Vector3.up * (Agent.height - 2f * radius);
        Vector3 movement = to - from;

        if (Physics.CheckCapsule(bottom + movement, top + movement, radius,
                _walls, QueryTriggerInteraction.Ignore))
            return false;

        return movement.sqrMagnitude < 0.0001f ||
               !Physics.CapsuleCast(bottom, top, radius, movement.normalized,
                   movement.magnitude, _walls, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Moves straight up, then across the top, then hands control back to the agent.</summary>
    private void UpdateClimb()
    {
        Vector3 target = transform.position.y < _climbLift.y ? _climbLift : _climbAcross;
        transform.position = Vector3.MoveTowards(transform.position, target, _climbSpeed * Time.deltaTime);

        if (transform.position != _climbAcross)
            return;

        bool hasLanded = Agent.Warp(_climbLanding);
        Agent.updatePosition = true;
        _isClimbing = false;

        if (hasLanded)
        {
            Agent.ResetPath();
        }

        _nextRepathTime = Time.time + RepathInterval;
    }
}
