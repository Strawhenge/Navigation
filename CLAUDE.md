# CLAUDE.md

Guidance for Claude when working in this repository.

## What this is

**Strawhenge Navigation** is a Unity package for character movement. It covers two ways of moving a humanoid character, plus the animation glue between them:

- **Destination** (AI / point-and-click): drives a `NavMeshAgent` to a target through a small state machine. It handles off-mesh links (jump across, drop down), "can't navigate right now" pauses, and result callbacks.
- **Locomotion** (direct control): a `CharacterController`-based mover with walk/run/sprint, acceleration, turning, jumping with coyote time, gravity and fall detection. Animation-driven **pivots** (turn-on-the-spot / 180° turns) and **landings** are selected from data-driven profiles.
- **Movement sources + animation sync**: `IMovementSource` abstracts "whatever is moving the character right now". `MovementSourceManagerScript` picks the first active source, and `MovementAnimationSyncScript` feeds its velocity and events into an `Animator`.

The package is a work in progress. See "Known gaps" below.

Part of the Strawhenge family of packages (Common, FunctionalUtilities, Inventory, Interactions, ...). GitHub: `Strawhenge/Navigation`. The working branch is `master`; `origin/main` only holds the initial commit.

## Repository layout

```
Navigation/                              <- git root
└── Strawhenge.Navigation.Unity/         <- Unity project (Unity 2021.3.17f1)
    ├── Assets/Package/                  <- THE PACKAGE (what ships)
    │   ├── Runtime/                     <- asmdef: Strawhenge.Navigation.Unity
    │   │   ├── Destination/             <- NavMesh-driven movement (namespace ...Unity.Destination)
    │   │   │   ├── States/              <- Idle, PrepareGoing, Going, Jumping, Falling, CannotNavigate
    │   │   │   ├── DestinationArgs/     <- args + fluent builder
    │   │   │   ├── Context/             <- IDestinationContext (CanNavigate gate)
    │   │   │   ├── Modes/               <- INavMeshAgentModes (area masks for Default / Leisurely), FixedNavMeshAgentModes
    │   │   │   └── PriorityManager/     <- avoidance-priority allocation across agents
    │   │   ├── Locomotion/              <- LocomotionScript, toggle, settings
    │   │   ├── Pivots/                  <- pivot ids + profiles (speed range + angle ranges)
    │   │   ├── Landings/                <- landing ids + profiles (speed range + fall distance range)
    │   │   ├── MovementSource/          <- IMovementSource, Destination/Locomotion adapters, manager
    │   │   ├── Animation/               <- MovementAnimationSyncScript, StateMachineBehaviours, parameters
    │   │   └── AssemblyInfo.cs          <- InternalsVisibleTo the test assembly
    │   ├── Editor/                      <- asmdef: Strawhenge.Navigation.Unity.Editor (editor-only)
    │   └── Tests/                       <- asmdef: Strawhenge.Navigation.Unity.Tests (PlayMode tests)
    │       ├── Fakes/                   <- Fake... test doubles, one per file
    │       └── Helpers/                 <- shared test setup (e.g. LocomotionTestCharacter)
    └── Assets/Development/              <- dev/test harness, NOT part of the package
        ├── Scenes/Main.unity            <- test scene (terrain, ramps, steps, NavMesh)
        ├── Scripts/                     <- ControlsScript (Input System -> LocomotionScript), CameraScript
        ├── Character.controller         <- reference Animator Controller
        ├── ScriptableObjects/           <- example locomotion settings, pivots, landings, agent modes
        └── Animations/, Y Bot.fbx       <- Mixamo assets
```

## Dependencies

