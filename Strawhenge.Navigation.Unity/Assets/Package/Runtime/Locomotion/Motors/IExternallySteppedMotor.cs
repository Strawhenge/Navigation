namespace Strawhenge.Navigation.Unity
{
    // A motor whose own callbacks drive the step (e.g. a third-party character controller that calls you to
    // ask for velocity and rotation). Its UpdateMode is External, and LocomotionScript hands it the stepper.
    public interface IExternallySteppedMotor : ICharacterMotor
    {
        void SetStepper(ILocomotionStepper stepper);
    }
}
