using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder (-100)] // so that unity runs this scripts Update before anything else. INPUT NEEDS TO BE READ FIRST ON EVERY FRAME
public class PlayerInput : MonoBehaviour
{
    // Actions
    [Header("Actions")]
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _lookAction;

    // Look
    [Header("Look")]
    [SerializeField] private float _mouseSensitivity = 0.1f;
    [SerializeField] private float _gamepadLookSpeed = 200f;
    [SerializeField] private bool _isVerticalLookInverted;

    // Properties
    // the movement input, x is strafing, y is forward. It's length is 0 to 1 
    public Vector2 MoveInput { get; private set; }
    
    // the degrees to rotate the look delta this frame. x = yaw (left and right), y = pitch (up and down)
    public Vector2 LookDelta { get; private set; }

    private void OnEnable()
    {
        // we always make sure to manually enable the actions, just incase something has been fiddled with
        _moveAction.action.Enable();
        _lookAction.action.Enable();
    }

    private void Update()
    {
        // gets the movement input value, clamps it to stay between 0 and 1 
        MoveInput = Vector2.ClampMagnitude(_moveAction.action.ReadValue<Vector2>(), 1f);
        
        // gets the look input value
        LookDelta = ReadLookDelta();
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
