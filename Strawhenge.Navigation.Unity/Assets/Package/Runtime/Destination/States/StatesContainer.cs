namespace Strawhenge.Navigation.Unity.Destination
{
    class StatesContainer
    {
        public StatesContainer(IDestinationContext context, Agent agent, System.Action<State> onChangeState)
        {
            Idle = new Idle(agent);
            PrepareGoing = new PrepareGoing(context, agent);
            Going = new Going(context, agent);
            CannotNavigate = new CannotNavigate(context, agent);

            Idle.States = this;
            PrepareGoing.States = this;
            Going.States = this;
            CannotNavigate.States = this;

            Idle.ChangeStateRequested += onChangeState;
            PrepareGoing.ChangeStateRequested += onChangeState;
            Going.ChangeStateRequested += onChangeState;
            CannotNavigate.ChangeStateRequested += onChangeState;
        }

        public Idle Idle { get; }

        public PrepareGoing PrepareGoing { get; }

        public Going Going { get; }

        public CannotNavigate CannotNavigate { get; }
    }
}