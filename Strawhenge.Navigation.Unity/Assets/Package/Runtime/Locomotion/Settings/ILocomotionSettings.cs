namespace Strawhenge.Navigation.Unity
{
    public interface ILocomotionSettings
    {
        float WalkSpeed { get; }
     
        float RunSpeed { get; }
      
        float SprintSpeed { get; }
      
        float Acceleration { get; }
      
        float Deceleration { get; }
     
        float TurnSpeed { get; }
      
        IPivotProfile[] StationaryPivots { get; }
        
        IPivotProfile[] MovingPivots { get; }
      
        float JumpHeight { get; }
      
        float CoyoteTime { get; }
      
        // Jumps played while not strafing. The first that matches plays its wind-up animation, which launches
        // the jump; with no match, the character jumps straight away.
        IJumpProfile[] Jumps { get; }

        // Jumps played while strafing, matched on speed and direction. With no match, the character jumps
        // straight away.
        IStrafeJumpProfile[] StrafeJumps { get; }
       
        ILandingProfile[] JumpLandings { get; }
       
        float Gravity { get; }

        // The downward speed (metres per second) a fall starts at when the character leaves the ground without
        // jumping, e.g. walking off a ledge. Also keeps it close to downhill slopes and stairs.
        float FallStartSpeed { get; }
      
        float FallDistance { get; }
       
        ILandingProfile[] FallLandings { get; }

        // Whether the character can turn while airborne: jumping, or falling once it has dropped FallDistance.
        // When it can't, it turns to the input once it lands.
        bool AllowAirborneRotation { get; }

        // How much control there is over horizontal movement while airborne.
        AirborneMovement AirborneMovement { get; }
    }
}