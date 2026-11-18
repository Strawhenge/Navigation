using UnityEngine;
#if UNITY_EDITOR
using Strawhenge.Common.Unity;
using UnityEditor.Animations;
#endif

namespace Strawhenge.Navigation.Unity.Tests
{
    // An Animator controller with the parameters MovementAnimationSyncScript sets (AnimatorParameters) and one
    // empty state. Without a controller, every parameter MovementAnimationSyncScript sets logs "Animator is not
    // playing an AnimatorController". It has no pivot or landing states: tests give
    // MovementAnimationSyncScript the behaviours through SetBehaviours and play the Animator's part themselves.
    // Built with UnityEditor.Animations, so only in the Editor, where PlayMode tests run.
    static class TestAnimatorController
    {
        public static RuntimeAnimatorController Create()
        {
#if UNITY_EDITOR
            var controller = new AnimatorController { name = "Test Animator Controller" };
            controller.AddLayer("Base Layer");
            controller.layers[0].stateMachine.AddState("Idle");

            AddParameter(controller, AnimatorParameters.MoveSpeed, AnimatorControllerParameterType.Float);
            AddParameter(controller, AnimatorParameters.Strafing, AnimatorControllerParameterType.Bool);
            AddParameter(controller, AnimatorParameters.StrafeX, AnimatorControllerParameterType.Float);
            AddParameter(controller, AnimatorParameters.StrafeY, AnimatorControllerParameterType.Float);
            AddParameter(controller, AnimatorParameters.Falling, AnimatorControllerParameterType.Bool);
            AddParameter(controller, AnimatorParameters.Jump, AnimatorControllerParameterType.Trigger);
            AddParameter(controller, AnimatorParameters.Jumping, AnimatorControllerParameterType.Bool);
            AddParameter(controller, AnimatorParameters.Pivot, AnimatorControllerParameterType.Trigger);
            AddParameter(controller, AnimatorParameters.PivotId, AnimatorControllerParameterType.Int);
            AddParameter(controller, AnimatorParameters.PivotRightFoot, AnimatorControllerParameterType.Bool);
            AddParameter(controller, AnimatorParameters.LandingId, AnimatorControllerParameterType.Int);
            AddParameter(controller, AnimatorParameters.Land, AnimatorControllerParameterType.Trigger);

            return controller;
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        static void AddParameter(
            AnimatorController controller,
            AnimatorParameter parameter,
            AnimatorControllerParameterType type) =>
            controller.AddParameter(parameter.Name, type);
#endif
    }
}