From `Packages/manifest.json` (git URLs pinned to each repo's `release` branch):
- `com.strawhenge.common`: used heavily, including `Strawhenge.Common.Unity.Helpers.ComponentRefHelper`, `Strawhenge.Common.Unity.Serialization` (`SerializedSource<,,>`, `NavMeshAreaMask`) and `Strawhenge.Common.Unity.AnimatorParameter`.
- `com.strawhenge.functionalutilities`: in the manifest, but the runtime code doesn't currently reference it directly.
- Unity: AI (NavMesh), Animation, Physics, Input System (Development only).

Assembly definitions reference dependencies by GUID, and the runtime asmdef has `autoReferenced: false`.

## Architecture notes

### Destination
- `DestinationScript` (MonoBehaviour) builds a `DestinationController` lazily on first access or in `Awake`, then ticks it from `Update`. Consumers use `destinationScript.DestinationController`.
- Public API: `GoTo(DestinationArgs)`, `Cancel()`, `IsLocationAccessible(...)`, `IsActive`, `CurrentVelocity`, `IsStrafing`, and the events `Activated/Deactivated/JumpBegan/JumpEnded/FallBegan/FallEnded`.
- Args come from `DestinationArgs.GoToExactly(loc)` / `GoToApproximately(loc)`, followed by builder calls (`WithSpeed`, `WithinDistance`, `Leisurely`, `Strafe`, `FromStartingPoint`, `Callback`) and `Build()`. The extension methods in `DestinationControllerExtensions` are a shorter alternative.
- Every request ends with **exactly one** callback: `Arrived`, `Inaccessible`, `Cancelled` or `CancelledByNewDestination`. Keep this invariant whenever you touch the states.
- **Update state, then invoke callbacks.** Callbacks often call `GoTo` / `Cancel` straight away (e.g. a patrol going to its next waypoint), so finish every change first (`CurrentArgs`, `_pendingArgs`, `ChangeState(...)`) and run callbacks last. Otherwise the nested call reaches the old state: the old request is called back again (endlessly, if its callback keeps redirecting) and the newest request is lost. In `Jumping` / `Falling`, `TryResolveQueuedRequest()` applies a queued `Cancel` / `GoTo` this way. Steering the `NavMeshAgent` for a request (e.g. `Going.GoTo` setting `destination`) happens after the callbacks, and only if that request is still current (`State.IsCurrent` and the same `CurrentArgs`). Setting the destination twice in one call chain leaves the agent heading for the first one.
- A redirect while walking (`Going.GoTo`) applies the new request's options in place (`Strafe` → `updateRotation`, `Leisurely` → `Agent.Enable` when the mode changes, speed, destination) so the agent keeps moving. Only a redirect with `FromStartingPoint` goes back through `PrepareGoing`, which warps and recalculates. Any new request option must be applied in both places.
- States are plain classes that share a `StatesContainer`. They switch with `ChangeState(...)` and hand over `CurrentArgs` to the next state before switching. While Jumping or Falling is mid-traversal, a new `GoTo` or `Cancel` is queued (`_pendingArgs` / `_cancelRequested`) and resolved when the traversal ends.
- The `NavMeshAgent` stays **disabled** while idle and is enabled in `PrepareGoing` through `Agent.Enable(leisurely)`, which also reserves an avoidance priority and applies the area-mask mode. Every `DestinationScript` shares `SingleAgentPriorityManager.Instance`. It's never cleared (clearing on scene unload broke additive scenes): slots are checked with Unity's `== null`, which is also true for destroyed agents, so their slots are reused automatically. Deactivating a character's GameObject mid-trip calls `Agent.Suspend()` (through `DestinationScript.OnDisable`), which frees its slot. Reactivating calls `Resume()`, which reserves a slot again for the mode in use (`IsLeisurely`) and calls `State.OnAgentReactivated()`. Unity resets a `NavMeshAgent` (its path is lost) when its GameObject is deactivated, so `Going` restarts the same request through `PrepareGoing`, and `Jumping` / `Falling` finish the traversal and then go through `PrepareGoing`. A state that relies on the agent's path must override `OnAgentReactivated`. Disabling only the `DestinationScript` component isn't supported yet: the `NavMeshAgent` keeps walking with nothing ticking the controller.
- `Leisurely` gives low avoidance priority plus the "leisurely" area mask. `Strafe` sets `updateRotation = false`, and a strafing character keeps its facing through jumps. `IsStrafing` reports the request's `Strafe` option (`State.IsStrafing`), not `updateRotation`, which `Jumping` / `Falling` also turn off while they move the character by hand. `Agent.GetAreaMask(leisurely)` is the single place a mode's mask is chosen; `INavMeshAgentModes` only supplies the masks. Without a modes asset, `DestinationScript` uses `FixedNavMeshAgentModes` with the NavMeshAgent's inspector mask.
- `IsLocationAccessible(location, leisurely = false)` queries the NavMesh directly (`NavMesh.SamplePosition` within one agent height, then `NavMesh.CalculatePath`) with the mode's area mask. It must never enable, move or reconfigure the agent, and only a **complete** path counts as accessible. (A `CanGoTo(DestinationArgs)` that also accepts partial paths for approximate requests may come later.)
- States must not leave lasting changes to the agent's own settings (e.g. `stoppingDistance`). `Going` treats anything within `MinArrivalDistance` (0.1 m) as arrived when the stopping distance is smaller, since `remainingDistance` can stay just above 0.
- Jumps and drops along off-mesh links are moved by hand (`updatePosition = false`). The speed and gravity constants for this are hard-coded TODOs in `Jumping` / `Falling`.

### Locomotion
- `LocomotionScript` is driven by `Move(Vector3 worldDirection)`, `Jump()` and the `Walk` / `Sprint` / `Strafe` flags.
- Coyote time is time since the `CharacterController` was last grounded (`_timeSinceGrounded`), whatever the drop. Falling (`FallBegan`, fall landings) is separate and starts only after dropping `FallDistance`, so stairs and small bumps don't trigger it.
- Settings come from `SerializedSource<ILocomotionSettings, SerializedLocomotionSettings, LocomotionSettingsScriptableObject>`, so they can be inline or an asset. If neither is set, `DefaultLocomotionSettings` is used. Both settings classes build their profile arrays with `ProfileSources.Resolve(...)`. It skips unset sources, and skips (with a warning) pivot or landing profiles whose `Id` is null, which means no pivot / landing asset is assigned. An incomplete profile never matches. Implementations must return null with an explicit `asset != null ? asset.Id : null` check: `asset?.Id` skips Unity's null check.
- The settings classes don't cache: their profile lists are resolved on every read. `LocomotionScript` always reads settings through `CachedLocomotionSettings`, which passes values through and caches the four profile arrays. Profile ranges are `ValueRange` (see below), so matching (`PivotProfileExtensions.Matches` / `IsInAngleRange`, `LandingProfileExtensions.Matches`) allocates nothing. `OnValidate` on the settings asset, the pivot / landing profile assets and `LocomotionScript` calls `CachedLocomotionSettings.Invalidate()`, so in the Editor any edit rebuilds the lists on the next read; in builds they're resolved once. `LocomotionScript.OnValidate` also re-resolves its settings in play mode (swapped asset, edited inline settings), unless they were given through `SetSettings`.
- Ranges are `ValueRange` (`Runtime/Ranges`): a serializable struct with `Min`, `Max` and `Contains(value)` (inclusive; max below min contains nothing). Reading or checking it never allocates. `ValueRangePropertyDrawer` draws it on one line and warns when max < min. Its fields are `_min` / `_max`, the same as Common's `SerializedFloatRange`, so assets saved with that type keep their values. Don't use Common's `FloatRange` / `SerializedFloatRange` here: `FloatRange` is a class and `SerializedFloatRange.Value` allocates one on every read.
- When a pivot completes with no rotation (the pivot fallback, or a clip without root rotation), `LocomotionScript` skips pivots until the turn leaves every pivot's angle range (`_suppressPivotsUntilTurned`) and turns normally. Otherwise the same pivot would match again every frame and the character would never turn.
- Pivots and landings are a handshake with the Animator. Locomotion raises `PivotRequested(id)` / `LandingRequested(id)` and blocks movement or rotation until `CompletePivot(rotationDelta)` / `CompleteLanding()` is called back. Those calls come from `MovementAnimationSyncScript` via `PivotStateMachineBehaviour` / `LandingStateMachineBehaviour` (`OnStateMachineExit`).
- When `DeferJumpTrigger` is set, the jump waits for an animation event that calls `MovementAnimationSyncScript.OnJumpLaunch()` → `TriggerJump()`.
- While `LocomotionScript` is disabled, the Animator's completion calls go to whichever movement source is active instead, so they never arrive. `OnDisable` therefore calls `CancelAnimatorWaits()` to clear the pivot, landing and jump-trigger waits (and any queued stationary pivot or jump not yet applied). Any new "wait for the Animator" flag must be reset there too.
- `LocomotionToggleScript` turns `LocomotionScript` off while the destination controller is active, or when a `BaseLocomotionToggleConditionsScript` says so. It applies the starting state in `Start` (after every `Awake`), then follows the change events.

### Animation contract
`MovementAnimationSyncScript` expects these Animator parameter names (see `AnimatorParameters`): `Move Speed`, `Strafing`, `Strafe X`, `Strafe Y`, `Falling`, `Jump`, `Jumping`, `Pivot`, `Pivot ID`, `Pivot Right Foot`, `Landing ID`, `Land`. The controller must contain a `PivotStateMachineBehaviour` and a `LandingStateMachineBehaviour`. `Assets/Development/Character.controller` is the reference setup. Renaming a parameter breaks every consumer's controller.

Setup problems never crash and never leave locomotion waiting for an animation that can't happen. `MovementAnimationSyncScript` warns once in `Awake` and falls back:
- **No Animator:** movement plays without animation, and a deferred jump triggers straight away.
- **No pivot or landing behaviour:** that request is completed straight away (a pivot with `Quaternion.identity`).
- **Non-humanoid rig:** no foot bones, so `Pivot Right Foot` stays false.

Two timeouts stop a pivot, landing or deferred jump from waiting forever. Both are serialized fields, so they can be tuned per character:
- **`MovementAnimationSyncScript._animationStartTimeout` (0.3 s):** the pivot or landing animation must *start* in this time. The behaviours raise `Started` from `OnStateEnter`, not `OnStateMachineEnter`, which transitions straight to a state skip. Otherwise it's skipped: the trigger is reset, root motion turned off, the request completed (a pivot with `Quaternion.identity`), and a warning logged once per ID. This is the usual "no transition for this ID" case. It only counts while the Animator is active. When `MovementSourceManagerScript.ActiveSourceChanged` fires, a pending animation is abandoned without completing it.
- **`LocomotionScript._animatorWaitTimeout` (2 s):** a safety net in `LocomotionScript` itself, for waits nobody completes (no sync script, nothing wired up, an animation that never finishes). Pivots and landings are completed, a deferred jump is **cancelled** rather than launched late, and a warning is logged. It must be longer than the longest pivot or landing clip.

## Code conventions

Follow the existing style. The ReSharper settings are in `Strawhenge.Navigation.Unity.sln.DotSettings`.
- Leave out `private`/`internal` where they're the default (`readonly Foo _foo;`, `class Agent`).
- Serialized fields are `[SerializeField] T _camelCase;`, never public fields. Expose them through get-only properties.
- **One type per file**, named after the type (`FakePivotProfile.cs` holds `FakePivotProfile`). Don't put several classes, interfaces or enums in one file, and don't nest classes inside other classes. The only exception is a private implementation type kept in its own partial-class file, as `DestinationArgs.Builder.cs` does for `DestinationArgs.ArgsBuilder`.
- Naming suffixes: `...Script` for MonoBehaviours, `...ScriptableObject` for ScriptableObjects, `Serialized...` for `[Serializable]` inline equivalents. Interfaces sit behind all three (`IPivotProfile`, `ILandingProfile`, `ILocomotionSettings`).
- Null objects: `NullX` with `static Instance { get; } = new();` and a private ctor, used instead of null checks (`NullDestinationContext`, `NullMovementSource`). When "nothing" still has to return a value, use a fallback that keeps the existing behaviour instead (e.g. `FixedNavMeshAgentModes` with the agent's own area mask).
- Resolve component references with `ComponentRefHelper.EnsureHierarchyComponent` / `EnsureRootHierarchyComponent` from Common. Warn with `Debug.LogWarning(..., this)` and fall back to defaults rather than throwing.
- Keep logic in plain C# classes and keep MonoBehaviours as thin wiring. Use `internal` for things only the package needs (e.g. `DestinationController.Update`).
- Namespaces: `Strawhenge.Navigation.Unity` for most code, `Strawhenge.Navigation.Unity.Destination` for the Destination folder, `...Unity.Editor` for the editor. Folders below that level don't add namespace segments, so ignore the CheckNamespace warning.
- Usings are not sorted System-first. The language level is the one Unity 2021.3 supports (C# 9: `new()`, `??=` and `is not` are fine; C# 10+ features like file-scoped namespaces and global usings are not).
- Avoid per-frame allocations: reuse `NavMeshPath`; use `NavMeshAgent.pathStatus`, not `path.status` (the `path` getter allocates); read settings through `CachedLocomotionSettings`, never the settings classes directly; use `ValueRange`, not Common's `FloatRange`; no LINQ or lambdas in per-frame code.
- Commit messages are lowercase, in the form `area; what changed`, e.g. `refactor locomotion settings; ...`.

## Working safely in a Unity project

- **Every asset and script has a `.meta` file holding its GUID.** When you move or rename a `.cs` file, move or rename its `.meta` along with it. Without that, references in scenes, prefabs and assets break silently. For new files Unity generates the `.meta` on import, so don't invent one.
- **Renaming a `[SerializeField]` field drops the data saved in existing assets** (`.asset`, `.unity`, `.controller`). Add `[UnityEngine.Serialization.FormerlySerializedAs("_old")]`, or tell the user the assets must be re-assigned.
- Renaming or moving a MonoBehaviour/ScriptableObject class is safe only if the file name still matches the class name and the `.meta` goes with it.
- Don't edit `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/` or the generated `*.csproj`/`*.sln` files. They're regenerated (and git-ignored).
- Hand-edit YAML assets (`.unity`, `.controller`, `.asset`) only when you have to, and keep the change minimal.
- Nothing can be compiled or run outside the Unity Editor here. After a change, ask the user to let Unity recompile, check the Console and run the tests (see Testing). For manual checks, use `Assets/Development/Scenes/Main.unity`. The `DestinationScript` inspector has play-mode buttons (GoToExactly / GoToApproximately / IsLocationAccessible / Cancel), and the `LocomotionScript` inspector shows private fields live.
- Package code must not reference anything in `Assets/Development`. Dependencies flow Development → Package only.

## Testing

PlayMode tests live in `Assets/Package/Tests/` (asmdef `Strawhenge.Navigation.Unity.Tests`). The user runs them from **Window → General → Test Runner → PlayMode → Run All**. Claude can't run Unity, so ask the user for the results.
- The asmdef follows Common's test assembly: `UNITY_INCLUDE_TESTS` define constraint (projects that consume the package don't compile it), `overrideReferences: true`, and precompiled `nunit.framework.dll`, `Strawhenge.Common.dll` and `FunctionalUtilities.dll`.
- Build objects in code rather than loading scenes or prefabs. Create the GameObject inactive, add its components, configure them, then activate it so `Awake` sees the configuration. Destroy everything in `[UnityTearDown]` and wait a frame.
- Test hooks are `internal` and exposed through `Runtime/AssemblyInfo.cs` (`InternalsVisibleTo`). `LocomotionScript.SetCharacterController(...)` and `SetSettings(...)` exist for this; `Awake` keeps settings that were already set. Add similar small hooks rather than using reflection.
- Fakes go in `Tests/Fakes/` and setup helpers in `Tests/Helpers/`, one per file, even when only one test class uses them. Test classes hold only their tests and private setup methods. Current fakes: `FakeLocomotionSettings` (defaults to `DefaultLocomotionSettings`, so each test sets only what it needs), `FakePivotProfile`, `FakeLandingProfile`, `FakeNavMeshAgentModes`. Current helpers: `LocomotionTestCharacter.Create(...)`, `TestNavMesh.Build(...)` / `TestNavMesh.Box(...)` (a NavMesh from flat boxes; older destination tests still build theirs inline).
- Assert behaviour through public API and events (position, `PivotRequested`, `JumpBegan`, ...), not private fields. Check that each test's setup worked ("Setup failed: ..."), and back any "doesn't happen" assertion with a baseline test showing it does happen normally.
- Bug-fix workflow: put the failing tests on a `test/...` branch (PR into `master`), and the fix on a `fix/...` branch from it (PR into the test branch). Merge the fix PR first, then the test PR, so `master` never has failing tests.
- Unity generates `.meta` files for new files and folders. Commit them once Unity has imported the files; don't hand-write them.

## Git workflow

- Work on a branch and open a pull request for the user to review. Don't merge it yourself.
- The repo stores LF line endings, and Windows checkouts use `core.autocrlf=true`, so files on disk are CRLF. Some files have a UTF-8 BOM. Preserve both when editing.

## Known gaps / TODOs

- **No `package.json` in `Assets/Package`.** Other projects can't consume this yet via a git URL (`?path=/Strawhenge.Navigation.Unity/Assets/Package#release`) the way the other Strawhenge packages are. It also needs a README.
- Jump and fall speed and gravity for destination off-mesh links are hard-coded constants (`Jumping.JumpSpeed`, `Falling.HorizontalSpeed`, `Falling.FallAcceleration`).
- The destination path never raises `PivotRequested` / `LandingRequested` / `JumpTriggerRequested`, and `Jumping.Velocity` is always zero. AI-driven characters therefore get no pivot or landing animations, and their speed parameter drops to 0 mid-jump.
- `SerializedLocomotionSettings` and `LocomotionSettingsScriptableObject` duplicate the same fields and logic.
- `AnimatorExtensions.SetFloat` and `PrivateFieldInspector` are marked "move to Common". `PivotScriptableObject` is marked "remove from asset menu / generate via editor tool".
- The reference controller has `Pivot Angle` / `Pivot Speed` parameters that the code doesn't use.
- `ValueRange` has a TODO to move into Strawhenge Common in place of `FloatRange` / `SerializedFloatRange`, so other packages stop allocating too. Navigation then uses it from Common.
