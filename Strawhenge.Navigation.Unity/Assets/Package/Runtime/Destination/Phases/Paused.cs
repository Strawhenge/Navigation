namespace Strawhenge.Navigation.Unity.Destination
{
    // The context says the character can't navigate right now, or the agent is off the NavMesh.
    // Keeps the agent disabled until it can, then starts the request again from where it is.
    class Paused : Phase
    {
        readonly IDestinationContext _context;
        readonly Agent _agent;

        public Paused(IDestinationContext context, Agent agent)
        {
            _context = context;
            _agent = agent;
        }

        public override PhaseStep Update(DestinationArgs args, float deltaTime)
        {
            _agent.Disable();

            return _context.CanNavigate
                ? PhaseStep.To(PhaseId.Preparing)
                : PhaseStep.Continue;
        }
    }
}