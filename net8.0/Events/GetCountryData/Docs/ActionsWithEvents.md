# Actions with Events: Property Dependencies and Method Events

This document explains the new decorators that expand Custom Actions with design-time behavior:
- Property dependencies: `DependencyDecorator`
- Method events: `ControlEventHandler`

It also contrasts the new model with the classic “no events” approach, using the country actions in this project as concrete examples.


## 0) Baseline: The classic No-Events approach

See `CountryWithNoEventsAction`.
- All option lists are predefined (hardcoded) in private fields.
- Users pick values for `Region`, `Country`, `Currency` and any outputs are computed at runtime in `Execute()`.
- No dynamic population or design-time execution occurs; all logic runs in `Execute()`.

The events feature builds on this and allows populating options and values dynamically at design time (in the canvas), before runtime execution.


## 1) OnLoad initialization (action-level event)

OnLoad handlers run immediately after the action is dragged onto the canvas.
- They are declared with `EventType = ControlEventType.OnLoad`.
- They do not have `TriggerControl` because the trigger is the action itself.

Example (see `CountryAllEventsAction.InitializeData`, `CountryWithEventsAction.InitializeData`, `CountryWithSplitAction.InitRegions`/`InitStats`, `CountryWithCredentialsAction.InitializeData`):
- Populate `Region` options (`OutputTarget.Options`).
- Compute `GlobalStats` (`OutputTarget.Value`).

This is why `Region` and `GlobalStats` appear as soon as the action is dropped.


## 2) Chained dynamic option population

Chaining is driven by property changes with `OnChange` method events:
- `Region` change populates `Country` options.
- `Country` change populates `Currency` options.

Each handler:
- Specifies one single property as trigger (example: `TriggerControl = nameof(Region)` or `TriggerControl = nameof(Country)`).
- Declares which properties will have outputs at the end of the event (example: `OutputControls = [nameof(Country), nameof(Currency)]`).
- Uses `OutputTarget = OutputTarget.Options` to fill the option lists of those properties' dropdowns.

Example (see `OnRegionChange` and `OnCountryChange` in the event-driven actions):
- `OnRegionChange` sets `Country` list (`OutputTarget.Options`) and `RegionInfo` (`OutputTarget.Value`).
- `OnCountryChange` sets `Currency` list (`OutputTarget.Options`) and other outputs (`OutputTarget.Value`).


## 3) Multiple ControlEventHandler attributes on the same method

You can place multiple `ControlEventHandler` attributes on a single method to target different outputs for the same trigger:
- One attribute can target `OutputTarget.Options` for dropdown options.
- Another attribute can target `OutputTarget.Value` for value outputs.

Critical: Input/Output declaration is reflection-driven
- `InputControls` lists all properties that must be populated before the method executes. Values are retrieved from the front end and loaded via reflection within the action before the method call.
- `OutputControls` lists all properties whose values the method will set. After the method executes, the platform reads those values via reflection and returns them to the front end. Method return types are ignored.
- If a property is used as an input in the method, it must be declared in `InputControls`.
- If a property should be returned to the front end, it must be in `OutputControls`.
- If a property is set inside the method but not included in `OutputControls`, that value is ignored and not returned the front end.

See the country actions with events for examples of multi-attribute handlers that populate both options and values.


## 4) Input validation is the user’s responsibility

Before using inputs in a method, validate them. Use your own business rules; the samples use `Validations.*` helpers, e.g.:
- `Validations.ValidateRegion(Region)`
- `Validations.ValidateCountry(Country)`
- `Validations.ValidateCurrency(Currency)`
- `Validations.ValidateCredentials(Credentials)` (on the credentials-based action)

If an input is required for logic, ensure it’s in `InputControls` and validate its value is populated before use.


## 5) OutputTarget.Options vs OutputTarget.Value

- `OutputTarget.Value` sets the actual values of output properties (data, files, etc.).
- `OutputTarget.Options` updates a control’s options list (e.g., dropdown entries). OutputControls in this case must include the affected property's name, not its associated options list name (example: for ControlEventHandler's `OutputTarget = OutputTarget.Options, OutputControls = [nameof(Region)])` because we already know the Region's FEDecorator has `Options = nameof(RegionList)`).

Do not mix `Options` and `Value` in the same `ControlEventHandler` attribute. If you need both for the same trigger, apply multiple attributes to the same method (or use split methods). Examples:
- All-in-one method (multiple attributes): see `CountryAllEventsAction.OnCountryChange`.
- Split per output (ordering control): see `CountryWithSplitAction` where `PopulateCountryInfo`, `PopulateCountrySummary`, `PopulateCountryLocalTime`, and `PopulateCurrencies` each handle a single responsibility and declare an `Order`.

