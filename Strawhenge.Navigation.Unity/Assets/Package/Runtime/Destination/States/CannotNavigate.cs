using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    class CannotNavigate : IState
    {
        readonly IDestinationContext _context;
        readonly Agent _agent;

        public CannotNavigate(IDestinationContext context, Agent agent)
        {
            _context = context;
            _agent = agent;
        }

        public bool IsActive => true;

        public Vector3 Velocity => Vector3.zero;

        public Idle IdleState { private get; set; }

        public PrepareGoing PrepareGoingState { private get; set; }

        public DestinationArgs CurrentArgs { private get; set; }

        public void Cancel(Action<IState> changeState)
        {
            changeState(IdleState);

            CurrentArgs.Callback(DestinationResult.Cancelled);
        }

        public void GoTo(DestinationArgs args, Action<IState> changeState)
        {
            PrepareGoingState.CurrentArgs = args;
            changeState(PrepareGoingState);

            CurrentArgs.Callback(
                DestinationResult.CancelledByNewDestination);
        }

        public void Update(Action<IState> changeState)
        {
            _agent.Disable();

            if (_context.CanNavigate)
            {
                PrepareGoingState.CurrentArgs = CurrentArgs;
                changeState(PrepareGoingState);
            }
        }
    }
}