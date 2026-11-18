using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace Strawhenge.Navigation.Unity.Tests
{
    // An Animator controller with the parameters MovementAnimationSyncScript sets (the names in
    // AnimatorParameters, which tests can't reference) and one empty state. Without a controller, every
    // parameter MovementAnimationSyncScript sets logs "Animator is not playing an AnimatorController".
    // It has no pivot or landing states: tests give MovementAnimationSyncScript the behaviours through
    // SetBehaviours and play the Animator's part themselves.
    // Built with UnityEditor.Animations, so only in the Editor, where PlayMode tests run.
    static class TestAnimatorController
    {
        public static RuntimeAnimatorController Create()
        {
#if UNITY_EDITOR
            var controller = new AnimatorController { name = "Test Animator Controller" };
            controller.AddLayer("Base Layer");
            controller.layers[0].stateMachine.AddState("Idle");

            controller.AddParameter("Move Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Strafing", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Strafe X", AnimatorControllerParameterType.Float);
            controller.AddParameter("Strafe Y", AnimatorControllerParameterType.Float);
            controller.AddParameter("Falling", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Jumping", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Pivot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Pivot ID", AnimatorControllerParameterType.Int);
            controller.AddParameter("Pivot Right Foot", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Landing ID", AnimatorControllerParameterType.Int);
            controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);

            return controller;
#else
            return null;
#endif
        }
    }
}