Multiple methods with the same `TriggerControl`
- All matching methods are invoked independently by reflection when the trigger property changes.
- Use the `Order` property to control execution order if it matters; default `Order` is 0.
- Important limitation: state does not persist between these method calls. Inputs come from the front-end state, not from a prior method in the same trigger chain. If `method1` changes a property value, `method2` will not see that new value unless the property is part of the front-end inputs for the trigger.


## Property dependencies (DependencyDecorator)

`DependencyDecorator` declares when a property should be visible/enabled in the front end based on other properties’ values. It does not execute logic; it controls UI visibility and order of interaction.

Examples applied in the country actions:
- `CountryCodesFile` depends on `Region` (and also on `Credentials` for the credentials-based action).
- `Country` depends on `Region` (and also on `Credentials` for the credentials-based action).
- `Currency` depends on `Country` (and also on `Credentials` for the credentials-based action).
- `Region` and `GlobalStats` depend on `Credentials` for the credentials-based action (they can’t initialize before credentials are chosen).
- `Refresh` depends on both `Region` and `GlobalStats` so it only shows after initialization. The `Refresh` button lets users re-run initialization to fetch updated data and repopulate options/outputs without recreating the action instance. See the `Refresh` handlers in the event-driven country actions. Not all actions needed a refresh (see: no events or credentials-based action that do not have an OnLoad behavior).

These dependencies guide the front end on how to show properties progressively. 
For the credentials-based action, users must first pick credentials, which trigger an OnChange event to initialize and show `Region` and `GlobalStats`, then users pick a region to see `Country`, and so on. All properties depend on credentials in this case in order to make the API call outside Procesio.
For actions without credentials, `Region` and `GlobalStats` have no dependencies since they are first in the chain and they initialize and show immediately on drag & drop (OnLoad event), then users pick a region to see `Country`, and so on. A basic HttpClient call to a free API is used behind the scenes.


## Credentials versus non-credentials actions

See `CountryWithCredentialsAction`:
- Use credentials mostly for APIs requiring user/password, API keys, OAuth, etc.
- In this sample, we still show how credentials can be useful even for a free API by moving the base URL to credentials.
  - Base URL (including version), e.g., `https://restcountries.com/v3.1`, is provided via credentials.
  - Code uses only the partial path, e.g., `"/all"`.
  - Benefit: you can change API version (e.g., from `v3.1` to a newer one) from the front end without changing code or redeploying the action.

This mirrors patterns also used by our GitHub action that use predefined GitHub credentials behind the scenes. `CustomCredentialsTypeGuid  = "62b726d4-89f4-490e-8287-064542c7295f"` is the equivalent id for this Procesio Credentials Template.


## Design-time events versus runtime execution

- All event methods (`OnLoad`, `OnChange`) execute at design time in the canvas. They prepare options and preview values.
- Only `Execute()` runs at runtime during process instance run or test action run. It's important for all events to be triggered for an action before `Execute()` runs, so that all inputs are ready.
- Based on user configuration (example if some inputs are not set with `IsRequired = true`) the front end may allow running the action even if not all events have been triggered. In that case, `Execute()` must handle missing inputs gracefully and it's the user's responsibility to create the code for it.
- For saving a process, the front end applies validation on action properties that are correctly configured, but with test action runs you might encounter errors if not events have been triggered and inputs are missing.

See examples:
- `CountryAllEventsAction`: `Execute()` performs only validations; all design-time logic (including preparing outputs) is done via events.
- `CountryWithEventsAction`: the last step (generating `CurrencyInfo`) runs in `Execute()` to demonstrate that part of the flow can be deferred to runtime.

Both approaches are correct; choose based on your business needs.


## Quick reference (what the samples demonstrate)

- OnLoad initialization (`EventType.OnLoad`). Re-initialization via a `Refresh` trigger.
- Using credentials vs httpClients. Credentials-based base URL and versioning convenience.
- Chained dynamic option population (Region ? Country ? Currency). Property dependencies (`DependencyDecorator`) guide user interaction.
- Multiple `ControlEventHandler` attributes on the same method (Options + Value) vs separate methods with a single dedicated `ControlEventHandler` ran based on Order.
- `OutputTarget.Value` vs `OutputTarget.Options` and not mixing them on the same decorator line.
- File input influencing option lists (`CountryCodesFile`). Output file generation (`CountrySummaryFile`).


## Best practices

- Always declare all required `InputControls` for a method; validate them before use.
- Always declare all affected `OutputControls` you want returned to the UI.
- Use multiple attributes on the same method to separate options and values targets, or split methods for explicit ordering.
- Use `Order` to control execution order when multiple methods share the same `TriggerControl`.
- Model `DependencyDecorator`s to guide user interaction in the UI.
- For APIs with versions, prefer moving base URL + version to credentials so version changes do not require code changes.
