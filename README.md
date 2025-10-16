# PROCESIO Custom Actions – Full Guide (with Events)

This guide explains what Custom Actions are, how to build them from scratch, how their decorators work, and how to use the Actions with Events feature to enable design-time behavior. It is generic and applies to any business logic you want to implement.

Knowledge base references recommended:
- https://docs.procesio.com/custom-actions
- https://docs.procesio.com/custom-actions/your-first-custom-action
- https://docs.procesio.com/custom-actions/custom-action-decorators
- https://docs.procesio.com/custom-actions/frontend-decorator
- https://docs.procesio.com/custom-actions/frontend-select-decorator-guide
- https://docs.procesio.com/custom-actions/class-decorator
- https://docs.procesio.com/custom-actions/backend-decorator
- https://docs.procesio.com/custom-actions/lists-and-display-rules-inside-custom-actions
- https://docs.procesio.com/custom-actions/full-guide-on-custom-actions


## What are Custom Actions?

Custom Actions are .NET classes compiled into a library and uploaded to PROCESIO to extend the platform with your own logic. An action exposes typed inputs and outputs (properties), a runtime entry point (`Execute()`), and metadata via decorators that drive how the action is displayed and behaves in the canvas.

- Runtime: The `Execute()` method runs during a process execution (or when testing an action). Put your business logic here.
- Design-time: The decorators configure the properties (their visuals and behaviors). Optional methods annotated with `ControlEventHandler` can run in the canvas to pre-populate options and preview values when users interact with inputs.

Target framework: `.NET 8`.


## Decorators overview

Decorators are attributes placed on classes, properties, and methods to configure how an action appears and behaves.

- `ClassDecorator`: Configures the action’s name, icon shape, description, classification, and whether it is testable.
- `Permissions`: Allows delete/duplicate/add from toolbar.
- `FEDecorator` (Front-End): Configures a property’s UI (label, type, tab, row order, options binding, tooltips).
- `BEDecorator` (Back-End): Marks a property as `Direction.Input`, `Direction.Output`, or `Direction.InputOutput` for runtime mapping.
- `Validator`: Adds simple validation metadata such as `IsRequired` to mark property as mandatory before execution.
- `DependencyDecorator`: Declares front-end display rules based on other properties (visibility/enablement dependent relationships).
- `ControlEventHandler`: Attaches a design-time event handler (method) for event types such as `OnLoad`, `OnChange` or `OnClick` triggers.

Use these building blocks to model your action’s UI and data flow.


## Class-level metadata

- `ClassDecorator(Name, Shape, Description, Classification, IsTestable)`: Controls the action tile and metadata.
- `Permissions(CanDelete, CanDuplicate, CanAddFromToolbar)`: Controls how users can manage the action in the canvas.
- `FEDecorator` can be used at class level to define `FeComponentType.Side_pannel` and Tab relationships via `Parent` attribute (Yes, we're aware of the side panel typo, but it's a pain to fix).


## Property metadata

Each property exposed to the front end typically has:
- `FEDecorator(Label, Type, RowId, Tab, Options?, Tooltip?)`
  - `Type` represents the UI control type (text, number, select, checkbox, file, button, credentials, etc.).
  - `RowId` determines ordering in the tab. Set `RowId` to keep a clean vertical order, otherwise properties get displayed in random order in the UI.
  - `Tab` groups properties into tabs. Use consistent tab name for your properties since you may have more than one tab for an action.
  - `Parent` defines the parent side panel for the property declared at class level.
  - `Tooltip` provides additional information about the property when hovered over.
  - `DefaultValue` can set an initial value for text/number/checkbox types (such as a default value of 60 seconds for a number property representing a timeout).
  - `Min` and `Max` can constrain number inputs within a limited range.
  - `TextFormat` is an optional attribute used alongside `FeComponentType.Code_editor` for the UI to assist with syntax highlighting (PLAINTEXT, JSON, SQL, HTML, JAVASCRIPT).
  - `CustomCredentialsTypeGuid` is an optional attribute paired with `FeComponentType.Credentials_Custom` to bind a specific credentials type's id. This must be a valid GUID.
  - `Options` binds a collection property providing dropdown entries for `FeComponentType.Select`. The list property contains items of `OptionModel` instances filled either statically (hardcoded) or dynamically (events).
