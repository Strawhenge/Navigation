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
       
        SerializedLandingProfile[] JumpLandings { get; }
       
        float Gravity { get; }
      
        float GroundedGravity { get; }
      
        float FallDistance { get; }
       
        SerializedLandingProfile[] FallLandings { get; }
    }
}