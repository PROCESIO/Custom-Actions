Copy-paste this prompt into your AI agent to generate Custom Actions that use the Actions with Events feature correctly.

Goal
- Create or modify PROCESIO Custom Actions that implement Actions with Events using best practices shown in:
  - `CountryAllEventsAction`
  - `CountryWithEventsAction`
  - `CountryWithSplitAction`
  - `CountryWithCredentialsAction`
  - `CountryWithNoEventsAction`
- Target .NET 8 and PROCESIO Action SDK attributes.

Constraints and conventions
- Listen to any user-provided descriptions, tooltips and names and apply them.
- Prefer shared helpers in a `Commons` static class to avoid code duplication.
- Use `DependencyDecorator` on properties to control UI visibility/enablement in the canvas.
- Use `ControlEventHandler` on methods to implement design-time behavior (OnLoad/OnChange).
- Never mix `OutputTarget.Options` and `OutputTarget.Value` in the same decorator attribute; use multiple attributes on the same method instead.
- When multiple methods share the same `TriggerControl`, all will execute independently via reflection; use `Order` to define execution order if needed.
- Reflection rule: Inputs must be declared in `InputControls` and outputs in `OutputControls`, otherwise values will not be provided/returned.
- Validate all inputs in methods before use (e.g., `Validations.ValidateRegion`, `ValidateCountry`, `ValidateCurrency`, `ValidateCredentials`).

What to implement
1) No-Events action if user is interested in only runtime behavior within a process.
- Use `CountryWithNoEventsAction` as the baseline: predefined option lists; all logic in `Execute()`.

2) OnLoad initialization if user is interested in drag-and-drop experience and wants data to be populated without manual user interaction when no dependencies are needed.
- Add one or more methods with `EventType = ControlEventType.OnLoad` to methods, examples:
  - Populate `Region` dropdown options (`OutputTarget.Options` to the `Region` property).
  - Compute `GlobalStats` (`OutputTarget.Value` to the `GlobalStats` property).
- These methods run immediately after drag-and-drop; they do not have `TriggerControl`.

3) Chained dynamic changes if user is interested in dependent dropdowns or values that change based on prior selections, examples:
- Region ? Country ? Currency dynamic population using `EventType = ControlEventType.OnChange` on methods:
  - `OnRegionChange`: `TriggerControl = nameof(Region)`; `OutputControls = [nameof(Country)]` with `OutputTarget.Options`.
  - `OnCountryChange`: `TriggerControl = nameof(Country)`; `OutputControls = [nameof(Currency)]` with `OutputTarget.Options`.
- You can also return values (e.g., `RegionInfo`, `CountryInfo`, `CountryLocalTime`, `CountrySummaryFile`) via a separate decorator on the same method with `OutputTarget.Value`, or split into separate methods and use `Order`.

4) File-driven behavior if user is interested in parsing files to influence behavior, examples:
- How `CountryCodesFile` input that limits `Country` options to provided ISO alpha-3 codes.
- Implement parser: JSON array or CSV/TSV separators or see what user needs.
- Behavior when file is missing/empty, or adding validation.

5) Property dependencies if user is interested in controlling visibility/enablement of properties based on other property values.
- Control relationships between properties via `DependencyDecorator` attributes on properties.
- Add conditions like if Value is not equals to null/empty, or equals to a specific value in order to be visible/enabled.
- Add credentials dependencies if properties rely on credentials being invoked/pre-populated beforehand.
- RowId layout and ordering will help with user experience in the canvas.

6) Input/Output reflection contract (critical)
- Before a handler runs, all `InputControls` are hydrated from front-end state and end up populating the action properties via reflection; after it returns, values from the declared `OutputControls` are read via reflection and sent to the UI.
- If a property is not listed in `InputControls`, do not assume it is set.
- If a property is set in code but not listed in `OutputControls`, its updated value is ignored by the UI.

7) Multiple decorators and Order
- To target both Options and Value for the same trigger, attach multiple `ControlEventHandler` attributes to the same method (one with `OutputTarget.Options`, another with `OutputTarget.Value`).
- If you need to return multiple outputs of the same type (e.g., multiple Value outputs), they can be set in the same `OutputControls` array if they have the same `OutputTarget`.
- If you split logic across multiple methods with the same `TriggerControl`, set `Order` to enforce the execution order. Methods do not share in-memory state between calls; inputs must come from the front end, not from a prior method.

8) Credentials usage (optional but recommended)
- Use `APICredentialsManager` when an API requires auth; also useful to move the base URL + version out of code:
  - Credentials base URL example: `https://restcountries.com/v3.1`.
  - Code should call only the partial path (e.g., `/all`).
  - Benefit: switching to a newer API version can be done from the front end without redeploying code.
- See our other types of credentials if needed (e.g., SMTP, FTP, Database, etc.).

9) Design-time vs runtime
- All event handlers (`OnLoad`, `OnChange`) execute at design-time in the canvas.
- Only `Execute()` executes at runtime.
- Both patterns are valid:
  - "All events" pattern: `Execute()` only validates.
  - "Runtime tail" pattern: compute the last step (e.g., currency details) in `Execute()`.

10) Validation to ensure correct inputs for each event method.
- Always validate inputs at the start of handlers, e.g.:
  - `Validations.ValidateRegion(Region)`
  - `Validations.ValidateCountry(Country)`
  - `Validations.ValidateCurrency(Currency)`
  - `Validations.ValidateCredentials(Credentials)`

Checklist for each event-driven action
- Has OnLoad to initialize properties (if applicable/needed).
- Has OnChange chain properties with correct `TriggerControl` and `OutputTarget`.
- Declares correct `InputControls` and `OutputControls` on all methods.
- Declares `DependencyDecorator`s on all methods that rely on other properties.
- Uses contiguous RowIds.
- Validates inputs in each method.
- Decides where the last step executes: design-time event or runtime `Execute()`.

Deliverables
- New or updated action classes following the above patterns.
- If creating credentials-based actions, ensure the base URL (with version) is read from the credentials and only relative paths are used in code.
- Reuse or create a `Commons` helper for shared logic and keep actions thin and easy to read. Reuse code as much as possible by extracting it into helper methods.
- Ensure all properties and methods have clear, user-provided descriptions/tooltips/names.
- Ensure code is clean, well-organized, and follows best practices for readability and maintainability.
