using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Owns the current request and makes every callback. Phases only move the agent: each update they
    // get the current args and return what should happen next (PhaseStep).
    public class DestinationController
    {
        readonly IDestinationContext _context;
        readonly Agent _agent;
        readonly Preparing _preparing;
        readonly Walking _walking;
        readonly Jumping _jumping;
        readonly Falling _falling;
        readonly Paused _paused;

        // The request being carried out. Null when there is none, including while a jump or drop
        // finishes after its request was cancelled.
        DestinationArgs _current;

        // What the agent is doing. Null when idle. Can outlive _current: a jump or drop always finishes.
        Phase _phase;

        // The request changed, or the agent was reactivated, during a jump or drop. The agent's path is
        // then stale, so it goes through Preparing rather than Walking once it lands.
        bool _restartAfterTraversal;

        // Without settings (e.g. in tests), DefaultDestinationSettings is used.
        internal DestinationController(IDestinationContext context, Agent agent, IDestinationSettings settings = null)
        {
            _context = context;
            _agent = agent;

            settings ??= DefaultDestinationSettings.Instance;

            _preparing = new Preparing(agent);
            _walking = new Walking(agent);
            _jumping = new Jumping(agent, settings);
            _falling = new Falling(agent, settings);
            _paused = new Paused(context, agent);

            _jumping.JumpBegan += OnJumpBegan;
            _jumping.JumpEnded += OnJumpEnded;
            _falling.FallBegan += OnFallBegan;
            _falling.FallEnded += OnFallEnded;
        }

        public event Action Activated;
        public event Action Deactivated;
        public event Action JumpBegan;
        public event Action JumpEnded;
        public event Action FallBegan;
        public event Action FallEnded;

        // Stays true until a jump or drop in progress lands, even if its request has already ended.
        public bool IsActive => _phase != null;

        public Vector3 CurrentVelocity => _phase?.Velocity ?? Vector3.zero;

        // The current request's Strafe option (a jump or drop keeps the one it started with). Not
        // NavMeshAgent.updateRotation, which jumps and drops also turn off while they move the character.
        public bool IsStrafing => _phase?.IsStrafing(_current) ?? false;

        // True if there is a complete path from the agent's current position, using the area mask of
        // the given mode. Has no side effects on the agent.
        public bool IsLocationAccessible(Vector3 location, bool leisurely = false) =>
            _agent.IsLocationAccessible(location, leisurely);

        // The previous request, if any, is called back straight away, even mid-jump or drop. The new
        // request is followed once the jump or drop lands.
        public void GoTo(DestinationArgs args)
        {
            var previous = _current;
            var wasIdle = _phase == null;
            var redirectedPhase = _phase;
            var redirectInPlace = !wasIdle && _phase.CanInterrupt && _phase.CanRedirectInPlace(args);

            _current = args;

            if (wasIdle)
            {
                Enter(PhaseId.Preparing);
                Activated?.Invoke();
            }
            else if (!_phase.CanInterrupt)
            {
                _restartAfterTraversal = true;
            }
            else if (!redirectInPlace)
            {
                Enter(PhaseId.Preparing);
            }

            // Called back last, so a callback that calls GoTo or Cancel finds everything up to date.
            previous?.Callback(DestinationResult.CancelledByNewDestination);

            // Steer only if the callback didn't start a newer request or cancel: setting the destination
            // twice in one call chain leaves the agent heading for the first of the two.
            if (redirectInPlace && ReferenceEquals(_current, args) && _phase == redirectedPhase)
                redirectedPhase.Redirect(args);
        }

        // The request is called back straight away. Mid-jump or drop, the character still lands first,
        // and stays active until then.
        public void Cancel()
        {
            if (_current == null)
                return;

            if (_phase.CanInterrupt)
            {
                Finish(DestinationResult.Cancelled);
                return;
            }

            var cancelled = _current;
            _current = null;

            cancelled.Callback(DestinationResult.Cancelled);
        }

        internal void Update(float deltaTime)
        {
            if (_phase == null)
            {
                _agent.Disable();
                return;
            }

            if (!_context.CanNavigate && _phase != _paused)
            {
                Apply(PhaseStep.To(PhaseId.Paused));
                return;
            }

            Apply(_phase.Update(_current, deltaTime));
        }

        // While the GameObject is inactive: frees the avoidance priority slot, keeping the trip.
        internal void Suspend() => _agent.Suspend();

        internal void Resume()
        {
            // Cancelled while paused: nothing to resume, so make sure the agent stays disabled.
            if (_phase == null)
            {
                _agent.Disable();
                return;
            }

            _agent.Resume();

            // Unity dropped the agent's path while it was disabled. A jump or drop is moved by hand, so it
            // carries on, and the path is set up again once it lands.
            if (_phase.CanInterrupt)
                Apply(_phase.OnAgentReactivated());
            else
                _restartAfterTraversal = true;
        }

        void Apply(PhaseStep step)
        {
            if (step.IsFinish)
            {
                Finish(step.Result);
                return;
            }

            if (step.IsContinue)
                return;

            var next = step.Next;

            if (!_phase.CanInterrupt)
            {
                // Landed after its request was cancelled; the callback has already been made.
                if (_current == null)
                {
                    Stop();
                    return;
                }

                if (_restartAfterTraversal && next == PhaseId.Walking)
                    next = PhaseId.Preparing;

                _restartAfterTraversal = false;
            }

            Enter(next);
        }

        void Enter(PhaseId id)
        {
            _phase?.Exit();
            _phase = GetPhase(id);
            _phase.Enter(_current);
        }

        // Every change is made before the callback, so a callback that calls GoTo or Cancel starts afresh.
        void Finish(DestinationResult result)
        {
            var finished = _current;
            _current = null;

            Stop();

            finished.Callback(result);
        }

        void Stop()
        {
            _phase.Exit();
            _phase = null;
            _restartAfterTraversal = false;

            Deactivated?.Invoke();
        }

        Phase GetPhase(PhaseId id) => id switch
        {
            PhaseId.Preparing => _preparing,
            PhaseId.Walking => _walking,
            PhaseId.Jumping => _jumping,
            PhaseId.Falling => _falling,
            PhaseId.Paused => _paused,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };

        void OnJumpBegan() => JumpBegan?.Invoke();

        void OnJumpEnded() => JumpEnded?.Invoke();

        void OnFallBegan() => FallBegan?.Invoke();

        void OnFallEnded() => FallEnded?.Invoke();
    }
}