- `BEDecorator(IOProperty = Direction.Input|Output|InputOutput)` to map to runtime inputs/outputs.
- `Validator(IsRequired = true|false)` for basic validation prior to execution.
- `DependencyDecorator(Tab, Control, Operator, Value)`: Shows a property only when another property meets a rule (e.g., `NotEquals null`). This controls properties visibility in the UI, but does not execute logic. Examples of dependency patterns:
  - A second dropdown depends on the first dropdown's selection as a chain reaction.
  - A modified value depends on a checkbox being checked.
  - With credentials, most inputs could depend on `Credentials` being provided first in order to make external calls needing authentication (REST API, SMTP, SFTP, etc.).
  - A file input may be necessary before making certain calls (such as certificates, or excel/csv with filtered row data, etc.).


## Runtime behavior (Backend mapping and validation)

At runtime (process execution or test action), only `Execute()` method runs.
- Ensure your `Execute()` validates inputs before using them and sets outputs after your code logic.
- Implement your own business validation in code. It's the user's responsibility to ensure all inputs have the expected value. If a property is not set as `IsRequired = true` then the property may have null value at runtime.
- `BEDecorator` determines what property values are sent to/returned from runtime. This is done via reflection, hence why `Execute()` does not have a method return type.


## Design-time behavior (Actions with Events)

Events allow you to populate options and preview values while users configure the action in the canvas. This is configured using the attribute `ControlEventHandler` applied on your methods.
-`ControlEventHandler` has several parameters to control when and how it runs:
  - `TriggerControl`: The property name that triggers the event. If no `TriggerControl` then it is consider an action-level event and will run automatically right after the action is dropped in the canvas (e.g. for configuring an action `OnLoad` to pre-populate the action with defaults dynamically).
  - `EventType`: The type of event (`OnLoad`, `OnChange`, `OnClick`, etc.). This informs the UI what kind of behavior is expected for the event (action loaded, dropdown loaded, value changed, dropdown value changed, button clicked, etc.)
  - `InputControls`: A comma-separated list of property names that must receive from the front end before the method runs. Just like the `Execute()` method, this is done via reflection so any property used in code must be declared here to ensure it is populated. It's the user's responsibility to ensure all inputs are declared in the decorator and validated before use. Attribute can be empty if no inputs are needed.
  - `OutputControls`: A comma-separated list of property names that are sending values back to the front end after the method runs. This is done via reflection, hence why method return types are not needed and will be ignored. It's the user's responsibility to ensure all outputs are declared here to have their values returned from runtime in the UI. Attribute can be empty if no outputs are modified/needed.
  - `OutputTarget`: Either `Options` (to update dropdown options) or `Value` (to update data values). Do not mix both in the same attribute line. Use multiple attributes on the same method if both are needed. Set output property names accordingly, since the bound collection for options will be retrieved by the BackEnd automatically from `FEDecorator.Options` while the value property will be retrieved from the property itself.
  - `Order`: An optional attribute of integer type to control execution order when multiple methods share the same `TriggerControl`. Lower numbers run first. This is useful if instead of one single method that has complex logic you want to split logic across methods for different affected properties or if you want a chained execution logic in which case method1 mutates properties on the action and method 2 sees those changes and uses the updated values. Default is 0 when attribute is not declared. On multiple methods without an order, they will execute in random order, so consider if methods could override property values in order to determine method execution order.


## Credentials usage

