using System;
using UnityEngine;
using ILogger = Strawhenge.Common.Logging.ILogger;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Owns the current request and makes every callback. Phases only move the agent: each update they
    // get the current args and return what should happen next (PhaseStep).
    public class DestinationController
    {
        readonly IDestinationContext _context;
        readonly Agent _agent;
        readonly Preparing _preparing;
        readonly Moving _moving;
        readonly Jumping _jumping;
        readonly Falling _falling;
        readonly Landing _landing;
        readonly Pivoting _pivoting;
        readonly Paused _paused;

        // The request being carried out. Null when there is none, including while a jump or drop
        // finishes after its request was cancelled.
        DestinationArgs _current;

        // What the agent is doing. Null when idle. Can outlive _current: a phase that can't be interrupted
        // (a jump, drop, pivot or landing) always finishes.
        Phase _phase;

        // The request changed, or the agent was reactivated, during a phase that can't be interrupted (a
        // jump, drop, pivot or landing). The agent's path is then stale, so it goes through Preparing rather
        // than Moving once that phase, and any that follows it without a break (a jump's landing), is done.
        bool _restartWhenSettled;

        // Without settings (e.g. in tests), DefaultDestinationSettings is used.
        internal DestinationController(
            IDestinationContext context,
            Agent agent,
            ILogger logger,
            IDestinationSettings settings = null)
        {
            _context = context;
            _agent = agent;

            settings = new CachedDestinationSettings(settings ?? DefaultDestinationSettings.Instance);

            var targets = new TargetResolver(agent, settings);

            _pivoting = new Pivoting(agent, settings, logger);
            _preparing = new Preparing(agent, targets, _pivoting);
            _moving = new Moving(agent, targets, _pivoting);
            _landing = new Landing(agent, settings, logger);
            _jumping = new Jumping(agent, settings, _landing, logger);
            _falling = new Falling(agent, settings, _landing);
            _paused = new Paused(context, agent);

            _jumping.JumpTriggerRequested += OnJumpTriggerRequested;
            _jumping.JumpBegan += OnJumpBegan;
            _jumping.JumpEnded += OnJumpEnded;
            _falling.FallBegan += OnFallBegan;
            _falling.FallEnded += OnFallEnded;
            _landing.LandingRequested += OnLandingRequested;
            _pivoting.PivotRequested += OnPivotRequested;
        }

        public event Action Activated;
        public event Action Deactivated;
        // A jump reached its off-mesh link with DeferJumpTrigger set: it waits there until TriggerJump is
        // called (MovementAnimationSyncScript.OnJumpLaunch does this), or the AnimatorWaitTimeout passes.
        public event Action JumpTriggerRequested;
        public event Action JumpBegan;
        public event Action JumpEnded;
        public event Action FallBegan;
        public event Action FallEnded;

        // A pivot profile matched the turn towards the path, before setting off or on the way: the agent
        // waits until CompletePivot is called (MovementAnimationSyncScript does this), or the settings'
        // AnimatorWaitTimeout passes. Never while strafing.
        public event Action<int> PivotRequested;

        // A jump or drop landed and a landing profile matched: the agent waits until CompleteLanding is
        // called (MovementAnimationSyncScript does this), or the settings' AnimatorWaitTimeout passes.
        public event Action<int> LandingRequested;

        // Stays true until a jump or drop in progress lands, even if its request has already ended.
        public bool IsActive => _phase != null;

        public Vector3 CurrentVelocity => _phase?.Velocity ?? Vector3.zero;

        // The current request's Strafe option (a jump or drop keeps the one it started with). Not
        // NavMeshAgent.updateRotation, which jumps and drops also turn off while they move the character.
        public bool IsStrafing => _phase?.IsStrafing(_current) ?? false;

        // True if there is a complete path from the agent's current position, using the area mask of
        // the given mode. Has no side effects on the agent. Only the exact location counts: unlike an
        // approximate GoTo, this never falls back to the nearest point on the NavMesh.
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
                _restartWhenSettled = true;
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
                Apply(redirectedPhase.Redirect(args));
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

            // A phase that can't be interrupted finishes first: pausing mid-jump would leave the character
            // hanging in the air. The next update pauses once it's done.
            if (!_context.CanNavigate && _phase.CanInterrupt && _phase != _paused)
            {
                Apply(PhaseStep.To(PhaseId.Paused));
                return;
            }

            Apply(_phase.Update(_current, deltaTime));
        }

        // The Animator's replies, passed on by DestinationMovementSource. Ignored unless the current phase
        // is waiting for them.
        internal void TriggerJump()
        {
            if (_phase != null)
                Apply(_phase.OnJumpTriggered());
        }

        internal void CompletePivot(Quaternion rotationDelta)
        {
            if (_phase != null)
                Apply(_phase.OnPivotCompleted(rotationDelta));
        }

        internal void CompleteLanding()
        {
            if (_phase != null)
                Apply(_phase.OnLandingCompleted());
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
                _restartWhenSettled = true;
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
                // Handing over to another phase that can't be interrupted (a jump's landing): it plays even
                // if the request was cancelled, and a restart waits until it's done too.
                if (!GetPhase(next).CanInterrupt)
                {
                    Enter(next);
                    return;
                }

                // Done after its request was cancelled; the callback has already been made.
                if (_current == null)
                {
                    Stop();
                    return;
                }

                if (_restartWhenSettled && next == PhaseId.Moving)
                    next = PhaseId.Preparing;

                _restartWhenSettled = false;
            }

            Enter(next);
        }

        void Enter(PhaseId id)
        {
            var next = GetPhase(id);

            // A run of phases that can't be interrupted keeps the facing the first one started with, even if
            // the request changed or was cancelled meanwhile.
            var strafe = _phase != null && !_phase.CanInterrupt && !next.CanInterrupt
                ? _phase.IsStrafing(_current)
                : _current.Strafe;

            _phase?.Exit();
            _phase = next;
            _phase.Enter(_current, strafe);
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
            _restartWhenSettled = false;

            Deactivated?.Invoke();
        }

        Phase GetPhase(PhaseId id) => id switch
        {
            PhaseId.Preparing => _preparing,
            PhaseId.Moving => _moving,
            PhaseId.Jumping => _jumping,
            PhaseId.Falling => _falling,
            PhaseId.Landing => _landing,
            PhaseId.Pivoting => _pivoting,
            PhaseId.Paused => _paused,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };

        void OnJumpTriggerRequested() => JumpTriggerRequested?.Invoke();

        void OnJumpBegan() => JumpBegan?.Invoke();

        void OnJumpEnded() => JumpEnded?.Invoke();

        void OnFallBegan() => FallBegan?.Invoke();

        void OnFallEnded() => FallEnded?.Invoke();

        void OnLandingRequested(int landingId) => LandingRequested?.Invoke(landingId);

        void OnPivotRequested(int pivotId) => PivotRequested?.Invoke(pivotId);
    }
}