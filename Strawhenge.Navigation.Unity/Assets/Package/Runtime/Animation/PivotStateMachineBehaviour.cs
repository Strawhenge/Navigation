using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Put on the pivot sub-state machine. Tells the MovementAnimationSyncScript on the Animator's GameObject when
    // the pivot animation starts and ends. It's found through the Animator, so it doesn't matter that Unity
    // creates new instances of this behaviour when it rebuilds the Animator.
    public class PivotStateMachineBehaviour : StateMachineBehaviour
    {
        readonly AnimatorComponentCache<MovementAnimationSyncScript> _sync = new();

        // A state inside this state machine was entered: the pivot animation has started.
        // OnStateEnter rather than OnStateMachineEnter, which is skipped by transitions straight to a state.
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_sync.TryGet(animator, out var sync))
                sync.OnPivotStarted();
        }

        public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
        {
            if (_sync.TryGet(animator, out var sync))
                sync.OnPivotEnded();
        }
    }
}