Use credentials whenever you require external connections such as REST API, SMTP, SFTP, etc.
- `APICredentialsManager` is the typical property type used for REST credentials paired with`FeComponentType.Credentials_Rest`. If Procesio's Call Api Action does not satisfy your need and you want custom implementations, this is useful for when an API requires tokens/passwords/apiKeys/OAuth2 so that the data comes from Procesio's Credentials safely without users exposing sensitive information. Alternatively, when you want to move base URLs and API versions out of code for ease of access and easy modifications so that you don't reupload the action because the code has it hardcoded (e.g., `https://api.something.com/v2` can change to `/v3` and could be edited easy from Procesio's Credentials Manager). Call only relative paths in code (e.g., `endpoint = "/orders"` for `var apiResponse = await Credentials.Client.GetAsync(endpoint, queries, headers)` where `Credentials` is a property of type `APICredentialsManager`).
- `DbCredentialsManager` is the typical property type used for Database credentials paired with `FeComponentType.Credentials_Db`. Use this when you want to connect to databases if the Procesio Database Actions do not satisfy your needs, but make sure to use parameters for safety against SQL injection and to set a timeout for your command. However, the existing Procesio Database actions should satisfy basic requirements and the platform will be updated to include different types of databases in the future.
- `FTPCredentialsManager` is the typical property type used for SFTP credentials paired with `FeComponentType.Credentials_Ftp`. Use this when you want to connect to FTP/SFTP servers if the Procesio FTP Actions do not satisfy your needs. However, Procesio has comprehensive FTP actions so this is a less common need.
- `SMTPCredentialsManager` is the typical property type used for SMTP credentials paired with `FeComponentType.Credentials_Smtp` or `FeComponentType.Credentials_Smtp_Inbound`. However, Procesio's Send Email and Read Mailbox should already cover most needs.
- `CustomCredentialsManager` is the typical property type used for custom credentials paired with `FeComponentType.Credentials_Custom` and `FEDecorator's CustomCredentialsTypeGuid` which is a GUID of a custom credentials type created in your workspace. Use this when you want to create your own credentials type with specific fields (e.g., API key + secret, or username + password + domain, etc.) and use it in your action. This is useful for custom authentication schemes or when you want to move configuration data out of code for easy access and modifications without re-uploading the action.


## Putting it all together (tips and tricks)

 0. Configure your development environment as per the official docs: https://docs.procesio.com/custom-actions/your-first-custom-action. Set your .csproj and Action.Core NuGet package accordingly. Your new class should inherit Action.Core.IAction. You should have only one Custom Action per .csproj.
 1. Setup the mandatory `ClassDecorator` on your class and consider which `Permissions` you want to enable on your action.
 2. Consider your actions input and output properties and declare them accordingly with the mandatory `BEDecorator` decorator.
 3. Consider whether the properties are mandatory or optional and add the mandatory `Validator` decorator as needed. The UI will apply the validation before you and prevent saving your process or action testing if value is missing on `IsRequired` properties.
 4. Configure properties with the mandatory decorator `FEDecorator` to define how they will appear in the UI (label, type, tab, tooltip, etc.).
 5. Don't forget to set `RowId` order so that the properties don't get displayed in random order each time. This is only a visual improvement, it does not affect execution, but it helps the user experience when reusing the action.
 6. Add `DependencyDecorator` to guide the UI as to which inputs appear after which in case you want to configure chain reactions. This is a visual improvement, it does not affect execution, but it helps in case you have multiple properties to follow the steps more easily as you setup your action in the canvas.
 7. Implement `Execute()` for runtime. Keep it resilient to missing inputs if some are optional. Add business validation as needed. Set mandatory outputs at the end.
 8. Implement events using `ControlEventHandler` on your methods. The name of the methods are the user's choice and has no impact on executing them. 
 9. If you are implementing events, consider if these events will have a `TriggerControl` or they will be action-level (no trigger, runs on first time configuring the action). Set the `ControlEventType` accordingly to determine the UI behavior. Consider having reset/refresh posibility for `OnLoad` initializing events so that the user isn't forced to drag and drop the action in the canvas again to trigger a refresh of first load time.
