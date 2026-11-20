namespace Strawhenge.Navigation.Unity
{
    public enum MotorUpdateMode
    {
        Update,
        FixedUpdate,

        // The motor steps the controller from its own callbacks (IExternallySteppedMotor).
        External
    }
}
