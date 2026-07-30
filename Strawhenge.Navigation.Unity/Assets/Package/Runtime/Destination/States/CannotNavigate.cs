using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    class CannotNavigate : State
    {
        readonly IDestinationContext _context;
        readonly Agent _agent;

        public CannotNavigate(IDestinationContext context, Agent agent)
        {
            _context = context;
            _agent = agent;
        }

        protected internal override Vector3 Velocity => Vector3.zero;

        public Idle IdleState { private get; set; }

        public PrepareGoing PrepareGoingState { private get; set; }

        public DestinationArgs CurrentArgs { private get; set; }

        protected internal override void Cancel()
        {
            ChangeState(IdleState);

            CurrentArgs.Callback(DestinationResult.Cancelled);
        }

        protected internal override void GoTo(DestinationArgs args)
        {
            PrepareGoingState.CurrentArgs = args;
            ChangeState(PrepareGoingState);

            CurrentArgs.Callback(
                DestinationResult.CancelledByNewDestination);
        }

        protected internal override void Update()
        {
            _agent.Disable();

            if (_context.CanNavigate)
            {
                PrepareGoingState.CurrentArgs = CurrentArgs;
                ChangeState(PrepareGoingState);
            }
        }
    }
}