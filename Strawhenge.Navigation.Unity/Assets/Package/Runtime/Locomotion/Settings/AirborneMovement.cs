namespace Strawhenge.Navigation.Unity
{
    // How much control there is over horizontal movement while airborne (jumping, or falling once the character
    // has dropped FallDistance). Speeds and acceleration are the ground ones.
    public enum AirborneMovement
    {
        // Steers and changes speed in the air as on the ground; slows to a stop when the input is released.
        Controlled,

        // Steers and changes speed in the air while there's input; carries on at the same velocity when it's
        // released.
        ControlledKeepsMomentum,

        // No control in the air: carries on at the velocity it left the ground with.
        Momentum
    }
}