10. If you are implementing events, ensure `InputControls` includes everything used by the method and validate them at the start of the method. Ensure `OutputControls` includes everything to return to the UI. Both are optional if you don't need inputs or affect outputs for your method (e.g. maybe it just does an API call). However, if you forget declaring them you will get null values at runtime since reflection won't populate them, or you won't get the returned output values at the end of the execution.
11. If you are implementing events and you have `OutputControls`, ensure you declare the `OutputTarget` which is the type of result the output will have. Use separate attributes for options vs values on a single method, or split in multiple methods triggered by the same `TriggerControl` property and ensure using `Order` if their logic impact each other. If you have multiple methods with the same `TriggerControl` and they don't have an `Order`, they will execute in random order, so consider if they could override each other's values. If you have multiple outputs in the `OutputControls` array and they have the same `OutputTarget` type, then a single attribute is enough for all of them.
12. Use `nameof(Property)` for each property to avoid typos. `Options = nameof(MyOptionsListProperty)` or `Options = "MyOptionsListProperty"` mean the same, just that the second option with hardcoded strings is more prone to errors in case you rename your properties and forget to update all impacted decorators using that property.


## Example skeletons (generic)

- One property example and its decorators:
```csharp
[FEDecorator(Label = "Currency", Type = FeComponentType.Select, RowId = 5, Tab = "Geo", Options = nameof(CurrencyList), Tooltip = "Currencies of the selected country.")]
[BEDecorator(IOProperty = Direction.InputOutput)]
[DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
public string? Currency { get; set; }
private IList<OptionModel> CurrencyList { get; set; } = new List<OptionModel>();
```
- When `Country` property's value is NotEquals null in the UI, the dependency relationship with the property `Currency` is triggered and it will display the `FeComponentType.Select` dropdown in the UI and trigger all the methods with `TriggerControl = nameof(Country)`.
- Event method example:
```csharp
[ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Country), InputControls = [nameof(Country)], OutputControls = [nameof(CountryLocalTime)], OutputTarget = OutputTarget.Value)]
[ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Country), InputControls = [nameof(Country)], OutputControls = [nameof(Currency)], OutputTarget = OutputTarget.Options)]
public async Task OnCountryChange() {}
```
- In our example there is only one method with this `TriggerControl`: `OnCountryChange()`. The UI will send all inputs values for `InputControls = [nameof(Country)]` to be used at runtime.
- `OutputControls = [nameof(CountryLocalTime)]` will have their direct value populated while `OutputControls = [nameof(Currency)]` has its options targeted, which means behind the scenes at runtime the `CurrencyList` will be populated and its value returned for the dropdown to display the options visually.
- Runtime execution example:
```csharp
public async Task Execute()
{
    Validations.ValidateCountry(Country);
    Validations.ValidateCurrency(Currency);
    CurrencyInfo = await Commons.BuildCurrencyInfo(Country, Currency);
}
```
- `CurrencyInfo` will be populated at runtime based on what the user selects from dropdowns for `Country` and `Currency` in our example.
- When are events useful? If you want `Country` and `Currency` to have their dropdowns populated dynamically via a public API or external source like a google drive excel, otherwise consider the no-events variation: Omit `ControlEventHandler` methods, predefine hardcoded option lists in code and just put all computation in `Execute()` only. Example:
```csharp
private IList<OptionModel> CurrencyList { get; set; } = new List<OptionModel>
    {
        new OptionModel { name = "Euro", value = "EUR" },
        new OptionModel { name = "United States dollar", value = "USD" },
        new OptionModel { name = "Japanese yen", value = "JPY" },
        new OptionModel { name = "Pound sterling", value = "GBP" }
    };
```


## Where to look next

- The official documentation links above cover all decorators and platform specifics in depth. Checkout our Discord to discuss Custom Actions with the community: https://discord.com/invite/CEBuKgJefv
- The `net8.0/Events/GetCountryData` has examples of events-focused actions and different ways to configure a Custom Action. The sample actions in this repo demonstrate different styles: all events, split-by-output, runtime-tail, no-events.
- The `net8.0/Events/GetCountryData/Common/ActionsWithEvents.md` file in this repo explains events-focused patterns and trade-offs based on `GetCountryData` actions examples.


