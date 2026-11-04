using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class LandingStateMachineBehaviour : StateMachineBehaviour
    {
        // Raised when a state inside this state machine is entered: the landing animation has started.
        // OnStateEnter rather than OnStateMachineEnter, which is skipped by transitions straight to a state.
        public event Action Started;

        public event Action Ended;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Started?.Invoke();
        }

        public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
        {
            Ended?.Invoke();
        }
    }
}