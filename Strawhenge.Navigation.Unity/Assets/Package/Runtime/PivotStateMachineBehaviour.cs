using System;
using UnityEngine;

public class PivotStateMachineBehaviour : StateMachineBehaviour
{
    public event Action Ended;

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        Ended?.Invoke();
    }
}