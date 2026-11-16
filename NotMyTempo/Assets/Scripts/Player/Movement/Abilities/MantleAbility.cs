using UnityEngine;

/// <summary>
///     A mantle needs three things:
///         1. A steep surface within reach
///         2. A walkable top no higher than 2m
///         3. Room to rise straight up, and to stand on top
///     
///     "mantle switches the controller off and moves the transform directly along a hand-made curve, then switches it back on."
///     
///     "Lerp (linear interpolation) blends between two values"
///     
/// 
///     Automatic ledge mantle (Titanfall / Cyberpunk). While airborne and pushing toward a ledge within reach,
///     you're pulled up and over it. There's no button: one less thing to think about mid-fight.
///     
/// </summary>
/// 
public class MantleAbility : MovementAbility
{
    [Tooltip("Ledges lower than this (measured from player's feet), aren't mantled (handled by step offset in character controller), m.")]
    [SerializeField] private float _minimumLedgeHeight = 0.5f;
    [Tooltip("How far past the capsule to look for a wall, m.")]
    [SerializeField] private float _wallReach = 0.6f;
    [Tooltip("Seconds after a mantle before another one can start.")]
    [SerializeField] private float _cooldown = 0.25f;
    [Tooltip("Heights above the feet where we look for a wall (the origins of the rays checking for mantle ledges), m.")]
    [SerializeField] private float[] _wallProbeHeights = { 0.3f, 0.9f, 1.5f };
    [Tooltip("Shortest a mantle can take, seconds, even when you arrive very fast")]
    [SerializeField] private float _shortestDuration = 0.12f;

    // -- Constants --
    private const int mantlePriority = 40; // highest priority off any ability
    private const float minimumWallAngle = 70f;
    private const float ledgeProbeInset = 0.1f;
    private const float ledgeProbeClearance = 0.1f;
    private const float landingLift = 0.02f;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private Vector3 _exitDirection;
    private float _progress; // how far into the mantle arc the player is
    private float _duration;
    private float _exitSpeed;
    private float _lastEndTime = float.NegativeInfinity; // negative infinity is better than zero in Unity for some reason

    public override int Priority {  get { return mantlePriority; } }
    public override bool CanJumpCancel { get { return false; } } // jump is buffered until right after mantle instead
    public override bool MovesTransformDirectly {  get { return true; } }
    public override bool WantsToActivate()
    {
        bool isOnCooldown = Time.time - _lastEndTime < _cooldown;
        if (Motor.IsGrounded ||  isOnCooldown || !Motor.HasMoveInput) // all the things that tell us to not do the thing
        {
            return false;
        }

        Vector3 direction = Motor.WishDirection.normalized; // player's direction
        if (!TryFindLedge(direction, out _targetPosition))
        {
            return false;
        }

        return true;
    }

    // -- Methods --
    public override void UpdateActive(float deltaTime)
    {
        _progress = Mathf.Min(1f, _progress + deltaTime / _duration);

        float riseAmount = 1f - Mathf.Pow(1f - _progress, 3f); // fast at first, gentle at the top 
        float moveOverAmount = _progress * _progress; // gentle at first, full speed at the end (ease in)

        Vector3 position = Vector3.Lerp(_startPosition, _targetPosition, moveOverAmount); // lerp the start and target position over "moveOverAmount" time, smoothhh
        position.y = Mathf.Lerp(_startPosition.y, _targetPosition.y, riseAmount); // same thing for y position
        Motor.transform.position = position; // directly setting position, not using character controller :)

        bool isFinished = _progress >= 1f; // if progress is greater than 1, were definitely done!
        if (isFinished)
        {
            Stop();
        }
    }

