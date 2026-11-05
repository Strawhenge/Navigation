using System;

namespace Strawhenge.Navigation.Unity.Destination
{
    class StatesContainer
    {
        public StatesContainer(
            IDestinationContext context,
            Agent agent,
            IDestinationSettings settings,
            Action<State> onChangeState,
            Action onJumpBegan, Action onJumpEnded,
            Action onFallBegan, Action onFallEnded)
        {
            Idle = new Idle(agent);
            PrepareGoing = new PrepareGoing(context, agent);
            Going = new Going(context, agent);
            CannotNavigate = new CannotNavigate(context, agent);
            Jumping = new Jumping(context, agent, settings);
            Falling = new Falling(context, agent, settings);

            Idle.SetStatesContainer(this);
            PrepareGoing.SetStatesContainer(this);
            Going.SetStatesContainer(this);
            CannotNavigate.SetStatesContainer(this);
            Jumping.SetStatesContainer(this);
            Falling.SetStatesContainer(this);

            Idle.ChangeStateRequested += onChangeState;
            PrepareGoing.ChangeStateRequested += onChangeState;
            Going.ChangeStateRequested += onChangeState;
            CannotNavigate.ChangeStateRequested += onChangeState;
            Jumping.ChangeStateRequested += onChangeState;
            Falling.ChangeStateRequested += onChangeState;
            Jumping.JumpBegan += onJumpBegan;
            Jumping.JumpEnded += onJumpEnded;
            Falling.FallBegan += onFallBegan;
            Falling.FallEnded += onFallEnded;
        }

        public Idle Idle { get; }

        public PrepareGoing PrepareGoing { get; }

        public Going Going { get; }

        public CannotNavigate CannotNavigate { get; }

        public Jumping Jumping { get; }

        public Falling Falling { get; }
    }
}