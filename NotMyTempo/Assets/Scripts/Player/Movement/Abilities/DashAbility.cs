using UnityEngine;

// TODO: add description
public class DashAbility : MovementAbility
{
    // -- fields --
    private const int DashPriority = 30; // magic number for right now
    private const float MinimumRechargeTime = 0.01f;

    private float _rechargeProgress;
    private Vector3 _dashDirection;
    private float _elapsedDashTime;
    private float _entrySpeed;
    private bool _wasJumpCancelled;

    public override int Priority { get { return DashPriority; } }
    public int Charges { get; private set; }
    public override bool WantsToActivate()
    {
        return Charges > 0 && InputReader.HasBufferedDash;
    }

    public override void UpdatePassive(float deltaTime)
    {
        bool isFull = Charges >= Stats.DashCharges;
        if (isFull)
        {
            Charges = Stats.DashCharges;
            _rechargeProgress = 0f;
            return;
        }

        _rechargeProgress += deltaTime / Mathf.Max(MinimumRechargeTime, Stats.DashRechargeTime);
        if (_rechargeProgress >= 1f)
        {
            Charges++;
            _rechargeProgress -= 1f;
        }
    }
    public override void UpdateActive(float deltaTime)
    {
        _elapsedDashTime += deltaTime;
        Motor.Velocity = _dashDirection * Stats.DashSpeed;

        bool isFinished = _elapsedDashTime >= Stats.DashDuration;
        if (isFinished)
        {
            Stop();
        }
    }
    public override void OnJumpCancel()
    {
        _wasJumpCancelled = true;
    }
    protected override void OnBound()
    {
        Charges = Stats.DashCharges;
    }
    protected override void OnActivated()
    {
        InputReader.ConsumeDash();
        Charges--;
        _elapsedDashTime = 0f;
        _wasJumpCancelled = false;
        _entrySpeed = Motor.HorizontalSpeed;

        if (Motor.HasMoveInput)
        {
            _dashDirection = Motor.WishDirection.normalized;
        }
        else
        {
            _dashDirection = Motor.transform.forward;
        }
        Motor.Velocity = _dashDirection * Stats.DashSpeed;
    }
    protected override void OnDeactivated()
    {
        if (_wasJumpCancelled)
        {
            return;
        }

        float exitSpeed = Mathf.Max(_entrySpeed, Stats.DashExitSpeed);
        Motor.Velocity = _dashDirection * exitSpeed;
    }
}
