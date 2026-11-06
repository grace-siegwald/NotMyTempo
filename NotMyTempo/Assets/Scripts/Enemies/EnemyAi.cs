using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAi : MonoBehaviour, IDamageable
{
    [Header("Climbing")]
    [Min(0f)] public float maxClimbHeight = 2.2f;
    [Min(0f)] public float climbReach = 2.2f;
    [Min(0.1f)] public float climbSpeed = 4f;

    private bool isClimbing;
    private Vector3 climbLift;
    private Vector3 climbAcross;
    private Vector3 climbLanding;

    public PlayerHealth player;
    public virtual float health { get; set; } = 50f;
    public virtual float attackRange { get; set; } = 1.8f;
    public virtual float damage { get; set; } = 10f;
    public virtual float attackCooldown { get; set; } = 1.2f;

    public LayerMask walls;

    private NavMeshAgent agent;
    private float nextAttack;
    private float nextUpdate;
    private HitFlash hitFlash;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hitFlash = GetComponent<HitFlash>();
        agent.stoppingDistance = attackRange * 0.8f;

     
        nextUpdate = Time.time + UnityEngine.Random.Range(0f, 0.25f);
        if (player == null) { player = FindFirstObjectByType<PlayerHealth>(); }
    }

    void Update()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;
        if (isClimbing)
        {
            UpdateClimb();
            return;
        }
        if (player == null || player.IsDead)
        {
            agent.isStopped = true;
            return;
        }

        if (Time.time < nextUpdate)
            return;

        nextUpdate = Time.time + 0.25f;

        float distance = Vector3.Distance(transform.position,
                                          player.transform.position);

        bool wallInWay = Physics.Linecast(
            transform.position + Vector3.up,
            player.transform.position + Vector3.up,
            walls, QueryTriggerInteraction.Ignore);

        if (distance <= attackRange && !wallInWay)
        {
            agent.isStopped = true;
            Attack();
        }
        else if (!TryStartClimb())
        {
            agent.isStopped = false;
            agent.SetDestination(player.transform.position);
        }
    }

    bool TryStartClimb()
    {
        if (maxClimbHeight <= 0f || climbReach <= 0f ||
            !agent.updatePosition || agent.isOnOffMeshLink)
            return false;

        const float clearance = 0.1f;
        Vector3 feet = transform.position - Vector3.up * agent.baseOffset;
        Vector3 direction = player.transform.position - feet;
        direction.y = 0f;

        float distanceToPlayer = direction.magnitude;
        if (distanceToPlayer < 0.01f)
            return false;

        direction /= distanceToPlayer;

        if (!Physics.Raycast(feet + Vector3.up * 0.15f, direction,
            out RaycastHit wallHit, Mathf.Min(climbReach, distanceToPlayer),
            walls, QueryTriggerInteraction.Ignore))
            return false;

       
        Vector3 topProbe = wallHit.point + direction * (agent.radius + 0.2f);
        topProbe.y = feet.y + maxClimbHeight + clearance;

        if (!Physics.Raycast(topProbe, Vector3.down, out RaycastHit topHit,
            maxClimbHeight, walls, QueryTriggerInteraction.Ignore) ||
            topHit.collider != wallHit.collider || topHit.normal.y < 0.7f)
            return false;

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        if (!NavMesh.SamplePosition(topHit.point, out NavMeshHit landing, 0.2f, filter) ||
            Mathf.Abs(landing.position.y - topHit.point.y) > clearance)
            return false;

        Vector3 raisedFeet = feet;
        raisedFeet.y = Mathf.Max(topHit.point.y, landing.position.y) + clearance;

        Vector3 acrossFeet = landing.position;
        acrossFeet.y = raisedFeet.y;

        if (!HasClimbClearance(feet, raisedFeet) ||
            !HasClimbClearance(raisedFeet, acrossFeet) ||
            !HasClimbClearance(acrossFeet, landing.position))
            return false;

        Vector3 pivotOffset = Vector3.up * agent.baseOffset;
        climbLift = raisedFeet + pivotOffset;
        climbAcross = acrossFeet + pivotOffset;
        climbLanding = landing.position;

        agent.isStopped = true;
        agent.updatePosition = false;
        isClimbing = true;
        return true;
    }

    bool HasClimbClearance(Vector3 from, Vector3 to)
    {
        float radius = Mathf.Min(agent.radius, agent.height * 0.5f);
        Vector3 bottom = from + Vector3.up * (radius + 0.05f);
        Vector3 top = bottom + Vector3.up * (agent.height - 2f * radius);
        Vector3 movement = to - from;

        if (Physics.CheckCapsule(bottom + movement, top + movement, radius,
            walls, QueryTriggerInteraction.Ignore))
            return false;

        return movement.sqrMagnitude < 0.0001f ||
            !Physics.CapsuleCast(bottom, top, radius, movement.normalized,
                movement.magnitude, walls, QueryTriggerInteraction.Ignore);
    }

    void UpdateClimb()
    {
        Vector3 target = transform.position.y < climbLift.y ? climbLift : climbAcross;
        transform.position = Vector3.MoveTowards(
            transform.position, target, climbSpeed * Time.deltaTime);

        if (transform.position != climbAcross)
            return;

        bool landed = agent.Warp(climbLanding);
        agent.updatePosition = true;
        isClimbing = false;

        if (landed)
            agent.ResetPath();

        nextUpdate = Time.time + 0.25f;
    }

    void OnDisable()
    {
        if (!isClimbing)
            return;

        agent.updatePosition = true;
        isClimbing = false;
    }

    void Attack()
    {
        if (Time.time < nextAttack)
            return;

        player.TakeDamage(damage);
        nextAttack = Time.time + Mathf.Max(0.1f, attackCooldown);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || health <= 0f) 
            return;

        health -= amount;

        if (hitFlash != null) 
            hitFlash.Flash();

        if (health <= 0f)
        {
            EnemyEvents.ReportKill(gameObject); 
            agent.enabled = false;
            GetComponent<Collider>().enabled = false;
            Destroy(gameObject, 0.08f); 
        }
    }
}
