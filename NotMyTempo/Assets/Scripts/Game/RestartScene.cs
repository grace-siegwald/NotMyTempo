using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class RestartScene : MonoBehaviour
{
    [Tooltip("\"Player/Restart\" from the InputSystem_Actions asset")]
    [SerializeField] private InputActionReference restartAction;

    [Tooltip("Found automatically if left empty")]
    [SerializeField] private PlayerHealth playerHealth;

    private bool canRestart;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        // PlayerInput gets switched off on death, so this script enables its own action
        restartAction.action.Enable();

        if (playerHealth != null)
            playerHealth.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        canRestart = true;
    }

    private void Update()
    {
        if (canRestart && restartAction.action.WasPressedThisFrame())
            Restart();
    }

    /// <summary>Public so a "Restart" button on the death panel can call it too.</summary>
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
