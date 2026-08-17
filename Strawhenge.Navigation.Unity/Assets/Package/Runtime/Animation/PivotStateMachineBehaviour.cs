using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class PivotStateMachineBehaviour : StateMachineBehaviour
    {
        public event Action Ended;

        public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
        {
            Ended?.Invoke();
        }
    }
}