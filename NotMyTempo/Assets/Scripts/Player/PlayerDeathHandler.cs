using UnityEngine;

/// <summary>
/// When the player dies: switch off movement, looking, input and shooting,
/// and free the cursor so the death screen can be used.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerDeathHandler : MonoBehaviour
{
    [Tooltip("Found automatically if left empty")]
    [SerializeField] private PlayerMotor motor;
    [Tooltip("Found automatically if left empty")]
    [SerializeField] private PlayerLook look;
    [Tooltip("Found automatically if left empty")]
    [SerializeField] private PlayerInput input;
    [Tooltip("Found automatically if left empty (searches the Player's children)")]
    [SerializeField] private RaycastShoot gun;

    private PlayerHealth health;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();

        if (motor == null) motor = GetComponent<PlayerMotor>();
        if (look == null) look = GetComponent<PlayerLook>();
        if (input == null) input = GetComponent<PlayerInput>();
        if (gun == null) gun = GetComponentInChildren<RaycastShoot>();
    }

    private void OnEnable()
    {
        health.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnDied -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (motor != null) motor.enabled = false;
        if (look != null) look.enabled = false;
        if (input != null) input.enabled = false;
        if (gun != null) gun.enabled = false;

        // PlayerLook locked the cursor; nothing unlocks it when PlayerLook turns off
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}