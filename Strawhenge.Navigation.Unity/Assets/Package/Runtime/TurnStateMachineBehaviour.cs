using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnStateMachineBehaviour : StateMachineBehaviour
{
    public event Action Ended;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Ended?.Invoke();
    }
}