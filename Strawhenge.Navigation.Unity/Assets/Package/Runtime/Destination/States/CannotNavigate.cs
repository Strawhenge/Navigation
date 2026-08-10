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

        public DestinationArgs CurrentArgs { private get; set; }

        protected internal override void Cancel()
        {
            ChangeState(States.Idle);

            CurrentArgs.Callback(DestinationResult.Cancelled);
        }

        protected internal override void GoTo(DestinationArgs args)
        {
            States.PrepareGoing.CurrentArgs = args;
            ChangeState(States.PrepareGoing);

            CurrentArgs.Callback(
                DestinationResult.CancelledByNewDestination);
        }

        protected internal override void Update()
        {
            _agent.Disable();

            if (_context.CanNavigate)
            {
                States.PrepareGoing.CurrentArgs = CurrentArgs;
                ChangeState(States.PrepareGoing);
            }
        }
    }
}