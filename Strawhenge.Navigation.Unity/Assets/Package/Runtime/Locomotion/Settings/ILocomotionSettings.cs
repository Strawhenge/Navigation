namespace Strawhenge.Navigation.Unity
{
    public interface ILocomotionSettings
    {
        float WalkSpeed { get; }
     
        float RunSpeed { get; }
      
        float SprintSpeed { get; }

        // The slowest the character moves while there's move input (metres per second), however little a stick is
        // pushed, so it doesn't hold speeds its walk animation can't keep pace with. Never more than the selected
        // speed (walk, run or sprint). 0 for none.
        float MinimumMoveSpeed { get; }
      
        float Acceleration { get; }
      
        float Deceleration { get; }
     
        float TurnSpeed { get; }

        // From this turn angle (degrees) on, the speed is limited to RunSpeed straight away, and the turn's start
        // is remembered for matching moving pivots.
        float SharpTurnAngle { get; }

        // From this turn angle (degrees) on, the speed is limited to WalkSpeed straight away.
        float ReversalAngle { get; }

        // Below this speed (metres per second) the character counts as standing, for stationary and start pivots.
        float StationaryPivotMaxSpeed { get; }
      
        // Turns on the spot from standing, played when the input is released within StationaryPivotDecisionTime
        // (a tap).
        IPivotProfile[] StationaryPivots { get; }

        // Turns from standing that move off, played when the input is still held after StationaryPivotDecisionTime.
        // With no match, the character turns and moves off normally.
        IPivotProfile[] StartPivots { get; }

        // How long (seconds) the character holds still from standing, towards a direction a stationary or start
        // pivot covers, to tell a tap from a hold.
        float StationaryPivotDecisionTime { get; }
        
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