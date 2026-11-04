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
    │   │   │   ├── Modes/               <- INavMeshAgentModes (area masks for Default / Leisurely)
    │   │   │   └── PriorityManager/     <- avoidance-priority allocation across agents
    │   │   ├── Locomotion/              <- LocomotionScript, toggle, settings
    │   │   ├── Pivots/                  <- pivot ids + profiles (speed range + angle ranges)
    │   │   ├── Landings/                <- landing ids + profiles (speed range + fall distance range)
    │   │   ├── MovementSource/          <- IMovementSource, Destination/Locomotion adapters, manager
    │   │   ├── Animation/               <- MovementAnimationSyncScript, StateMachineBehaviours, parameters
    │   │   └── AssemblyInfo.cs          <- InternalsVisibleTo the test assembly
    │   ├── Editor/                      <- asmdef: Strawhenge.Navigation.Unity.Editor (editor-only)
    │   └── Tests/                       <- asmdef: Strawhenge.Navigation.Unity.Tests (PlayMode tests)
    └── Assets/Development/              <- dev/test harness, NOT part of the package
        ├── Scenes/Main.unity            <- test scene (terrain, ramps, steps, NavMesh)
        ├── Scripts/                     <- ControlsScript (Input System -> LocomotionScript), CameraScript
        ├── Character.controller         <- reference Animator Controller
        ├── ScriptableObjects/           <- example locomotion settings, pivots, landings, agent modes
        └── Animations/, Y Bot.fbx       <- Mixamo assets
