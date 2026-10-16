using UnityEngine;
using UnityEngine.InputSystem;

// Turns look input into rotation. Yaw (left/right) rotates the whole Player; pitch(up / down)
// rotates only the CameraRoot, so the Player's forward always stays level.
// Esc frees the cursor; clicking the Game view captures it again.

[DefaultExecutionOrder(-50)] // execute this scripts Update after the player input is read :)
[RequireComponent(typeof(PlayerInput))] // got auto recomended for me, this makes sense tho! 
public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform _cameraRoot; // the location of the camera on the player's body 
    [SerializeField] private float _maxPitch = 89f; // how far up and down you can look

    private PlayerInput _inputReader;
    private float _yaw;
    private float _pitch;

    private static void SetCursorLocked(bool isLocked) // For when we need to lock the cursor while looking, don't want thang swingin around !
    {
        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
        Cursor.visible = !isLocked; // cursor is visible when not locked :)
    }
    private void Awake()
    {
        _inputReader = GetComponent<PlayerInput>();
        _yaw = transform.eulerAngles.y;
    }
    private void OnEnable()
    {
        // lock the cursor bro, don't need that shit swangin
        SetCursorLocked(true);
    }

    private void Update()
    {
        UpdateCursorLock();

        bool isCursorLocked = Cursor.lockState == CursorLockMode.Locked;
        
        // just get out of here if the cursor is not locked man
        if (!isCursorLocked)
        {
            return;
        }

        Vector2 lookDelta = _inputReader.LookDelta;
        _yaw += lookDelta.x; // REMEMBER: yaw is left right...
        _pitch = Mathf.Clamp(_pitch - lookDelta.y, -_maxPitch, _maxPitch); // .. and pitch is up down (clamped)

        // assigns the rotation values (yaw and pitch), I HAVE NO IDEA HOW QUATERNIONS WORKKKKKK
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        _cameraRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }


    // TEMP FOR DEBUGGING CONTROLS (until we have a menu lol)
    private void UpdateCursorLock()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool wasEscapePressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        bool isCursorFree = Cursor.lockState != CursorLockMode.Locked;
        bool wasScreenClicked = isCursorFree && mouse != null && mouse.leftButton.wasPressedThisFrame;

        if (wasEscapePressed)
        {
            SetCursorLocked(false);
        }
        else if (wasScreenClicked)
        {
            SetCursorLocked(true);
        }
    }
}