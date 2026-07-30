using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Idle : State
    {
        readonly Agent _agent;

        public Idle(Agent agent)
        {
            _agent = agent;
        }

        protected internal override bool IsActive => false;

        protected internal override Vector3 Velocity => Vector3.zero;

        public PrepareGoing PrepareGoingState { private get; set; }

        protected internal override void Cancel()
        {
        }

        protected internal override void GoTo(DestinationArgs args)
        {
            PrepareGoingState.CurrentArgs = args;
            ChangeState(PrepareGoingState);
        }

        protected internal override void Update()
        {
            _agent.NavMeshAgent.enabled = false;
        }
    }
}