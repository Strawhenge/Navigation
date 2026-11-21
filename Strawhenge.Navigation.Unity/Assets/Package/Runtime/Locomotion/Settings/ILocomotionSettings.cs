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
      
        bool DeferJumpTrigger { get; }
       
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
    }
}