using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GoogleSheetsAction.Models;
using GoogleSheetsAction.Services;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;
using Ringhel.Procesio.Action.Core.Utils;

namespace GoogleSheetsAction;

[ClassDecorator(Name = "Google Sheets Connector", Shape = ActionShape.Square,
    Description = "Create and manage Google Sheets from PROCESIO.",
    Classification = Classification.cat1, IsTestable = true)]
[FEDecorator(Label = "Configuration", Type = FeComponentType.Side_pannel, RowId = 1, Tab = "Google Sheets", Parent = "Configuration")]
[Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
public class GoogleSheetsConnector : IAction
{
    private IList<OptionModel> ActionOptions { get; } = Enum.GetValues(typeof(GoogleSheetsActionType))
        .Cast<GoogleSheetsActionType>()
        .Select(action => new OptionModel
        {
            name = FormatActionName(action),
            value = action.ToString()
        }).ToList();

    private IList<OptionModel> DriveOptions { get; } = new List<OptionModel>();
    private IList<OptionModel> SpreadsheetOptions { get; } = new List<OptionModel>();
    private IList<OptionModel> SheetOptions { get; } = new List<OptionModel>();
    private IList<OptionModel> HeaderOptions { get; } = new List<OptionModel>();
    private IList<OptionModel> RowIndexOptions { get; } = new List<OptionModel>();

    [FEDecorator(Label = "Google Sheets Credential", Type = FeComponentType.Credentials_Rest, RowId = 1, Tab = "Google Sheets",
        CustomCredentialsTypeGuid = "a65377cf-3092-4467-83e8-61b71d59cbbd")] //Google Sheets CredentialTemplate id
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = true)]
    public APICredentialsManager? SheetsCredentials { get; set; }

    [FEDecorator(Label = "Google Drive Credential", Type = FeComponentType.Credentials_Rest, RowId = 1, Tab = "Google Sheets",
        CustomCredentialsTypeGuid = "b2831f53-3e6f-401c-8a0e-31aaa06ea9fc")] //Google Drive CredentialTemplate id
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = true)]
    public APICredentialsManager? DriveCredentials { get; set; }

    [FEDecorator(Label = "Action", Type = FeComponentType.Select, RowId = 2, Tab = "Google Sheets",
        Options = nameof(ActionOptions))]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [Validator(IsRequired = true)]
    public string? SelectedAction { get; set; }

    [FEDecorator(Label = "Drive", Type = FeComponentType.Select, RowId = 10, Parent = "Configuration",
        Options = nameof(DriveOptions), Tooltip = "Select the shared drive where the spreadsheet should be stored.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public string? DriveId { get; set; }

    [FEDecorator(Label = "Spreadsheet", Type = FeComponentType.Select, RowId = 20, Parent = "Configuration",
        Options = nameof(SpreadsheetOptions))]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteSpreadsheet))]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public string? SpreadsheetId { get; set; }

    [FEDecorator(Label = "Sheet", Type = FeComponentType.Select, RowId = 30, Parent = "Configuration",
        Options = nameof(SheetOptions))]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public string? SheetId { get; set; }

    [FEDecorator(Label = "Sheet Name", Type = FeComponentType.Text, RowId = 31, Parent = "Configuration")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSheet))]
    [Validator(IsRequired = false)]
    public string? NewSheetTitle { get; set; }

    [FEDecorator(Label = "Range", Type = FeComponentType.Text, RowId = 40, Parent = "Configuration")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.GetRows))]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.ClearRange))]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.UpdateRowByRange))]
    [Validator(IsRequired = false)]
    public string? Range { get; set; }

    [FEDecorator(Label = "Title", Type = FeComponentType.Text, RowId = 50, Parent = "Configuration",
        Tooltip = "Name of the spreadsheet that will be created.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSpreadsheet))]
    [Validator(IsRequired = false)]
    public string? SpreadsheetTitle { get; set; }

    [FEDecorator(Label = "Headers", Type = FeComponentType.Text, RowId = 60, Parent = "Configuration",
        Tooltip = "Optional comma, semicolon or newline separated column headers for the first row.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSpreadsheet))]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSheet), LogicalOperator = LogicalOperator.Or)]
    [Validator(IsRequired = false)]
    public string? Headers { get; set; }

    [FEDecorator(Label = "Overwrite Existing", Type = FeComponentType.Check_box, RowId = 70, Parent = "Configuration")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSheet))]
    [Validator(IsRequired = false)]
    public bool OverwriteSheet { get; set; }

    [FEDecorator(Label = "Key Column", Type = FeComponentType.Select, RowId = 80, Parent = "Configuration",
        Options = nameof(HeaderOptions))]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendOrUpdateRow))]
    [Validator(IsRequired = false)]
    public string? KeyColumn { get; set; }

    [FEDecorator(Label = "Row Number", Type = FeComponentType.Select, RowId = 90, Parent = "Configuration",
        Options = nameof(RowIndexOptions))]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.UpdateRowByRange))]
    [Validator(IsRequired = false)]
    public string? TargetRowNumber { get; set; }

    [FEDecorator(Label = "Start Index", Type = FeComponentType.Number, RowId = 100, Parent = "Configuration")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteDimension))]
    [Validator(IsRequired = false)]
    public int? StartIndex { get; set; }

    [FEDecorator(Label = "End Index", Type = FeComponentType.Number, RowId = 110, Parent = "Configuration")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteDimension))]
    [Validator(IsRequired = false)]
    public int? EndIndex { get; set; }

    [FEDecorator(Label = "Row Values", Type = FeComponentType.Code_editor, RowId = 120, Parent = "Configuration", TextFormat = FeTextFormat.JSON,
        Tooltip = "Provide a JSON object mapping column headers to values.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendRow))]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendOrUpdateRow))]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.UpdateRowByRange))]
    [Validator(IsRequired = false)]
    public string? RowValuesJson { get; set; }

    [FEDecorator(Label = "Response", Type = FeComponentType.DataType, RowId = 200, Parent = "Configuration",
        Tooltip = "Full response returned by the Google Sheets API.")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public object? Response { get; set; }

    private static JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task Execute()
    {
        if (string.IsNullOrWhiteSpace(SelectedAction))
        {
            Response = BuildError("MissingAction", "An action must be selected before execution.");
            return;
        }

        if (!Enum.TryParse(SelectedAction, out GoogleSheetsActionType actionType))
        {
            Response = BuildError("InvalidAction", $"Unsupported action '{SelectedAction}'.");
            return;
        }

        Response = actionType switch
        {
            GoogleSheetsActionType.CreateSpreadsheet => await ExecuteCreateSpreadsheetAsync().ConfigureAwait(false),
            _ => BuildError("UnsupportedAction", $"Action '{actionType}' is not implemented.")
        };
    }

    [ControlEventHandler(EventType = ControlEventType.OnLoad,
        InputControls = [nameof(DriveCredentials)],
        OutputControls = [nameof(DriveId)],
        OutputTarget = OutputTarget.Options)]
    public async Task InitializeAsync()
    {
        if (DriveCredentials?.Client is null)
        {
            return;
        }

        await LoadDriveOptionsAsync(clearExisting: true, preserveSelection: true).ConfigureAwait(false);
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(DriveCredentials),
        InputControls = [nameof(DriveCredentials), nameof(SelectedAction)],
        OutputControls = [nameof(DriveId)], OutputTarget = OutputTarget.Options)]
    public async Task OnCredentialsChanged()
    {
        ResetActionScopedOptions();
        if (DriveCredentials?.Client is null)
        {
            return;
        }

        await LoadDriveOptionsAsync(clearExisting: true, preserveSelection: false).ConfigureAwait(false);
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(DriveId),
        InputControls = [nameof(DriveCredentials), nameof(DriveId)],
        OutputControls = [nameof(SpreadsheetId)], OutputTarget = OutputTarget.Options)]
    public async Task OnDriveChanged()
    {
        SpreadsheetOptions.Clear();
        SheetOptions.Clear();
        HeaderOptions.Clear();
        RowIndexOptions.Clear();

        if (DriveCredentials?.Client is null ||
            string.IsNullOrWhiteSpace(DriveId))
        {
            return;
        }

        var driveClient = new GoogleDriveClient(DriveCredentials!);
        var spreadsheets = await driveClient.ListSpreadsheetsAsync(DriveId).ConfigureAwait(false);
        foreach (var file in spreadsheets)
        {
            if (!string.IsNullOrWhiteSpace(file.Id))
            {
                SpreadsheetOptions.Add(new OptionModel { name = file.Name ?? file.Id, value = file.Id });
            }
        }
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(SpreadsheetId),
        InputControls = [nameof(SheetsCredentials), nameof(SpreadsheetId)], OutputControls = [nameof(SheetId)], OutputTarget = OutputTarget.Options)]
    public async Task OnSpreadsheetChanged()
    {
        SheetOptions.Clear();
        HeaderOptions.Clear();
        RowIndexOptions.Clear();

        if (SheetsCredentials?.Client is null ||
            string.IsNullOrWhiteSpace(SelectedAction) ||
            string.IsNullOrWhiteSpace(SpreadsheetId))
        {
            return;
        }

        var sheetsClient = new GoogleSheetsClient(SheetsCredentials!);
        var spreadsheet = await sheetsClient.GetSpreadsheetAsync(SpreadsheetId).ConfigureAwait(false);
        if (spreadsheet?.Sheets is null)
        {
            return;
        }

        foreach (var sheet in spreadsheet.Sheets)
        {
            if (sheet.Properties is { } properties)
            {
                SheetOptions.Add(new OptionModel
                {
                    name = string.IsNullOrWhiteSpace(properties.Title) ? properties.SheetId.ToString() : properties.Title,
                    value = properties.SheetId.ToString()
                });
            }
        }
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(SheetId),
        InputControls = [nameof(SheetsCredentials), nameof(SpreadsheetId), nameof(SheetId), nameof(SelectedAction)],
        OutputControls = [nameof(KeyColumn), nameof(TargetRowNumber)], OutputTarget = OutputTarget.Options)]
    public async Task OnSheetChanged()
    {
        HeaderOptions.Clear();
        RowIndexOptions.Clear();

        if (SheetsCredentials?.Client is null ||
            string.IsNullOrWhiteSpace(SelectedAction) ||
            string.IsNullOrWhiteSpace(SpreadsheetId) ||
            string.IsNullOrWhiteSpace(SheetId))
        {
            return;
        }

        var sheetsClient = new GoogleSheetsClient(SheetsCredentials!);
        var sheetName = SheetOptions.FirstOrDefault(option => option.value.ToString() == SheetId)?.name ?? SheetId;
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            return;
        }

        if (RequiresHeaders())
        {
            var headers = await sheetsClient.BuildHeaderOptionsAsync(SpreadsheetId, sheetName).ConfigureAwait(false);
            foreach (var header in headers)
            {
                HeaderOptions.Add(header);
            }
        }

        if (RequiresRowSelection())
        {
            var rows = await sheetsClient.BuildRowNumberOptionsAsync(SpreadsheetId, sheetName).ConfigureAwait(false);
            foreach (var row in rows)
            {
                RowIndexOptions.Add(row);
            }
        }
    }

    private void ResetActionScopedOptions()
    {
        DriveOptions.Clear();
        SpreadsheetOptions.Clear();
        SheetOptions.Clear();
        HeaderOptions.Clear();
        RowIndexOptions.Clear();
        DriveId = null;
        SpreadsheetId = null;
        SheetId = null;
        KeyColumn = null;
        TargetRowNumber = null;
        RowValuesJson = null;
        Headers = null;
        SpreadsheetTitle = null;
        NewSheetTitle = null;
        Range = null;
        StartIndex = null;
        EndIndex = null;
        OverwriteSheet = false;
    }

    private async Task LoadDriveOptionsAsync(bool clearExisting, bool preserveSelection)
    {
        var existingSelection = preserveSelection ? DriveId : null;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!clearExisting)
        {
            foreach (var option in DriveOptions)
            {
                var value = option.value?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    seen.Add(value);
                }
            }
        }
        else
        {
            DriveOptions.Clear();
        }

        if (DriveCredentials?.Client is null)
        {
            return;
        }

        var driveClient = new GoogleDriveClient(DriveCredentials);
        var drives = await driveClient.ListDrivesAsync().ConfigureAwait(false);
        foreach (var drive in drives)
        {
            if (string.IsNullOrWhiteSpace(drive.Id) || !seen.Add(drive.Id))
            {
                continue;
            }

            DriveOptions.Add(new OptionModel
            {
                name = string.IsNullOrWhiteSpace(drive.Name) ? drive.Id : drive.Name,
                value = drive.Id
            });
        }

        if (!seen.Contains("root"))
        {
            DriveOptions.Insert(0, new OptionModel { name = "My Google Drive", value = "root" });
            seen.Add("root");
        }

        if (!string.IsNullOrWhiteSpace(existingSelection) && !seen.Contains(existingSelection))
        {
            DriveOptions.Add(new OptionModel { name = existingSelection, value = existingSelection });
        }
    }

    private async Task<object?> ExecuteCreateSpreadsheetAsync()
    {
        if (SheetsCredentials?.Client is null)
        {
            return BuildError("MissingSheetsCredentials", "Google Sheets credentials are required to create a spreadsheet.");
        }

        var title = SpreadsheetTitle?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return BuildError("MissingTitle", "Spreadsheet title is required.");
        }

        var request = new
        {
            properties = new
            {
                title
            }
        };

        HttpResponseMessage createResponse;
        string createPayload;
        try
        {
            createResponse = await SendPostAsync(SheetsCredentials, "v4/spreadsheets", request).ConfigureAwait(false);
            createPayload = await createResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return BuildError("CreateSpreadsheetFailed", ex.Message);
        }

        if (!createResponse.IsSuccessStatusCode)
        {
            return BuildError("CreateSpreadsheetFailed",
                $"Google Sheets API responded with status {(int)createResponse.StatusCode} {createResponse.StatusCode}.",
                createResponse.StatusCode, createPayload);
        }

        var spreadsheetNode = ParseJsonNode(createPayload);
        if (spreadsheetNode is null)
        {
            return BuildError("CreateSpreadsheetFailed", "Unable to parse the Sheets API response.",
                createResponse.StatusCode, createPayload);
        }

        var spreadsheetId = spreadsheetNode["spreadsheetId"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            return BuildError("CreateSpreadsheetFailed", "The Sheets API response did not include a spreadsheetId.",
                createResponse.StatusCode, createPayload);
        }

        string? defaultSheetTitle = null;
        if (spreadsheetNode["sheets"] is JsonArray sheetsArray)
        {
            var firstSheet = sheetsArray.FirstOrDefault();
            if (firstSheet is JsonObject sheetObject &&
                sheetObject["properties"] is JsonObject properties &&
                properties["title"] is JsonNode sheetTitleNode)
            {
                defaultSheetTitle = sheetTitleNode.GetValue<string?>();
            }
        }

        if (!string.IsNullOrWhiteSpace(DriveId) && !string.Equals(DriveId, "root", StringComparison.OrdinalIgnoreCase) &&
            DriveCredentials?.Client is not null)
        {
            var driveQuery = new Dictionary<string, string>
            {
                ["supportsAllDrives"] = "true",
                ["includeItemsFromAllDrives"] = "true",
                ["addParents"] = DriveId!
            };

            try
            {
                var patchResponse = await SendPatchAsync(DriveCredentials, $"drive/v3/files/{spreadsheetId}", new { }, driveQuery)
                    .ConfigureAwait(false);
                if (!patchResponse.IsSuccessStatusCode)
                {
                    var patchPayload = await patchResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return BuildError("AssignDriveFailed",
                        $"Failed to assign the spreadsheet to drive '{DriveId}'. Status {(int)patchResponse.StatusCode} {patchResponse.StatusCode}.",
                        patchResponse.StatusCode, patchPayload, spreadsheetNode);
                }
            }
            catch (Exception ex)
            {
                return BuildError("AssignDriveFailed", ex.Message, null, null, spreadsheetNode);
            }
        }

        var headerValues = ParseHeaders();
        if (headerValues.Count > 0)
        {
            var targetSheet = string.IsNullOrWhiteSpace(defaultSheetTitle) ? "Sheet1" : defaultSheetTitle!;
            var range = $"{targetSheet}!1:1";
            var updateQuery = new Dictionary<string, string>
            {
                ["valueInputOption"] = "RAW"
            };

            var updateBody = new
            {
                values = new List<IList<string>> { headerValues }
            };

            try
            {
                var updateResponse = await SendPutAsync(SheetsCredentials, $"v4/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(range)}",
                        updateBody, updateQuery)
                    .ConfigureAwait(false);

                if (!updateResponse.IsSuccessStatusCode)
                {
                    var updatePayload = await updateResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return BuildError("ApplyHeadersFailed",
                        $"Failed to apply headers to the new spreadsheet. Status {(int)updateResponse.StatusCode} {updateResponse.StatusCode}.",
                        updateResponse.StatusCode, updatePayload, spreadsheetNode);
                }
            }
            catch (Exception ex)
            {
                return BuildError("ApplyHeadersFailed", ex.Message, null, null, spreadsheetNode);
            }
        }

        return spreadsheetNode;
    }

    private List<string> ParseHeaders()
    {
        if (string.IsNullOrWhiteSpace(Headers))
        {
            return new List<string>();
        }

        var trimmed = Headers.Trim();
        try
        {
            if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
            {
                var asJson = JsonSerializer.Deserialize<List<string>>(trimmed, SerializerOptions);
                if (asJson is { Count: > 0 })
                {
                    return asJson
                        .Select(header => header?.Trim())
                        .Where(header => !string.IsNullOrWhiteSpace(header))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(header => header!)
                        .ToList();
                }
            }
        }
        catch (JsonException)
        {
            // Ignore JSON parsing failures and fall back to delimiter-based parsing.
        }

        var separators = new[] { ',', ';', '\n', '\r', '\t' };
        return Headers
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(header => header.Trim())
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static JsonNode? ParseJsonNode(string json)
    {
        try
        {
            return JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = false });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static object BuildError(string code, string message, HttpStatusCode? statusCode = null, string? rawResponse = null,
        JsonNode? originalResponse = null)
    {
        return new
        {
            Success = false,
            Error = new
            {
                Code = code,
                Message = message,
                Status = statusCode?.ToString(),
                RawResponse = rawResponse,
                OriginalResponse = originalResponse?.DeepClone()
            }
        };
    }

    private Task<HttpResponseMessage> SendPostAsync(APICredentialsManager credentials, string endpoint, object? body,
        IDictionary<string, string>? query = null)
    {
        return InvokeClientMethodAsync(credentials, "PostAsync", HttpMethod.Post, endpoint, body, query);
    }

    private Task<HttpResponseMessage> SendPatchAsync(APICredentialsManager credentials, string endpoint, object? body,
        IDictionary<string, string>? query = null)
    {
        return InvokeClientMethodAsync(credentials, "PatchAsync", HttpMethod.Patch, endpoint, body, query);
    }

    private Task<HttpResponseMessage> SendPutAsync(APICredentialsManager credentials, string endpoint, object? body,
        IDictionary<string, string>? query = null)
    {
        return InvokeClientMethodAsync(credentials, "PutAsync", HttpMethod.Put, endpoint, body, query);
    }

    private async Task<HttpResponseMessage> InvokeClientMethodAsync(APICredentialsManager credentials, string methodName,
        HttpMethod fallbackMethod, string endpoint, object? body, IDictionary<string, string>? query)
    {
        var client = credentials.Client ?? throw new InvalidOperationException("Credentials client is not configured.");
        var methods = client.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => string.Equals(method.Name, methodName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var method in methods)
        {
            var arguments = TryBuildArguments(method, endpoint, body, query);
            if (arguments is null)
            {
                continue;
            }

            var invocationResult = method.Invoke(client, arguments);
            if (invocationResult is Task<HttpResponseMessage> httpTask)
            {
                return await httpTask.ConfigureAwait(false);
            }

            if (invocationResult is HttpResponseMessage httpResponse)
            {
                return httpResponse;
            }
        }

        return await SendHttpRequestAsync(client, fallbackMethod, endpoint, query, body).ConfigureAwait(false);
    }

    private static object?[]? TryBuildArguments(MethodInfo method, string endpoint, object? body, IDictionary<string, string>? query)
    {
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];
        var queryArgument = query ?? new Dictionary<string, string>();
        var headersArgument = new Dictionary<string, string>();
        var queryAssigned = false;
        var headersAssigned = false;

        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            var type = parameter.ParameterType;

            if (type == typeof(string))
            {
                arguments[index] = endpoint;
                continue;
            }

            if (type == typeof(object))
            {
                arguments[index] = body ?? new { };
                continue;
            }

            if (typeof(HttpContent).IsAssignableFrom(type))
            {
                arguments[index] = body as HttpContent ??
                    new StringContent(JsonSerializer.Serialize(body ?? new { }, SerializerOptions), Encoding.UTF8, "application/json");
                continue;
            }

            if (typeof(IDictionary<string, string>).IsAssignableFrom(type))
            {
                if (IsHeaderParameter(parameter))
                {
                    arguments[index] = headersArgument;
                    headersAssigned = true;
                }
                else if (IsQueryParameter(parameter))
                {
                    arguments[index] = queryArgument;
                    queryAssigned = true;
                }
                else if (!queryAssigned)
                {
                    arguments[index] = queryArgument;
                    queryAssigned = true;
                }
                else if (!headersAssigned)
                {
                    arguments[index] = headersArgument;
                    headersAssigned = true;
                }
                else
                {
                    arguments[index] = new Dictionary<string, string>();
                }

                continue;
            }

            if (type == typeof(CancellationToken))
            {
                arguments[index] = CancellationToken.None;
                continue;
            }

            return null;
        }

        return arguments;
    }

    private static bool IsHeaderParameter(ParameterInfo parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter.Name))
        {
            return false;
        }

        return parameter.Name.Contains("header", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsQueryParameter(ParameterInfo parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter.Name))
        {
            return false;
        }

        return parameter.Name.Contains("query", StringComparison.OrdinalIgnoreCase) ||
            parameter.Name.Contains("param", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<HttpResponseMessage> SendHttpRequestAsync(object client, HttpMethod method, string endpoint,
        IDictionary<string, string>? query, object? body)
    {
        var requestUri = BuildRelativeUri(endpoint, query);
        using var request = new HttpRequestMessage(method, requestUri);

        if (body is HttpContent httpContent)
        {
            request.Content = httpContent;
        }
        else if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, SerializerOptions), Encoding.UTF8, "application/json");
        }

        var sendMethod = client.GetType().GetMethod("SendAsync", new[] { typeof(HttpRequestMessage), typeof(CancellationToken) }) ??
                         client.GetType().GetMethod("SendAsync", new[] { typeof(HttpRequestMessage) });

        if (sendMethod is null)
        {
            throw new InvalidOperationException("The credentials client does not expose a SendAsync method.");
        }

        object? invocationResult;
        if (sendMethod.GetParameters().Length == 2)
        {
            invocationResult = sendMethod.Invoke(client, new object?[] { request, CancellationToken.None });
        }
        else
        {
            invocationResult = sendMethod.Invoke(client, new object?[] { request });
        }

        if (invocationResult is Task<HttpResponseMessage> httpTask)
        {
            return await httpTask.ConfigureAwait(false);
        }

        if (invocationResult is HttpResponseMessage httpResponse)
        {
            return httpResponse;
        }

        throw new InvalidOperationException("Unexpected return type from SendAsync.");
    }

    private static string BuildRelativeUri(string endpoint, IDictionary<string, string>? query)
    {
        if (query is null || query.Count == 0)
        {
            return endpoint;
        }

        var builder = new StringBuilder(endpoint);
        builder.Append(endpoint.Contains('?') ? '&' : '?');
        builder.Append(string.Join("&", query.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}")));
        return builder.ToString();
    }

    private bool RequiresHeaders()
    {
        if (!Enum.TryParse(SelectedAction, out GoogleSheetsActionType actionType))
        {
            return false;
        }

        return actionType is GoogleSheetsActionType.AppendRow
            or GoogleSheetsActionType.AppendOrUpdateRow
            or GoogleSheetsActionType.UpdateRowByRange;
    }

    private bool RequiresRowSelection()
    {
        if (!Enum.TryParse(SelectedAction, out GoogleSheetsActionType actionType))
        {
            return false;
        }

        return actionType is GoogleSheetsActionType.UpdateRowByRange;
    }

    private static string FormatActionName(GoogleSheetsActionType action)
    {
        return action switch
        {
            GoogleSheetsActionType.CreateSpreadsheet => "Create spreadsheet",
            GoogleSheetsActionType.DeleteSpreadsheet => "Delete spreadsheet",
            GoogleSheetsActionType.AppendRow => "Append row",
            GoogleSheetsActionType.AppendOrUpdateRow => "Append or update row",
            GoogleSheetsActionType.ClearRange => "Clear sheet or range",
            GoogleSheetsActionType.CreateSheet => "Create sheet",
            GoogleSheetsActionType.DeleteSheet => "Delete sheet",
            GoogleSheetsActionType.DeleteDimension => "Delete rows or columns",
            GoogleSheetsActionType.GetRows => "Get rows",
            GoogleSheetsActionType.UpdateRowByRange => "Update row by range",
            _ => action.ToString()
        };
    }
}
