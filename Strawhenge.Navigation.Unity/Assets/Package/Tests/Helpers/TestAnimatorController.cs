using UnityEngine;
#if UNITY_EDITOR
using Strawhenge.Common.Unity;
using UnityEditor.Animations;
#endif

namespace Strawhenge.Navigation.Unity.Tests
{
    // Animator controllers for tests, built with UnityEditor.Animations, so only in the Editor, where PlayMode
    // tests run. Without a controller, every parameter MovementAnimationSyncScript sets logs "Animator is not
    // playing an AnimatorController".
    static class TestAnimatorController
    {
        public const int PivotId = 1;
        public const int LandingId = 2;

        // The tags on the pivot and landing states, to tell when the Animator is playing one.
        public const string PivotTag = "Pivot";
        public const string LandingTag = "Landing";

        // How long the pivot and landing states play before they exit.
        public const float ClipLength = 0.2f;

        static AnimationClip _clip;

        // The parameters MovementAnimationSyncScript sets (AnimatorParameters) and one empty state. It has no
        // pivot or landing states: tests play the Animator's part by calling the behaviours themselves.
        public static RuntimeAnimatorController Create()
        {
#if UNITY_EDITOR
            return CreateController();
#else
            return null;
#endif
        }

        // As Create, plus Pivot and Landing sub-state machines carrying the real behaviours, as in a consumer's
        // controller, so the Animator plays the pivot and landing itself. Each has one state, for PivotId /
        // LandingId, entered from Idle on the trigger and the ID, which plays for ClipLength and then exits
        // back to Idle.
        public static RuntimeAnimatorController CreateWithPivotAndLandingStates()
        {
#if UNITY_EDITOR
            var controller = CreateController();
            var root = controller.layers[0].stateMachine;
            var idle = root.defaultState;

            AddRequestedState<PivotStateMachineBehaviour>(
                root, idle, PivotTag, AnimatorParameters.Pivot, AnimatorParameters.PivotId, PivotId);

            AddRequestedState<LandingStateMachineBehaviour>(
                root, idle, LandingTag, AnimatorParameters.Land, AnimatorParameters.LandingId, LandingId);

            return controller;
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        static AnimatorController CreateController()
        {
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
        }

        static void AddParameter(
            AnimatorController controller,
            AnimatorParameter parameter,
            AnimatorControllerParameterType type) =>
            controller.AddParameter(parameter.Name, type);

        static void AddRequestedState<T>(
            AnimatorStateMachine root,
            AnimatorState idle,
            string name,
            AnimatorParameter trigger,
            AnimatorParameter idParameter,
            int id)
            where T : StateMachineBehaviour
        {
            var stateMachine = root.AddStateMachine(name);
            stateMachine.AddStateMachineBehaviour<T>();

            var state = stateMachine.AddState($"{name} {id}");
            state.motion = GetClip();
            state.tag = name;

            var enter = idle.AddTransition(state);
            enter.hasExitTime = false;
            enter.hasFixedDuration = true;
            enter.duration = 0;
            enter.AddCondition(AnimatorConditionMode.If, 0, trigger.Name);
            enter.AddCondition(AnimatorConditionMode.Equals, id, idParameter.Name);

            var exit = state.AddExitTransition();
            exit.hasExitTime = true;
            exit.exitTime = 1;
            exit.hasFixedDuration = true;
            exit.duration = 0;

            root.AddStateMachineTransition(stateMachine, idle);
        }

        // A clip that lasts ClipLength and animates nothing: its curve is bound to a child that doesn't exist.
        // Shared by every controller and never destroyed.
        static AnimationClip GetClip()
        {
            if (_clip != null)
                return _clip;

            _clip = new AnimationClip { name = "Test Clip", hideFlags = HideFlags.HideAndDontSave };
            _clip.SetCurve("Unbound", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0, ClipLength, 0));

            return _clip;
        }
#endif
    }
}
