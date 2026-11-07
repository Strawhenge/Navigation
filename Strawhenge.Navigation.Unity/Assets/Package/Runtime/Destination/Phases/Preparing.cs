using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Enables the agent, warps it to where the trip starts and calculates the path to the request's
    // target (see TargetResolver). Hands over to Pivoting first if a stationary pivot matches the turn
    // towards the path.
    class Preparing : Phase
    {
        readonly Agent _agent;
        readonly TargetResolver _targets;
        readonly Pivoting _pivoting;
        readonly NavMeshPath _path = new();

        // The start of the path and its first corner; reused to avoid garbage.
        readonly Vector3[] _corners = new Vector3[2];

        public Preparing(Agent agent, TargetResolver targets, Pivoting pivoting)
        {
            _agent = agent;
            _targets = targets;
            _pivoting = pivoting;
        }

        public override PhaseStep Update(DestinationArgs args, float deltaTime)
        {
            if (_agent.NavMeshAgent.isOnOffMeshLink)
                return PhaseStep.Continue;

            // Before the warp, which stops the agent: a trip restarted on the move shouldn't play a pivot
            // meant for standing still.
            var speed = GetCurrentSpeed();

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

            if (!_targets.TryResolve(args, out var target))
                return PhaseStep.Finish(DestinationResult.Inaccessible);

            _agent.NavMeshAgent.CalculatePath(target, _path);

            if (!IsAccessible(args))
                return PhaseStep.Finish(DestinationResult.Inaccessible);

            _agent.NavMeshAgent.speed = args.Speed;
            _agent.NavMeshAgent.SetPath(_path);

            // The agent stays stopped while the pivot plays; Pivoting starts it again.
            if (!args.Strafe && TryPivot(speed))
                return PhaseStep.To(PhaseId.Pivoting);

            _agent.NavMeshAgent.isStopped = false;

            return PhaseStep.To(PhaseId.Moving);
        }

        bool TryPivot(float speed)
        {
            if (_path.GetCornersNonAlloc(_corners) < 2)
                return false;

            var direction = _corners[1] - _agent.GetCurrentPosition();
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return false;

            var turnAngle = Vector3.SignedAngle(_agent.NavMeshAgent.transform.forward, direction, Vector3.up);
            return _pivoting.TrySetUpStationary(speed, turnAngle);
        }

        float GetCurrentSpeed()
        {
            if (!_agent.NavMeshAgent.isActiveAndEnabled || !_agent.NavMeshAgent.isOnNavMesh)
                return 0f;

            var velocity = _agent.NavMeshAgent.velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }

        bool IsAccessible(DestinationArgs args) =>
            _path.status == NavMeshPathStatus.PathComplete ||
            (!args.LocationMustBeExact && _path.status == NavMeshPathStatus.PathPartial);
    }
}