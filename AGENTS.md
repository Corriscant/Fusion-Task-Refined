AGENTS Instructions:

* Unity scripts should be attached to objects if you want them to be part of the Unity lifecycle.
* Branch names must be strictly in English.
* Comments in Unity code must be written in English.
* Comments in Delphi code must be written in Russian.
* Follow the existing coding style, including variable and constant naming.
ТЕСТ

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
