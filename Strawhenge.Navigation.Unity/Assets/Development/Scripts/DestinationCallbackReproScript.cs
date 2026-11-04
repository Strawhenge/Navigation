using Strawhenge.Navigation.Unity.Destination;
using System;
using UnityEngine;

namespace Development
{
    // Manual repro for review item 3: destination callbacks that start a new request.
    // In Play mode, run the scenarios from this component's ⋮ menu (or right-click its header),
    // then read the "[Repro]" lines in the Console. Every request should get exactly one callback.
    public class DestinationCallbackReproScript : MonoBehaviour
    {
        [SerializeField] DestinationScript _destination;
        [SerializeField] Transform _pointA;
        [SerializeField] Transform _pointB;
        [Tooltip("Points either side of a jump-across link (e.g. the Stream). Start the agent near the first.")]
        [SerializeField] Transform _linkNearSide;
        [SerializeField] Transform _linkFarSide;
        [SerializeField] float _speed = 2f;
        [Tooltip("Caps the chained callbacks so the bug can't overflow the stack and crash the editor.")]
        [SerializeField] int _maxChain = 5;

        int _nextId;
        int _chainCount;
        bool _cancelOnNextJumpOrFall;

        DestinationController Controller => _destination.DestinationController;

        void Awake()
        {
            Controller.JumpBegan += OnJumpOrFallBegan;
            Controller.FallBegan += OnJumpOrFallBegan;
        }

        void OnDestroy()
        {
            if (_destination == null) return;

            Controller.JumpBegan -= OnJumpOrFallBegan;
            Controller.FallBegan -= OnJumpOrFallBegan;
        }

        // Going.GoTo: a patrol whose callback re-issues GoTo on every result is redirected mid-walk.
        [ContextMenu("1. Patrol to A, redirect to B after 1 second")]
        void PatrolThenRedirectLater()
        {
            if (!CheckPlaying()) return;

            StartPatrol();
            Invoke(nameof(RedirectToB), 1f);
        }

        // PrepareGoing.GoTo: the same, but redirected in the same frame, before the agent starts moving.
        [ContextMenu("2. Patrol to A, redirect to B immediately")]
        void PatrolThenRedirectNow()
        {
            if (!CheckPlaying()) return;

            StartPatrol();
            RedirectToB();
        }

        // Jumping / Falling: cancel mid-jump, then start a new request from the Cancelled callback.
        [ContextMenu("3. Go across link, cancel mid-jump, re-route to B in callback")]
        void CancelMidJumpThenReRoute()
        {
            if (!CheckPlaying()) return;

            _cancelOnNextJumpOrFall = true;

            Go(_linkFarSide.position, "cross link", result =>
            {
                if (result.IsCancelled && !result.HasNewDestination)
                    Go(_pointB.position, "re-route after cancel");
            });
        }

        // Run after scenario 3: crosses the link back the other way.
        [ContextMenu("4. Go back across link (after 3)")]
        void CrossLinkBack()
        {
            if (!CheckPlaying()) return;

            Go(_linkNearSide.position, "cross link back");
        }

        void StartPatrol()
        {
            _chainCount = 0;
            Go(_pointA.position, "patrol", OnPatrolResult);
        }

        void OnPatrolResult(DestinationResult result)
        {
            // Like `.Callback(GoToNextWaypoint)`: reacts to every result, including CancelledByNewDestination.
            if (_chainCount >= _maxChain)
            {
                Debug.LogWarning($"[Repro] Chain cap ({_maxChain}) reached - stopping the patrol.");
                return;
            }

            _chainCount++;
            Go(_pointA.position, $"patrol (chained {_chainCount})", OnPatrolResult);
        }

        void RedirectToB() => Go(_pointB.position, "redirect");

        void Go(Vector3 location, string label, Action<DestinationResult> onResult = null)
        {
            var id = ++_nextId;
            Debug.Log($"[Repro] #{id} {label}: GoTo {location}");

            Controller.GoTo(
                DestinationArgs
                    .GoToApproximately(location)
                    .WithSpeed(_speed)
                    .Callback(result =>
                    {
                        Debug.Log($"[Repro] #{id} {label}: callback {Describe(result)}");
                        onResult?.Invoke(result);
                    })
                    .Build());
        }

        void OnJumpOrFallBegan()
        {
            if (!_cancelOnNextJumpOrFall) return;
            _cancelOnNextJumpOrFall = false;

            Debug.Log("[Repro] Jump/fall began - calling Cancel()");
            Controller.Cancel();
        }

        static string Describe(DestinationResult result)
        {
            if (result.IsAtDestination) return "Arrived";
            if (result.IsDestinationInaccessible) return "Inaccessible";
            if (result.HasNewDestination) return "CancelledByNewDestination";
            return "Cancelled";
        }

        static bool CheckPlaying()
        {
            if (Application.isPlaying) return true;

            Debug.LogWarning("[Repro] Enter Play mode first.");
            return false;
        }
    }
}
