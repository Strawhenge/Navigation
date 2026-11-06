using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Enables the agent, warps it to where the trip starts and calculates the path.
    class Preparing : Phase
    {
        readonly Agent _agent;
        readonly NavMeshPath _path = new();

        public Preparing(Agent agent)
        {
            _agent = agent;
        }

        public override PhaseStep Update(DestinationArgs args, float deltaTime)
        {
            if (_agent.NavMeshAgent.isOnOffMeshLink)
                return PhaseStep.Continue;

            _agent.Enable(args.Leisurely);

            _agent.NavMeshAgent.Warp(
                args.StartingLocation ?? _agent.GetCurrentPosition());

            if (!_agent.NavMeshAgent.isOnNavMesh)
            {
                _agent.Disable();
                return PhaseStep.Finish(DestinationResult.Inaccessible);
            }

            _agent.NavMeshAgent.isStopped = true;
            _agent.NavMeshAgent.updatePosition = true;
            _agent.NavMeshAgent.updateRotation = !args.Strafe;

            _agent.NavMeshAgent.CalculatePath(args.Location, _path);

            if (!IsAccessible(args))
                return PhaseStep.Finish(DestinationResult.Inaccessible);

            _agent.NavMeshAgent.speed = args.Speed;
            _agent.NavMeshAgent.SetPath(_path);
            _agent.NavMeshAgent.isStopped = false;

            return PhaseStep.To(PhaseId.Walking);
        }

        bool IsAccessible(DestinationArgs args) =>
            _path.status == NavMeshPathStatus.PathComplete ||
            (!args.LocationMustBeExact && _path.status == NavMeshPathStatus.PathPartial);
    }
}