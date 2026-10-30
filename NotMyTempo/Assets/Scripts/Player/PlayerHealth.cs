using System;
using UnityEngine;

/// <summary>
/// The player's health. Other scripts listen to its events
/// instead of checking it every frame.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;
    public event Action<float, float> OnHealthChanged; // Fired whenever health changes. Passes (current, max).
    public event Action OnDied; // Fired once, the moment the player dies.

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        // Tell listeners the starting health, so the health bar starts full
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player died!");
        OnDied?.Invoke();
    }
}
