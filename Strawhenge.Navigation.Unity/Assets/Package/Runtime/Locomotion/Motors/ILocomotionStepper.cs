namespace Strawhenge.Navigation.Unity
{
    // The two halves of a locomotion step, for motors that step it themselves (IExternallySteppedMotor):
    // BeginStep before the motor moves, EndStep with the result after.
    public interface ILocomotionStepper
    {
        LocomotionStep BeginStep(float deltaTime);

        void EndStep(MotorMoveResult result);
    }
}