```

## Dependencies

From `Packages/manifest.json` (git URLs pinned to each repo's `release` branch):
- `com.strawhenge.common`: used heavily, including `Strawhenge.Common.Unity.Helpers.ComponentRefHelper`, `Strawhenge.Common.Unity.Serialization` (`SerializedSource<,,>`, `SerializedFloatRange`, `NavMeshAreaMask`), `Strawhenge.Common.Ranges.FloatRange` and `Strawhenge.Common.Unity.AnimatorParameter`.
- `com.strawhenge.functionalutilities`: in the manifest, but the runtime code doesn't currently reference it directly.
- Unity: AI (NavMesh), Animation, Physics, Input System (Development only).

Assembly definitions reference dependencies by GUID, and the runtime asmdef has `autoReferenced: false`.

## Architecture notes

### Destination
- `DestinationScript` (MonoBehaviour) builds a `DestinationController` lazily on first access or in `Awake`, then ticks it from `Update`. Consumers use `destinationScript.DestinationController`.
- Public API: `GoTo(DestinationArgs)`, `Cancel()`, `IsLocationAccessible(...)`, `IsActive`, `CurrentVelocity`, `IsStrafing`, and the events `Activated/Deactivated/JumpBegan/JumpEnded/FallBegan/FallEnded`.
- Args come from `DestinationArgs.GoToExactly(loc)` / `GoToApproximately(loc)`, followed by builder calls (`WithSpeed`, `WithinDistance`, `Leisurely`, `Strafe`, `FromStartingPoint`, `Callback`) and `Build()`. The extension methods in `DestinationControllerExtensions` are a shorter alternative.
- Every request ends with **exactly one** callback: `Arrived`, `Inaccessible`, `Cancelled` or `CancelledByNewDestination`. Keep this invariant whenever you touch the states.
- States are plain classes that share a `StatesContainer`. They switch with `ChangeState(...)` and hand over `CurrentArgs` to the next state before switching. While Jumping or Falling is mid-traversal, a new `GoTo` or `Cancel` is queued (`_pendingArgs` / `_cancelRequested`) and resolved when the traversal ends.
- The `NavMeshAgent` stays **disabled** while idle and is enabled in `PrepareGoing` through `Agent.Enable(leisurely)`, which also reserves an avoidance priority and applies the area-mask mode.
- `Leisurely` gives low avoidance priority plus the "leisurely" area mask. `Strafe` sets `updateRotation = false`.
- Jumps and drops along off-mesh links are moved by hand (`updatePosition = false`). The speed and gravity constants for this are hard-coded TODOs in `Jumping` / `Falling`.

### Locomotion
- `LocomotionScript` is driven by `Move(Vector3 worldDirection)`, `Jump()` and the `Walk` / `Sprint` / `Strafe` flags.
- Settings come from `SerializedSource<ILocomotionSettings, SerializedLocomotionSettings, LocomotionSettingsScriptableObject>`, so they can be inline or an asset. If neither is set, `DefaultLocomotionSettings` is used.
- Pivots and landings are a handshake with the Animator. Locomotion raises `PivotRequested(id)` / `LandingRequested(id)` and blocks movement or rotation until `CompletePivot(rotationDelta)` / `CompleteLanding()` is called back. Those calls come from `MovementAnimationSyncScript` via `PivotStateMachineBehaviour` / `LandingStateMachineBehaviour` (`OnStateMachineExit`).
- When `DeferJumpTrigger` is set, the jump waits for an animation event that calls `MovementAnimationSyncScript.OnJumpLaunch()` → `TriggerJump()`.
- While `LocomotionScript` is disabled, the Animator's completion calls go to whichever movement source is active instead, so they never arrive. `OnDisable` therefore calls `CancelAnimatorWaits()` to clear the pivot, landing and jump-trigger waits (and any queued stationary pivot or jump not yet applied). Any new "wait for the Animator" flag must be reset there too.
- `LocomotionToggleScript` turns `LocomotionScript` off while the destination controller is active, or when a `BaseLocomotionToggleConditionsScript` says so.

### Animation contract
`MovementAnimationSyncScript` expects these Animator parameter names (see `AnimatorParameters`): `Move Speed`, `Strafing`, `Strafe X`, `Strafe Y`, `Falling`, `Jump`, `Jumping`, `Pivot`, `Pivot ID`, `Pivot Right Foot`, `Landing ID`, `Land`. The controller must contain a `PivotStateMachineBehaviour` and a `LandingStateMachineBehaviour`. `Assets/Development/Character.controller` is the reference setup. Renaming a parameter breaks every consumer's controller.

## Code conventions

Follow the existing style. The ReSharper settings are in `Strawhenge.Navigation.Unity.sln.DotSettings`.
- Leave out `private`/`internal` where they're the default (`readonly Foo _foo;`, `class Agent`).
- Serialized fields are `[SerializeField] T _camelCase;`, never public fields. Expose them through get-only properties.
- Naming suffixes: `...Script` for MonoBehaviours, `...ScriptableObject` for ScriptableObjects, `Serialized...` for `[Serializable]` inline equivalents. Interfaces sit behind all three (`IPivotProfile`, `ILandingProfile`, `ILocomotionSettings`).
- Null objects: `NullX` with `static Instance { get; } = new();` and a private ctor, used instead of null checks (`NullDestinationContext`, `NullNavMeshAgentModes`, `NullMovementSource`).
- Resolve component references with `ComponentRefHelper.EnsureHierarchyComponent` / `EnsureRootHierarchyComponent` from Common. Warn with `Debug.LogWarning(..., this)` and fall back to defaults rather than throwing.
- Keep logic in plain C# classes and keep MonoBehaviours as thin wiring. Use `internal` for things only the package needs (e.g. `DestinationController.Update`).
- Namespaces: `Strawhenge.Navigation.Unity` for most code, `Strawhenge.Navigation.Unity.Destination` for the Destination folder, `...Unity.Editor` for the editor. Folders below that level don't add namespace segments, so ignore the CheckNamespace warning.
- Usings are not sorted System-first. The language level is the one Unity 2021.3 supports (C# 9: `new()`, `??=` and `is not` are fine; C# 10+ features like file-scoped namespaces and global usings are not).
- Avoid per-frame allocations: reuse `NavMeshPath` and cache profile arrays.
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
- The asmdef follows Common's test assembly: `UNITY_INCLUDE_TESTS` define constraint (projects that consume the package don't compile it), `overrideReferences: true`, and precompiled `nunit.framework.dll`, `Strawhenge.Common.dll` (where `FloatRange` lives) and `FunctionalUtilities.dll`.
- Build objects in code rather than loading scenes or prefabs. Create the GameObject inactive, add its components, configure them, then activate it so `Awake` sees the configuration. Destroy everything in `[UnityTearDown]` and wait a frame.
- Test hooks are `internal` and exposed through `Runtime/AssemblyInfo.cs` (`InternalsVisibleTo`). `LocomotionScript.SetCharacterController(...)` and `SetSettings(...)` exist for this; `Awake` keeps settings that were already set. Add similar small hooks rather than using reflection.
- Fakes (`FakeLocomotionSettings`, `FakePivotProfile`, `FakeLandingProfile`) are nested in the test class. Settings default to `DefaultLocomotionSettings`, so each test sets only what it needs.
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
