using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAi : MonoBehaviour
{
    public PlayerHealth player;
    public float health = 50f;
    public float attackRange = 1.8f;
    public float damage = 10f;
    public float attackCooldown = 1.2f;

    // Select wall/environment layers only.
    public LayerMask walls;

    private NavMeshAgent agent;
    private float nextAttack;
    private float nextUpdate;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = attackRange * 0.8f;

        // Spread path updates across different frames.
        nextUpdate = Time.time + UnityEngine.Random.Range(0f, 0.25f);
    }

    void Update()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

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
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.transform.position);
        }
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

        if (health <= 0f)
        {
            agent.enabled = false;
            Destroy(gameObject);
        }
    }
}
