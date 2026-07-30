using System;
using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    public class DestinationController
    {
        readonly NavMeshAgent _agent;
        IState _state;

        internal DestinationController(IDestinationContext context, Agent agent)
        {
            _agent = agent.NavMeshAgent;

            var idleState = new Idle(agent);
            var prepareGoingState = new PrepareGoing(context, agent);
            var goingState = new Going(context, agent);
            var cannotNavigateState = new CannotNavigate(context, agent);

            idleState.PrepareGoingState = prepareGoingState;
            prepareGoingState.IdleState = idleState;
            prepareGoingState.GoingState = goingState;
            prepareGoingState.CannotNavigateState = cannotNavigateState;
            goingState.IdleState = idleState;
            goingState.CannotNavigateState = cannotNavigateState;
            cannotNavigateState.IdleState = idleState;
            cannotNavigateState.PrepareGoingState = prepareGoingState;

            _state = idleState;
        }

        public event Action Activated;
        public event Action Deactivated;

        public bool IsActive => _state.IsActive;

        public Vector3 CurrentVelocity => _state.Velocity;

        public bool IsLocationAccessible(Vector3 location) => _agent.IsLocationAccessible(location);

        public void GoTo(DestinationArgs args) => _state.GoTo(args, OnChangeState);

        public void Cancel() => _state.Cancel(OnChangeState);

        public void Update() => _state.Update(OnChangeState);

        void OnChangeState(IState newState)
        {
            var oldState = _state;
            _state = newState;

            if (!oldState.IsActive && newState.IsActive)
                Activated?.Invoke();

            if (oldState.IsActive && !newState.IsActive)
                Deactivated?.Invoke();
        }
    }
}