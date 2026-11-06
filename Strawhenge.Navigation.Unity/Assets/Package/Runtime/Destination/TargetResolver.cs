using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Where to calculate a request's path to. An exact request goes to its location as given. An
    // approximate request goes to the nearest point on the NavMesh within ApproximateSearchDistance,
    // which is its own location when that's on the NavMesh. The path there may still be partial, ending
    // at the closest point the character can reach, which approximate requests accept.
    class TargetResolver
    {
        readonly Agent _agent;
        readonly IDestinationSettings _settings;

        public TargetResolver(Agent agent, IDestinationSettings settings)
        {
            _agent = agent;
            _settings = settings;
        }

        public bool TryResolve(DestinationArgs args, out Vector3 target)
        {
            if (args.LocationMustBeExact)
            {
                target = args.Location;
                return true;
            }

            return _agent.TryGetNearestNavMeshPoint(
                args.Location,
                args.Leisurely,
                _settings.ApproximateSearchDistance,
                out target);
        }
    }
}
