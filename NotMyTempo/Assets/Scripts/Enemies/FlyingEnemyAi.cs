using UnityEngine;

public class FlyingEnemyAi : MonoBehaviour
{
    public PlayerHealth player;

    public float speed = 3f;
    public float heightAbovePlayer = 1f;

    public float health = 30f;
    public float attackRange = 2f;
    public float damage = 10f;
    public float attackCooldown = 1f;

    public LayerMask walls;
    private HitFlash hitFlash;
    private UnityEngine.AI.NavMeshAgent agent;

    private float nextAttackTime;

    void Update()
    {
        if (player == null || player.IsDead)
            return;

        Vector3 target = player.transform.position
            + Vector3.up * heightAbovePlayer;

        Vector3 direction = target - transform.position;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                5f * Time.deltaTime
            );
        }

        if (Physics.Linecast(
            transform.position,
            target,
            walls,
            QueryTriggerInteraction.Ignore))
        {
            return;
        }

        float distance = direction.magnitude;

        if (distance > attackRange)
        {
         
            float step = Mathf.Min(
                speed * Time.deltaTime,
                distance - attackRange
            );

            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                step
            );
        }
        else if (Time.time >= nextAttackTime)
        {
            player.TakeDamage(damage);
            nextAttackTime = Time.time + attackCooldown;
        }
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