using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Idle : IState
    {
        readonly Agent _agent;

        public Idle(Agent agent)
        {
            _agent = agent;
        }

        public bool IsActive => false;

        public Vector3 Velocity => Vector3.zero;
        
        public PrepareGoing PrepareGoingState {private get; set;}

        public void Cancel(Action<IState> changeState)
        {
        }

        public void GoTo(DestinationArgs args, Action<IState> changeState)
        {
            PrepareGoingState.CurrentArgs = args;
            changeState(PrepareGoingState);
        }

        public void Update(Action<IState> changeState)
        {
            _agent.NavMeshAgent.enabled = false;
        }
    }
}
