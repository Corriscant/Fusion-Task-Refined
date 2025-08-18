AGENTS Instructions:

* Unity scripts should be attached to objects if you want them to be part of the Unity lifecycle.
* Branch names must be strictly in English.
* Comments in Unity code must be written in English.
* Comments in Delphi code must be written in Russian.
* Follow the existing coding style, including variable and constant naming.
* Always use an explicit == null comparison to check for null on UnityEngine.Object and its derivatives. Avoid modern operators like ?., ??, ??= and the is null pattern, as they do not recognize Unity's "destroyed" object state and will cause a MissingReferenceException. For interface variables, use the safe cast: if (myInterface as UnityEngine.Object == null).
* Ensure the Tests folder for unit tests is placed within the Assets directory, not at the project root.

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

---

**Unity Object Null-Check Rule**

When checking if a variable referencing a `UnityEngine.Object` (especially via an interface) is null or has been destroyed, always prefer using the `IsNullOrDestroyed()` extension method.

-   **Don't:**
    ```csharp
    if (_networkObjectInjector as UnityEngine.Object != null) { ... }
    ```

-   **Do:**
    ```csharp
    if (!_networkObjectInjector.IsNullOrDestroyed()) { ... }
    ```

This approach is cleaner, more readable, and encapsulates Unity's specific object lifecycle logic in a single, reusable place.

---

**VContainer Usage Rule: `InjectGameObject`**

When dynamically instantiating a prefab and injecting its dependencies, the `InjectGameObject` method must be used.

This method is an extension provided by VContainer. Always ensure the necessary `using` directive is included at the top of the C# file.

-   **Required `using`:**
    ```csharp
    using VContainer.Unity;
    ```

-   **Example Usage:**
    ```csharp
    // File must contain 'using VContainer.Unity;'
    var playerInstance = container.InjectGameObject(playerPrefab);
    ```

Failure to include `using VContainer.Unity;` will result in a compile error, as the `InjectGameObject` extension method will not be resolved.

---
## Heuristic for Skipping Trivial Unit Tests

Avoid generating unit tests for methods that are simple, single-line wrappers or proxies for trusted framework APIs. A method is considered a trivial wrapper and should be skipped if it meets **all** of the following criteria:

1. The method body consists of a **single line**.
2. This line is a single `return` statement.
3. The returned value is a direct call to a method from a stable, well-tested framework (e.g., `UnityEngine` API, `.NET System` libraries) with no additional logic, calculations, or transformations.

**Example of a method to SKIP testing:**
This is a simple proxy call to the Unity Engine. We trust that `Vector3.ClampMagnitude` is already tested by Unity.
```csharp
public Vector3 ClampOffset(Vector3 offset, float allowedOffset)
{
    return Vector3.ClampMagnitude(offset, allowedOffset);
}
```

**Example of a simple method that SHOULD BE TESTED:**
Although this method is a single line, it contains original business logic (the condition health > 0) that belongs to our application and must be verified.

```csharp
public string GetStatus(int health)
{
    return health > 0 ? "Alive" : "Dead";
}
```