    protected override void OnActivated() // all the things we need to set when we start a mantle 
    {
        _startPosition = Motor.transform.position;
        _progress = 0f; // set progress to zero at activation

        Vector3 toTarget = _targetPosition - _startPosition;
        toTarget.y = 0f;
        float horiDistance = toTarget.magnitude;
        _exitDirection = toTarget.normalized;

        // An ease-in curve ends at twice its average speed, so this duration makes it
        // finish at exactly the speed we hand back to the motor.
        float desiredExitSpeed = Mathf.Max(Motor.HorizontalSpeed, Stats.MantleExitSpeed);
        _duration = Mathf.Clamp(2f * horiDistance / desiredExitSpeed, _shortestDuration, Stats.MantleDuration);
        _exitSpeed = 2f * horiDistance / _duration;

        Motor.Velocity = Vector3.zero;
        Motor.Controller.enabled = false; // get the character controller outa here when we start this, not using this
    }
    protected override void OnDeactivated() // all the things we need to set when we stop a  mantle
    {
        Motor.Controller.enabled = true; // re-enable the character controller when were done
        Physics.SyncTransforms(); // resyncs stuff because we weren't using character controller for a sec
        Motor.Velocity = _exitDirection * _exitSpeed; // pass off the velocity back to the motor
        _lastEndTime = Time.time; // current time of the end of the mantle, yayayay
    }

    private bool TryFindLedge(Vector3 direction, out Vector3 ledgePosition) // returns true or false AND hands back the position through out
    {
        ledgePosition = default;
        CharacterController controller = Motor.Controller;
        Vector3 feetPosition = Motor.transform.position;
        float maxHeight = Stats.MantleMaxHeight;

        // 1. Is there a wall in front of us?
        float reach = controller.radius + _wallReach;
        if (!TryFindWall(feetPosition, direction, reach, maxHeight, out RaycastHit wallHit))
        {
            return false;
        }

        // 2. Is there a walkable top within reach? Cast down from above, just past the wall's face.
        Vector3 intoWall = new Vector3(-wallHit.normal.x, 0f, -wallHit.normal.z).normalized;
        Vector3 ledgeProbeOrigin = wallHit.point + intoWall * (controller.radius + ledgeProbeInset);
        ledgeProbeOrigin.y = feetPosition.y + maxHeight + ledgeProbeClearance;
        float ledgeProbeLength = maxHeight + ledgeProbeClearance - _minimumLedgeHeight;

        bool hasLedgeTop = Physics.Raycast(ledgeProbeOrigin, Vector3.down, out RaycastHit ledgeHit,
            ledgeProbeLength, Motor.GroundMask, QueryTriggerInteraction.Ignore);
        bool isWalkableTop = hasLedgeTop && Vector3.Angle(ledgeHit.normal, Vector3.up) <= controller.slopeLimit;
        if (!isWalkableTop)
        {
            return false;
        }

        // 3. Room to stand on top, and room to rise straight up before moving over the lip?
        Vector3 candidatePosition = ledgeHit.point + Vector3.up * landingLift;
        Vector3 raisedPosition = new Vector3(feetPosition.x, candidatePosition.y, feetPosition.z);
        bool hasRoomOnTop = Motor.IsCapsuleFree(candidatePosition, Motor.StandingHeight);
        bool hasRoomToRise = Motor.IsCapsuleFree(raisedPosition, Motor.CurrentHeight);
        if (!hasRoomOnTop || !hasRoomToRise)
        {
            return false;
        }

        ledgePosition = candidatePosition;
        return true;
    }

    private bool TryFindWall(Vector3 feetPosition, Vector3 direction, float reach, float maxHeight, out RaycastHit wallHit)
    {
        foreach (float probeHeight in _wallProbeHeights)
        {
            if (probeHeight > maxHeight)
            {
                break;
            }

            Vector3 probeOrigin = feetPosition + Vector3.up * probeHeight;
            bool hasHit = Physics.Raycast(probeOrigin, direction, out wallHit, reach,
                Motor.GroundMask, QueryTriggerInteraction.Ignore);
            bool isSteepEnough = hasHit && Vector3.Angle(wallHit.normal, Vector3.up) >= minimumWallAngle;
            if (isSteepEnough)
            {
                return true;
            }
        }
        wallHit = default;
        return false;
    }

}
