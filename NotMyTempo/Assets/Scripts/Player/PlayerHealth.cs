using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        Debug.Log("Player health: " + CurrentHealth);

        if (IsDead)
            Die();
    }

    void Die()
    {
        Debug.Log("Player died!");
        // Gonna add a game over screen or restart here or something like that
        //.
    }
}
