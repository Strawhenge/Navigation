namespace Strawhenge.Navigation.Unity.Destination
{
    // What a phase wants next: carry on, move to another phase, or end the request with a result.
    // A struct, so returning one every frame allocates nothing.
    readonly struct PhaseStep
    {
        readonly PhaseId? _next;

        PhaseStep(PhaseId? next, DestinationResult result)
        {
            _next = next;
            Result = result;
        }

        public static PhaseStep Continue => default;

        public static PhaseStep To(PhaseId next) => new(next, null);

        public static PhaseStep Finish(DestinationResult result) => new(null, result);

        public bool IsContinue => !_next.HasValue && Result == null;

        public bool IsFinish => Result != null;

        public PhaseId Next => _next.GetValueOrDefault();

        public DestinationResult Result { get; }
    }
}