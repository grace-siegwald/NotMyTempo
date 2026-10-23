using UnityEngine;

/// <summary>
/// Base class for every movement ability like the slide dash mantle grapple + anything else we come up with
/// 
/// Only one ability is active at a time. It has the player's velocity. DESIGN PILLAR BABY
/// </summary>

// Basically everything that makes an ability and *ability* besides from the actual *behavior* that 
// makes it a specific ability (slide has different behavior than dash, etc)
// It's abstract because it's only here to be inherited from, seperation of concerns BABY

// Motor calls Bind() on startup
// Every frame: UpdatePassive() is an always food, WantsToActivate() is a sometimes food for only when
// its inactive, UpdateActive() is a sometimes food for only when its active.
// OnActivated and OnDeactivated run once at each change.
public abstract class MovementAbility : MonoBehaviour
{
    // -- properties -- 
    public bool IsActive { get; private set; } // is the ability active?
    public virtual bool CanJumpCancel { get { return true; } } // a jump input can "end" the ability and inherit its momentum
    public virtual int Priority { get {  return 0; }  } // higher priority abilities can interrupt lower ones
    public virtual bool MovesTransformDirectly { get { return false; } } // for moves that skip CharacterController.Move, just for mantle rn
    
    // components references, protected so only the subclasses can see
    protected PlayerMotor Motor { get; private set; }
    protected PlayerInput InputReader { get { return Motor.InputReader; } } 
    protected MovementStats Stats { get { return Motor.Stats; } }


    // -- methods -- 

    // Abstract because every subclass NEEDS to have own version of this
    // Asked every frame while inactive, return true to take over.
    public abstract bool WantsToActivate();
    // runs every frame while ability is active instead of the motor's "defualt" movement
    public abstract void UpdateActive(float deltaTime);
    // runs every frame in the background (active or not), timers and cooldowns and such
    public virtual void UpdatePassive(float deltaTime)
    {
    }
    // runs when the player has activated a jump cancel (so whenever the player ends this ability with a jump)
    public virtual void OnJumpCancel()
    {
    }
    
    // internal stuff // ONLY FOR PLAYER MOTOR, DO NOT CALL ELSEWHERE
    internal void Bind(PlayerMotor motor)
    {
        Motor = motor;
        OnBound();
    }
    internal void Activate()
    {
        IsActive = true;
        OnActivated();
    }
    internal void Deactivate()
    {
        IsActive = false;
        OnDeactivated();
    }

    // called once when the motor gets this ability. 
    protected virtual void OnBound()
    {
    }
    protected virtual void OnActivated()
    {
    }
    protected virtual void OnDeactivated()
    {
    }

    // ends the ability. motor defualt movement takes over this frame
    protected void Stop()
    {
        Motor.StopAbility(this); 
    }
}
