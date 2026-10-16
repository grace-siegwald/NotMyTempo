using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads the player's actions from the InputSystem_Actions asset gets their "values"
/// for the other player movement scripts. Nothing else on the Player talks to the Input System directly.
/// it all comes through here :)
/// </summary>

[DefaultExecutionOrder (-100)] // so that unity runs this scripts Update before anything else. INPUT NEEDS TO BE READ FIRST ON EVERY FRAME
public class PlayerInput : MonoBehaviour
{
    // Actions
    [Header("Actions")]
    [Tooltip("\"Player/Move\" from the InputSystem_Actions thing")]
    [SerializeField] private InputActionReference _moveAction; // "Player/Move" from the InputSystem_Actions thing
    [Tooltip("\"Player/Look\" from the InputSystem_Actions thing")]
    [SerializeField] private InputActionReference _lookAction; // "Player/Look" from the InputSystem_Actions thing
    [Tooltip("\"Player/Jump\" from the InputSystem_Actions thing")]
    [SerializeField] private InputActionReference _jumpAction; // "Player/Jump" from the InputSystem_Actions thing

    // Look
    [Header("Look")]
    [Tooltip("Degrees of rotation per pixel of mouse movement")]
    [SerializeField] private float _mouseSensitivity = 0.1f; // degrees of rotation per pixel of mouse movement
    [Tooltip("Degrees per second at the FULL right stick tilt position")]
    [SerializeField] private float _gamepadLookSpeed = 200f; // degrees per second at the FULL right stick tilt position
    [Tooltip("Invert vertial looking")]
    [SerializeField] private bool _isVerticalLookInverted; // invert vertial looking

    // Input Buffering
    [Header("Buffering")]
    [Tooltip("Amount of time a button press is remembered, so a slightly early press still counts")]
    [SerializeField] private float _bufferTime = 0.15f; // amount of time a button press is remembered, so a slightly early press still counts

    private float _jumpPressedTime = float.NegativeInfinity; // float negitive infinity means "never pressed"

    // Properties
    public Vector2 MoveInput { get; private set; } // the movement input, x is strafing, y is forward. It's length is 0 to 1 

    // the degrees to rotate the look delta this frame. x = yaw (left and right), y = pitch (up and down)
    public Vector2 LookDelta { get; private set; }
    public bool HasBufferedJump { get { return Time.time - _jumpPressedTime <= _bufferTime; } }

    public void ConsumeJump()
    {
        _jumpPressedTime = float.NegativeInfinity; 
    }

    private void OnEnable()
    {
        // we always make sure to manually enable the actions, just incase something has been fiddled with
        _moveAction.action.Enable();
        _lookAction.action.Enable();
        _jumpAction.action.Enable();
    }

    private void Update()
    {
        // gets the movement input value, clamps it to stay between 0 and 1 
        MoveInput = Vector2.ClampMagnitude(_moveAction.action.ReadValue<Vector2>(), 1f);
        
        // gets the look input value
        LookDelta = ReadLookDelta();

        // gets the jump input time pressed
        if (_jumpAction.action.WasPressedThisFrame())
        {
            _jumpPressedTime = Time.time;
        }
    }

    // gets the look delta from the input system and iterprets it as needed per input type (controller or mouse)
    private Vector2 ReadLookDelta()
    {
        Vector2 lookDelta;
        InputAction lookAction = _lookAction.action; // just getting the assigned look action from the system
        Vector2 rawLook = lookAction.ReadValue<Vector2>(); // and then getting its vector2 value
        bool isUsingGamepad = lookAction.activeControl?.device is Gamepad;

        // need to multiply gamepad look by deltaTime, but not mouse look, so split them up
        if (isUsingGamepad)
        {
            lookDelta = rawLook * (_gamepadLookSpeed * Time.deltaTime);
        }
        else
        {
            lookDelta = rawLook * _mouseSensitivity;
        }

        // for when look is inverted for those FREAKS
        if (_isVerticalLookInverted)
        {
            lookDelta.y = -lookDelta.y;
        }

        return lookDelta;
    }
}
