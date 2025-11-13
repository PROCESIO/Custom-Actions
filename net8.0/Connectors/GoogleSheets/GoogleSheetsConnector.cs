using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;
using Ringhel.Procesio.Action.Core.Utils;
using GoogleSheetsAction.Models;
using GoogleSheetsAction.Services;

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
        Options = nameof(DriveOptions))]
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

    [FEDecorator(Label = "Title", Type = FeComponentType.Text, RowId = 50, Parent = "Configuration")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSpreadsheet))]
    [Validator(IsRequired = false)]
    public string? SpreadsheetTitle { get; set; }

    [FEDecorator(Label = "Headers", Type = FeComponentType.Text, RowId = 60, Parent = "Configuration")]
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

    [FEDecorator(Label = "Response", Type = FeComponentType.DataType, RowId = 200, Parent = "Configuration")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public object? Response { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrWhiteSpace(SelectedAction))
        {
            throw new Exception("An action must be selected before execution.");
        }

        if (!Enum.TryParse(SelectedAction, out GoogleSheetsActionType actionType))
        {
            throw new Exception($"Unsupported action '{SelectedAction}'.");
        }

        var execute = new GoogleExecutionService(SheetsCredentials, DriveCredentials);
        Response = actionType switch
        {
            GoogleSheetsActionType.CreateSpreadsheet
                => Response = await execute.CreateSpreadsheet(SpreadsheetTitle, DriveId, Headers),
            GoogleSheetsActionType.DeleteSpreadsheet
                => Response = await execute.DeleteSpreadsheet(SpreadsheetId),
            GoogleSheetsActionType.AppendRow => throw new Exception($"Action '{actionType}' is not implemented."),
            GoogleSheetsActionType.AppendOrUpdateRow => throw new Exception($"Action '{actionType}' is not implemented."),
            GoogleSheetsActionType.ClearRange => throw new Exception($"Action '{actionType}' is not implemented."),
            GoogleSheetsActionType.CreateSheet
                => Response = await execute.CreateSheet(SpreadsheetId, NewSheetTitle, OverwriteSheet, Headers),
            GoogleSheetsActionType.DeleteSheet => throw new Exception($"Action '{actionType}' is not implemented."),
            GoogleSheetsActionType.DeleteDimension => throw new Exception($"Action '{actionType}' is not implemented."),
            GoogleSheetsActionType.GetRows => throw new Exception($"Action '{actionType}' is not implemented."),
            GoogleSheetsActionType.UpdateRowByRange => throw new Exception($"Action '{actionType}' is not implemented."),
            _ => throw new Exception($"Action '{actionType}' is not implemented.")
        };
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

        var driveClient = new GoogleDriveClient(DriveCredentials!);
        var drives = await driveClient.ListDrivesAsync();
        DriveOptions.Clear();
        foreach (var drive in drives)
        {
            if (!string.IsNullOrWhiteSpace(drive.Id))
            {
                DriveOptions.Add(new OptionModel { name = drive.Name ?? drive.Id, value = drive.Id });
            }
        }

        if (DriveOptions.All(option => option.value.ToString() != "root"))
        {
            DriveOptions.Insert(0, new OptionModel { name = "My Google Drive", value = "root" });
        }
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(DriveId),
        InputControls = [nameof(DriveCredentials), nameof(DriveId)],
        OutputControls = [nameof(SpreadsheetId)], OutputTarget = OutputTarget.Options)]
    public async Task OnDriveChanged()
    {
        //TODO: Next iteration when we develop the delte spreadsheet
        //SpreadsheetOptions.Clear();
        //SheetOptions.Clear();
        //HeaderOptions.Clear();
        //RowIndexOptions.Clear();

        //if (DriveCredentials?.Client is null ||
        //    string.IsNullOrWhiteSpace(DriveId))
        //{
        //    return;
        //}

        //var driveClient = new GoogleDriveClient(DriveCredentials!);
        //var spreadsheets = await driveClient.ListSpreadsheetsAsync(DriveId);
        //foreach (var file in spreadsheets)
        //{
        //    if (!string.IsNullOrWhiteSpace(file.Id))
        //    {
        //        SpreadsheetOptions.Add(new OptionModel { name = file.Name ?? file.Id, value = file.Id });
        //    }
        //}
        return;
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
        var spreadsheet = await sheetsClient.GetSpreadsheetAsync(SpreadsheetId);
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
            var headers = await sheetsClient.BuildHeaderOptionsAsync(SpreadsheetId, sheetName);
            foreach (var header in headers)
            {
                HeaderOptions.Add(header);
            }
        }

        if (RequiresRowSelection())
        {
            var rows = await sheetsClient.BuildRowNumberOptionsAsync(SpreadsheetId, sheetName);
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
