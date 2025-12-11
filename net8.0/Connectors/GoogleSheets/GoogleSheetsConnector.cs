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
[FEDecorator(Label = "Configuration", Type = FeComponentType.Side_pannel, RowId = 4, Tab = "Google Sheets", Parent = "Configuration")]
[Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
public class GoogleSheetsConnector : IAction
{
    #region lists
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
    private IList<OptionModel> DimensionOptions { get; } = new List<OptionModel>
    {
        new OptionModel { name = "Rows", value = "ROWS" },
        new OptionModel { name = "Columns", value = "COLUMNS" }
    };
    #endregion

    #region properties
    [FEDecorator(Label = "Google Sheets Credential", Type = FeComponentType.Credentials_Rest, RowId = 2, Tab = "Google Sheets",
        CustomCredentialsTypeGuid = "a65377cf-3092-4467-83e8-61b71d59cbbd")] //Google Sheets CredentialTemplate id
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = true)]
    public APICredentialsManager? SheetsCredentials { get; set; }

    [FEDecorator(Label = "Google Drive Credential", Type = FeComponentType.Credentials_Rest, RowId = 1, Tab = "Google Sheets",
        CustomCredentialsTypeGuid = "b2831f53-3e6f-401c-8a0e-31aaa06ea9fc")] //Google Drive CredentialTemplate id
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = true)]
    public APICredentialsManager? DriveCredentials { get; set; }

    [FEDecorator(Label = "Action", Type = FeComponentType.Select, RowId = 3, Tab = "Google Sheets",
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
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSheet), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteSheet), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendRow), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendOrUpdateRow), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteDimension), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.GetRows), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.ClearRange), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.UpdateRow), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public string? SpreadsheetId { get; set; }

    [FEDecorator(Label = "Sheet", Type = FeComponentType.Select, RowId = 30, Parent = "Configuration",
        Options = nameof(SheetOptions))]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteSheet))]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendRow), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendOrUpdateRow), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteDimension), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.GetRows), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.ClearRange), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.UpdateRow), LogicalOperator = LogicalOperator.Or)]
    [Validator(IsRequired = false)]
    public string? SheetId { get; set; }

    [FEDecorator(Label = "Sheet Name", Type = FeComponentType.Text, RowId = 31, Parent = "Configuration",
        Tooltip = "Enter the name for the new sheet (tab) within the spreadsheet.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSheet))]
    [Validator(IsRequired = false)]
    public string? NewSheetTitle { get; set; }

    [FEDecorator(Label = "Range", Type = FeComponentType.Text, RowId = 40, Parent = "Configuration",
        Tooltip = "Optional. Specify a range in A1 notation. Examples: \"A1:C10\" (cell range), \"A:C\" (columns A to C), \"2:5\" (rows 2 to 5). Leave empty to use entire sheet.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.GetRows))]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.ClearRange), LogicalOperator = LogicalOperator.Or)]
    [Validator(IsRequired = false)]
    public string? Range { get; set; }

    [FEDecorator(Label = "Title", Type = FeComponentType.Text, RowId = 50, Parent = "Configuration",
        Tooltip = "Enter the name for the new spreadsheet.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSpreadsheet))]
    [Validator(IsRequired = false)]
    public string? SpreadsheetTitle { get; set; }

    [FEDecorator(Label = "Headers", Type = FeComponentType.Text, RowId = 60, Parent = "Configuration",
        Tooltip = "Provide headers as a list of strings. Example: [\"Column1\", \"Column2\", \"Column3\"]")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSpreadsheet))]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSheet), LogicalOperator = LogicalOperator.Or)]
    [Validator(IsRequired = false)]
    public IList<string>? Headers { get; set; }

    [FEDecorator(Label = "Overwrite Existing", Type = FeComponentType.Check_box, RowId = 70, Parent = "Configuration",
        Tooltip = "If checked, will delete or clear any existing sheet with the same name before creating a new one.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSheet))]
    [Validator(IsRequired = false)]
    public bool OverwriteSheet { get; set; }

    [FEDecorator(Label = "Key Column", Type = FeComponentType.Select, RowId = 80, Parent = "Configuration",
        Options = nameof(HeaderOptions), 
        Tooltip = "Select the column header that contains unique identifiers for each row. This is used to determine if a row should be updated (when a matching value is found) or appended as new (when no match exists).")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendOrUpdateRow))]
    [Validator(IsRequired = false)]
    public string? KeyColumn { get; set; }

    [FEDecorator(Label = "Row Number", Type = FeComponentType.Select, RowId = 90, Parent = "Configuration",
        Options = nameof(RowIndexOptions),
        Tooltip = "Select the row number to update. Row numbers are 1-based (row 1 is the first row).")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.UpdateRow))]
    [Validator(IsRequired = false)]
    public string? TargetRowNumber { get; set; }

    [FEDecorator(Label = "Start Index", Type = FeComponentType.Number, RowId = 100, Parent = "Configuration",
        Tooltip = "0-based index of the first row or column to delete. For example, 0 refers to the first row/column, 1 to the second, etc.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteDimension))]
    [Validator(IsRequired = false)]
    public int? StartIndex { get; set; }

    [FEDecorator(Label = "End Index", Type = FeComponentType.Number, RowId = 110, Parent = "Configuration",
        Tooltip = "0-based index of the row/column after the last one to delete (exclusive). For example, to delete rows 2-4, use Start Index = 1 and End Index = 4. Leave empty to delete from Start Index to the end of the sheet.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteDimension))]
    [Validator(IsRequired = false)]
    public int? EndIndex { get; set; }

    [FEDecorator(Label = "Direction", Type = FeComponentType.Select, RowId = 95, Parent = "Configuration",
        Options = nameof(DimensionOptions),
        Tooltip = "Select whether to delete entire rows or entire columns.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteDimension))]
    [Validator(IsRequired = false)]
    public string? Dimension { get; set; }

    [FEDecorator(Label = "Row Values", Type = FeComponentType.Code_editor, RowId = 120, Parent = "Configuration", TextFormat = FeTextFormat.JSON,
        Tooltip = "Provide row data as a JSON object mapping column headers to values. Example: {\"header1\":\"value1\",\"header2\":\"value2\"}")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SheetId), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendRow))]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendOrUpdateRow), LogicalOperator = LogicalOperator.Or)]
    [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.UpdateRow), LogicalOperator = LogicalOperator.Or)]
    [Validator(IsRequired = false)]
    public string? RowValuesJson { get; set; }

    [FEDecorator(Label = "Response", Type = FeComponentType.DataType, RowId = 200, Parent = "Configuration")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public object? Response { get; set; }
    #endregion

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
            GoogleSheetsActionType.CreateSheet
                => Response = await execute.CreateSheet(SpreadsheetId, NewSheetTitle, OverwriteSheet, Headers),
            GoogleSheetsActionType.DeleteSheet
                => Response = await execute.DeleteSheet(SpreadsheetId, SheetId),
            GoogleSheetsActionType.AppendRow
                => Response = await execute.AppendRow(SpreadsheetId, SheetId, RowValuesJson),
            GoogleSheetsActionType.AppendOrUpdateRow
                => Response = await execute.AppendOrUpdateRow(SpreadsheetId, SheetId, KeyColumn, RowValuesJson),
            GoogleSheetsActionType.ClearRange
                => Response = await execute.ClearRange(SpreadsheetId, SheetId, Range),
            GoogleSheetsActionType.DeleteDimension
                => Response = await execute.DeleteDimension(SpreadsheetId, SheetId, Dimension, StartIndex, EndIndex),
            GoogleSheetsActionType.GetRows
                => Response = await execute.GetRows(SpreadsheetId, SheetId, Range),
            GoogleSheetsActionType.UpdateRow
                => Response = await execute.UpdateRow(SpreadsheetId, SheetId, TargetRowNumber, RowValuesJson),
            _ => throw new Exception($"Action '{actionType}' is not implemented.")
        };
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(DriveCredentials),
        InputControls = [nameof(DriveCredentials), nameof(SelectedAction)],
        OutputControls = [nameof(DriveId)], OutputTarget = OutputTarget.Options)]
    public async Task OnCredentialsChanged()
    {
        var executionService = new GoogleExecutionService(SheetsCredentials, DriveCredentials);
        var drives = await executionService.GetDrives();

        foreach (var drive in drives)
        {
            if (!string.IsNullOrWhiteSpace(drive.Id))
            {
                DriveOptions.Add(new OptionModel { name = drive.Name ?? drive.Id, value = drive.Id });
            }
        }

        if (DriveOptions.All(option => option.value.ToString() != GoogleDriveClient.RootDriveId))
        {
            DriveOptions.Insert(0, new OptionModel { name = "My Google Drive", value = GoogleDriveClient.RootDriveId });
        }
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(DriveId),
        InputControls = [nameof(DriveCredentials), nameof(DriveId), nameof(SelectedAction)],
        OutputControls = [nameof(SpreadsheetId)], OutputTarget = OutputTarget.Options)]
    public async Task OnDriveChanged()
    {
        var permittedActions = new List<GoogleSheetsActionType>()
        {
            GoogleSheetsActionType.DeleteSpreadsheet,
            GoogleSheetsActionType.CreateSheet,
            GoogleSheetsActionType.DeleteSheet,
            GoogleSheetsActionType.AppendRow,
            GoogleSheetsActionType.AppendOrUpdateRow,
            GoogleSheetsActionType.DeleteDimension,
            GoogleSheetsActionType.GetRows,
            GoogleSheetsActionType.ClearRange,
            GoogleSheetsActionType.UpdateRow
        };

        if (!Enum.TryParse(SelectedAction, out GoogleSheetsActionType actionType) ||
            !permittedActions.Contains(actionType))
        {
            return;
        }

        var executionService = new GoogleExecutionService(SheetsCredentials, DriveCredentials);
        var spreadsheets = await executionService.GetSpreadsheets(DriveId);

        foreach (var file in spreadsheets)
        {
            if (!string.IsNullOrWhiteSpace(file.Id))
            {
                SpreadsheetOptions.Add(new OptionModel
                {
                    name = string.IsNullOrWhiteSpace(file.Name) ? file.Id : file.Name,
                    value = file.Id
                });
            }
        }
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(SpreadsheetId),
        InputControls = [nameof(SheetsCredentials), nameof(SpreadsheetId), nameof(SelectedAction)],
        OutputControls = [nameof(SheetId)], OutputTarget = OutputTarget.Options)]
    public async Task OnSpreadsheetChanged()
    {
        var permittedActions = new List<GoogleSheetsActionType>()
        {
            GoogleSheetsActionType.CreateSheet,
            GoogleSheetsActionType.DeleteSheet,
            GoogleSheetsActionType.AppendRow,
            GoogleSheetsActionType.AppendOrUpdateRow,
            GoogleSheetsActionType.DeleteDimension,
            GoogleSheetsActionType.GetRows,
            GoogleSheetsActionType.ClearRange,
            GoogleSheetsActionType.UpdateRow
        };

        if (!Enum.TryParse(SelectedAction, out GoogleSheetsActionType actionType) ||
            !permittedActions.Contains(actionType))
        {
            return;
        }

        var executionService = new GoogleExecutionService(SheetsCredentials, DriveCredentials);
        var sheets = await executionService.GetSheets(SpreadsheetId);

        foreach (var sheet in sheets)
        {
            SheetOptions.Add(sheet);
        }
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(SheetId),
        InputControls = [nameof(SheetsCredentials), nameof(SpreadsheetId), nameof(SheetId), nameof(SelectedAction)],
        OutputControls = [nameof(KeyColumn), nameof(TargetRowNumber)], OutputTarget = OutputTarget.Options)]
    public async Task OnSheetChanged()
    {
        var permittedActions = new List<GoogleSheetsActionType>()
        {
            GoogleSheetsActionType.AppendOrUpdateRow,
            GoogleSheetsActionType.UpdateRow
        };

        if (!Enum.TryParse(SelectedAction, out GoogleSheetsActionType actionType) ||
            !permittedActions.Contains(actionType))
        {
            return;
        }

        var executionService = new GoogleExecutionService(SheetsCredentials, DriveCredentials);

        if (actionType is GoogleSheetsActionType.AppendOrUpdateRow)
        {
            var headers = await executionService.GetSheetHeaders(SpreadsheetId, SheetId);
            foreach (var header in headers)
            {
                HeaderOptions.Add(header);
            }
        }

        if (actionType is GoogleSheetsActionType.UpdateRow)
        {
            var rows = await executionService.GetRowNumbers(SpreadsheetId, SheetId);
            foreach (var row in rows)
            {
                RowIndexOptions.Add(row);
            }
        }
    }

    private static string FormatActionName(GoogleSheetsActionType action)
    {
        return action switch
        {
            GoogleSheetsActionType.CreateSpreadsheet => "Create spreadsheet",
            GoogleSheetsActionType.DeleteSpreadsheet => "Delete spreadsheet",
            GoogleSheetsActionType.CreateSheet => "Create sheet",
            GoogleSheetsActionType.DeleteSheet => "Delete sheet",
            GoogleSheetsActionType.GetRows => "Get rows",
            GoogleSheetsActionType.AppendRow => "Append row",
            GoogleSheetsActionType.AppendOrUpdateRow => "Append or update row",
            GoogleSheetsActionType.UpdateRow => "Update row",
            GoogleSheetsActionType.ClearRange => "Clear sheet or range",
            GoogleSheetsActionType.DeleteDimension => "Delete rows or columns",
            _ => action.ToString()
        };
    }
}
