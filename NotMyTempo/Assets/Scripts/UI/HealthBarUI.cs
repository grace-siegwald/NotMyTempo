using UnityEngine;

/// <summary>
/// Shrinks the health bar's fill to match the player's health.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Tooltip("The bar that shrinks. Its Pivot X must be 0 so it shrinks toward the left")]
    [SerializeField] private RectTransform fill;

    [Tooltip("Found automatically if left empty")]
    [SerializeField] private PlayerHealth playerHealth;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateBar;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateBar;
    }

    private void UpdateBar(float current, float max)
    {
        // Same as this: float fraction = max > 0f ? current / max : 0f;
        float fraction;
        if (max > 0f)
        {
            fraction = current / max; // turns health into a number from 0 to 1
        }
        else
        {
            fraction = 0f;
        }
        fill.localScale = new Vector3(fraction, 1f, 1f);
    }
}