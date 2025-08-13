AGENTS Instructions:

* Unity scripts should be attached to objects if you want them to be part of the Unity lifecycle.
* Branch names must be strictly in English.
* Comments in Unity code must be written in English.
* Comments in Delphi code must be written in Russian.
* Follow the existing coding style, including variable and constant naming.
* Always use an explicit == null comparison to check for null on UnityEngine.Object and its derivatives. Avoid modern operators like ?., ??, ??= and the is null pattern, as they do not recognize Unity's "destroyed" object state and will cause a MissingReferenceException. For interface variables, use the safe cast: if (myInterface as UnityEngine.Object == null).

### Critical Guideline for Asynchronous Code in Unity

**Directive:** You **MUST NOT** declare Unity lifecycle methods as `async void`. This is a critical anti-pattern in Unity development and introduces severe instability.

**1. Forbidden Method Signatures:**
Never modify engine-invoked methods to be asynchronous. This includes, but is not limited to:
- `private async void Awake()`
- `private async void Start()`
- `private async void OnEnable()`
- `private async void Update()`
- `private async void LateUpdate()`
- `private async void FixedUpdate()`
- `private async void OnGUI()`

**2. Inherent Problems & Glitches:**
Generating code with the patterns above will lead to critical failures:

-   **Unhandled Exceptions:** Any exception inside an `async void` lifecycle method is uncatchable by the Unity engine's main thread. This will result in an immediate and difficult-to-debug application crash.
-   **Race Conditions and State Corruption:** Since methods like `Update` or `OnGUI` are called every frame, an `async` version will launch a new, independent task on each invocation. This creates dozens of overlapping, competing tasks, leading to unpredictable behavior, corrupted state, and visual glitches.
-   **Violation of the Game Loop:** The entire Unity architecture relies on a predictable, synchronous execution order (`Awake` -> `Start` -> `Update`, etc.). Using `async void` on these methods breaks this fundamental contract, making application flow completely unreliable.

## Preserve Existing Comments (Mandatory)

- **Do not delete existing comments.** This especially applies to `<Summary>` blocks and any logical/explanatory comments.
- **Update, don’t remove.** If a comment is inaccurate or outdated, **revise it** to match the updated code instead of removing it.
- **Respect intent.** Explanatory comments exist for a reason (context, rationale, constraints). Keep them and improve clarity when necessary.
- **When in doubt, keep it.** Prefer minimal edits over deletion.


# AGENT GUIDELINES: Using VContainer in Unity

## Goal
Implement and wire dependencies with **VContainer** in a way that is portable across projects, minimal, and safe.

---

## Core Rules

1. **Single Root Scope**
   - Expect exactly one root `LifetimeScope` in the scene (the “project scope”).
   - Class name convention: `ProjectLifetimeScope : VContainer.Unity.LifetimeScope`.
   - All registrations happen in `protected override void Configure(IContainerBuilder builder)`.

2. **Registering**
   - **Services (providers)**: prefer components that already exist in scene/prefabs.
     ```csharp
     // Provide service as its interface
     builder.RegisterComponentInHierarchy<MyService>().As<IMyService>();
     ```
   - **Consumers (MonoBehaviours)** that need injection:
     ```csharp
     builder.RegisterComponentInHierarchy<MyConsumerComponent>();
     ```

3. **Scene Entry Points**
   - Any root `GameObject` that contains (or whose children contain) DI consumers must have a `LifetimeScope`.
   - That scope must **Parent** to the project scope.

4. **Injection Pattern for MonoBehaviours**
   - Use method injection with `[Inject]` and store references in fields.
   - Do NOT use injected services in `Awake()`. Use them in `Start()` or later.
     ```csharp
     public class MyConsumerComponent : MonoBehaviour
     {
         private IMyService _myService;

         [Inject]
         public void Construct(IMyService myService)
         {
             _myService = myService;
         }

         private void Start()
         {
             _myService.DoWork();
         }
     }
     ```

5. **Interfaces First**
   - Prefer registering services as interfaces (`As<IMyService>()`) to keep components swappable and testable.

6. **No Hidden Side Effects**
   - Do not perform registration from arbitrary scripts. All registration stays in `*LifetimeScope.Configure(...)`.

---

## When Manual Unity Actions Are Required
If an action cannot be performed by code (e.g., adding a `LifetimeScope` component in the scene, setting its **Parent** reference, creating the root `[ProjectLifetimeScope]` object), the agent must:

- Leave explicit **TODO comments** in changed files near the relevant code.
- Add a brief **Task Notes** section at the top of the response (or PR/commit message) listing:
  - Which `GameObject` needs a `LifetimeScope`.
  - Which `LifetimeScope.Parent` must be set and to what.
  - Any required drag-and-drop references in the Inspector.

Example Task Notes:
Task Notes (manual steps required):

1.   Add a LifetimeScope to "Managers" GameObject and set Parent = [ProjectLifetimeScope].

2.   Ensure [ProjectLifetimeScope] exists in the scene root with ProjectLifetimeScope.cs attached.
	

---

## Don’ts
- Don’t call or use injected services in `Awake()`.
- Don’t register consumers/services outside of `LifetimeScope.Configure`.
- Don’t assume the root scope exists—if missing, create the class and note manual scene setup in Task Notes.
