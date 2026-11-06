using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    // One stage of moving the agent. Phases know nothing about requests: they never make callbacks or
    // switch phase themselves. DestinationController passes in the current args and applies the
    // PhaseStep they return.
    abstract class Phase
    {
        // False while moving the character by hand (jumps and drops): those always finish, and a new
        // request is followed once they land.
        public virtual bool CanInterrupt => true;

        public virtual Vector3 Velocity => Vector3.zero;

        // current is null after a cancel mid-jump or drop.
        public virtual bool IsStrafing(DestinationArgs current) => current?.Strafe ?? false;

        public virtual void Enter(DestinationArgs args)
        {
        }

        // args is null only in a phase that can't be interrupted, after its request was cancelled.
        public abstract PhaseStep Update(DestinationArgs args, float deltaTime);

        // Leaving for any reason: the next phase, an ended request or a redirect.
        public virtual void Exit()
        {
        }

        // Whether a new request can be applied without leaving this phase (see Redirect).
        public virtual bool CanRedirectInPlace(DestinationArgs args) => false;

        public virtual void Redirect(DestinationArgs args)
        {
        }

        // The character's GameObject was reactivated. Unity resets a NavMeshAgent when its GameObject
        // is deactivated, so its path is gone: phases that rely on it must set it up again.
        public virtual PhaseStep OnAgentReactivated() => PhaseStep.Continue;
    }
}