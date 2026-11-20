using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // A LocomotionController on a FakeMotor, active and stepped by hand at a fixed frame time. No GameObjects or
    // physics, so tests run instantly and frame by frame.
    sealed class LocomotionControllerHarness
    {
        public const float FrameTime = 1f / 60f;
        public const float AnimatorWaitTimeout = 2f;

        public LocomotionControllerHarness(ILocomotionSettings settings = null)
        {
            Controller = new LocomotionController(
                Motor,
                settings ?? new FakeLocomotionSettings(),
                AnimatorWaitTimeout,
                Logger);

            Controller.Activate();
        }

        public FakeMotor Motor { get; } = new();

        public FakeLogger Logger { get; } = new();

        public LocomotionController Controller { get; }

        // The horizontal speed the controller asked for in its last move.
        public float LastRequestedSpeed => Vector3.ProjectOnPlane(Motor.LastMove.Velocity, Vector3.up).magnitude;

        public void Step(int frames = 1)
        {
            for (var i = 0; i < frames; i++)
                Controller.Step(FrameTime);
        }

        public void StepFor(float seconds) => Step(Mathf.CeilToInt(seconds / FrameTime));

        // Steps until condition is true, for at most timeout seconds. Returns whether it became true.
        public bool StepUntil(Func<bool> condition, float timeout = 5f)
        {
            for (var time = 0f; time < timeout; time += FrameTime)
            {
                if (condition())
                    return true;

                Controller.Step(FrameTime);
            }

            return condition();
        }
    }
}
