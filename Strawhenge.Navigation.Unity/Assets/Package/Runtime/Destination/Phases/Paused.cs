namespace Strawhenge.Navigation.Unity.Destination
{
    // The context says the character can't navigate right now, or the agent is off the NavMesh.
    // Disables the agent as soon as it's entered, so it stops moving the character and frees its avoidance
    // priority slot straight away. Waits until the character can navigate, then starts the request again
    // from where it is.
    class Paused : Phase
    {
        readonly IDestinationContext _context;
        readonly Agent _agent;

        public Paused(IDestinationContext context, Agent agent)
        {
            _context = context;
            _agent = agent;
        }

        public override void Enter(DestinationArgs args, bool strafe)
        {
            _agent.Disable();
        }

        public override PhaseStep Update(DestinationArgs args, float deltaTime)
        {
            return _context.CanNavigate
                ? PhaseStep.To(PhaseId.Preparing)
                : PhaseStep.Continue;
        }
    }
}