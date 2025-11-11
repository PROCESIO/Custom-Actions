using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoogleSheetConnector.Models;
using GoogleSheetConnector.Services;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;

namespace GoogleSheetConnector
{
    [ClassDecorator(Name = "Google Sheets Connector", Shape = ActionShape.Square,
        Description = "Create and manage Google Sheets from PROCESIO.",
        Classification = Classification.cat1, IsTestable = true)]
    [FEDecorator(Label = "Configuration", Type = FeComponentType.Side_pannel, RowId = 1, Tab = "Google Sheets", Parent = "Configuration")]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class GoogleSheetsConnectorAction : IAction
    {
        public GoogleSheetsConnectorAction()
        {
            ActionOptions = BuildActionOptions();
        }

        public List<OptionModel> ActionOptions { get; }

        public List<OptionModel> DriveOptions { get; } = new();

        public List<OptionModel> SpreadsheetOptions { get; } = new();

        public List<OptionModel> SheetOptions { get; } = new();

        public List<OptionModel> HeaderOptions { get; } = new();

        public List<OptionModel> RowIndexOptions { get; } = new();

        [FEDecorator(Label = "Google Sheets Credential", Type = FeComponentType.Credentials_Rest, RowId = 1, Tab = "Google Sheets")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public APICredentialsManager? Credentials { get; set; }

        [FEDecorator(Label = "Action", Type = FeComponentType.Select, RowId = 2, Tab = "Google Sheets",
            Options = nameof(ActionOptions))]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [Validator(IsRequired = true)]
        public string? SelectedAction { get; set; }

        [FEDecorator(Label = "Drive", Type = FeComponentType.Select, RowId = 10, Parent = "Configuration",
            Options = nameof(DriveOptions))]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.NotEquals, Value = null)]
        public string? DriveId { get; set; }

        [FEDecorator(Label = "Spreadsheet", Type = FeComponentType.Select, RowId = 20, Parent = "Configuration",
            Options = nameof(SpreadsheetOptions))]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.NotEquals, Value = null)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(DriveId), Operator = Operator.NotEquals, Value = null)]
        public string? SpreadsheetId { get; set; }

        [FEDecorator(Label = "Sheet", Type = FeComponentType.Select, RowId = 30, Parent = "Configuration",
            Options = nameof(SheetOptions))]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SpreadsheetId), Operator = Operator.NotEquals, Value = null)]
        public string? SheetId { get; set; }

        [FEDecorator(Label = "Sheet Name", Type = FeComponentType.Text, RowId = 31, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSheet))]
        public string? NewSheetTitle { get; set; }

        [FEDecorator(Label = "Range", Type = FeComponentType.Text, RowId = 40, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.GetRows))]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.ClearRange))]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.UpdateRowByRange))]
        public string? Range { get; set; }

        [FEDecorator(Label = "Title", Type = FeComponentType.Text, RowId = 50, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSpreadsheet))]
        public string? SpreadsheetTitle { get; set; }

        [FEDecorator(Label = "Headers", Type = FeComponentType.Text, RowId = 60, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSpreadsheet))]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSheet))]
        public string? Headers { get; set; }

        [FEDecorator(Label = "Overwrite Existing", Type = FeComponentType.Check_box, RowId = 70, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.CreateSheet))]
        public bool OverwriteSheet { get; set; }

        [FEDecorator(Label = "Key Column", Type = FeComponentType.Select, RowId = 80, Parent = "Configuration",
            Options = nameof(HeaderOptions))]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendOrUpdateRow))]
        public string? KeyColumn { get; set; }

        [FEDecorator(Label = "Row Number", Type = FeComponentType.Select, RowId = 90, Parent = "Configuration",
            Options = nameof(RowIndexOptions))]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.UpdateRowByRange))]
        public string? TargetRowNumber { get; set; }

        [FEDecorator(Label = "Start Index", Type = FeComponentType.Number, RowId = 100, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteDimension))]
        public int? StartIndex { get; set; }

        [FEDecorator(Label = "End Index", Type = FeComponentType.Number, RowId = 110, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.DeleteDimension))]
        public int? EndIndex { get; set; }

        [FEDecorator(Label = "Row Values", Type = FeComponentType.Code_editor, RowId = 120, Parent = "Configuration", TextFormat = TextFormat.JSON,
            Tooltip = "Provide a JSON object mapping column headers to values.")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendRow))]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.AppendOrUpdateRow))]
        [DependencyDecorator(Tab = "Google Sheets", Control = nameof(SelectedAction), Operator = Operator.Equals, Value = nameof(GoogleSheetsActionType.UpdateRowByRange))]
        public string? RowValuesJson { get; set; }

        [FEDecorator(Label = "Response", Type = FeComponentType.DataType, RowId = 1000, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.Output)]
        public object? Response { get; set; }

        public Task Execute()
        {
            // Runtime execution will be implemented per user story in subsequent iterations.
            return Task.CompletedTask;
        }

        [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(SelectedAction),
            InputControls = new[] { nameof(SelectedAction) },
            OutputControls = new[] { nameof(DriveId), nameof(SpreadsheetId), nameof(SheetId), nameof(KeyColumn), nameof(TargetRowNumber) },
            OutputTarget = OutputTarget.Options)]
        public Task OnActionChanged()
        {
            ResetActionScopedOptions();
            return ShouldLoadDesignTimeData() ? OnCredentialsChanged() : Task.CompletedTask;
        }

        [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Credentials),
            InputControls = new[] { nameof(Credentials), nameof(SelectedAction) }, OutputControls = new[] { nameof(DriveId) }, OutputTarget = OutputTarget.Options)]
        public async Task OnCredentialsChanged()
        {
            ResetActionScopedOptions();
            if (!ShouldLoadDesignTimeData())
            {
                return;
            }

            var credentials = Credentials;
            if (credentials?.Client == null)
            {
                return;
            }

            var driveClient = new GoogleDriveClient(credentials);
            var drives = await driveClient.ListDrivesAsync().ConfigureAwait(false);
            DriveOptions.Clear();
            foreach (var drive in drives)
            {
                if (!string.IsNullOrWhiteSpace(drive.Id))
                {
                    var display = string.IsNullOrWhiteSpace(drive.Name) ? drive.Id : drive.Name;
                    DriveOptions.Add(new OptionModel { name = display, value = drive.Id });
                }
            }

            if (DriveOptions.All(option => option.value != "root"))
            {
                DriveOptions.Insert(0, new OptionModel { name = "My Google Drive", value = "root" });
            }
        }

        [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(DriveId),
            InputControls = new[] { nameof(Credentials), nameof(DriveId) }, OutputControls = new[] { nameof(SpreadsheetId) }, OutputTarget = OutputTarget.Options)]
        public async Task OnDriveChanged()
        {
            SpreadsheetOptions.Clear();
            SheetOptions.Clear();
            HeaderOptions.Clear();
            RowIndexOptions.Clear();

            if (!ShouldLoadDesignTimeData() || string.IsNullOrWhiteSpace(DriveId))
            {
                return;
            }

            var credentials = Credentials;
            if (credentials?.Client == null)
            {
                return;
            }

            var driveClient = new GoogleDriveClient(credentials);
            var driveId = DriveId;
            if (driveId == null)
            {
                return;
            }

            var spreadsheets = await driveClient.ListSpreadsheetsAsync(driveId).ConfigureAwait(false);
            foreach (var file in spreadsheets)
            {
                if (!string.IsNullOrWhiteSpace(file.Id))
                {
                    var display = string.IsNullOrWhiteSpace(file.Name) ? file.Id : file.Name;
                    SpreadsheetOptions.Add(new OptionModel { name = display, value = file.Id });
                }
            }
        }

        [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(SpreadsheetId),
            InputControls = new[] { nameof(Credentials), nameof(SpreadsheetId) }, OutputControls = new[] { nameof(SheetId) }, OutputTarget = OutputTarget.Options)]
        public async Task OnSpreadsheetChanged()
        {
            SheetOptions.Clear();
            HeaderOptions.Clear();
            RowIndexOptions.Clear();

            if (!ShouldLoadDesignTimeData() || string.IsNullOrWhiteSpace(SpreadsheetId))
            {
                return;
            }

            var credentials = Credentials;
            if (credentials?.Client == null)
            {
                return;
            }

            var sheetsClient = new GoogleSheetsClient(credentials);
            var spreadsheetId = SpreadsheetId;
            if (spreadsheetId == null)
            {
                return;
            }

            var spreadsheet = await sheetsClient.GetSpreadsheetAsync(spreadsheetId).ConfigureAwait(false);
            if (spreadsheet?.Sheets == null)
            {
                return;
            }

            foreach (var sheet in spreadsheet.Sheets)
            {
                if (sheet?.Properties == null)
                {
                    continue;
                }

                var sheetId = sheet.Properties.SheetId.ToString();
                var display = string.IsNullOrWhiteSpace(sheet.Properties.Title) ? sheetId : sheet.Properties.Title;
                SheetOptions.Add(new OptionModel { name = display, value = sheetId });
            }
        }

        [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(SheetId),
            InputControls = new[] { nameof(Credentials), nameof(SpreadsheetId), nameof(SheetId), nameof(SelectedAction) },
            OutputControls = new[] { nameof(KeyColumn), nameof(TargetRowNumber) }, OutputTarget = OutputTarget.Options)]
        public async Task OnSheetChanged()
        {
            HeaderOptions.Clear();
            RowIndexOptions.Clear();

            var spreadsheetId = SpreadsheetId;
            var sheetId = SheetId;

            if (!ShouldLoadDesignTimeData() || string.IsNullOrWhiteSpace(spreadsheetId) || string.IsNullOrWhiteSpace(sheetId))
            {
                return;
            }

            if (spreadsheetId == null || sheetId == null)
            {
                return;
            }

            var credentials = Credentials;
            if (credentials?.Client == null)
            {
                return;
            }

            var sheetsClient = new GoogleSheetsClient(credentials);
            var sheetOption = SheetOptions.FirstOrDefault(option => option.value == sheetId);
            var sheetName = sheetOption?.name ?? sheetId;
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                return;
            }

            if (RequiresHeaders())
            {
                var headers = await sheetsClient.BuildHeaderOptionsAsync(spreadsheetId, sheetName).ConfigureAwait(false);
                HeaderOptions.AddRange(headers);
            }

            if (RequiresRowSelection())
            {
                var rows = await sheetsClient.BuildRowNumberOptionsAsync(spreadsheetId, sheetName).ConfigureAwait(false);
                RowIndexOptions.AddRange(rows);
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

        private bool ShouldLoadDesignTimeData()
        {
            return Credentials?.Client != null && !string.IsNullOrWhiteSpace(SelectedAction);
        }

        private bool RequiresHeaders()
        {
            return Enum.TryParse(SelectedAction, out GoogleSheetsActionType actionType) &&
                (actionType == GoogleSheetsActionType.AppendRow ||
                 actionType == GoogleSheetsActionType.AppendOrUpdateRow ||
                 actionType == GoogleSheetsActionType.UpdateRowByRange);
        }

        private bool RequiresRowSelection()
        {
            return Enum.TryParse(SelectedAction, out GoogleSheetsActionType actionType) &&
                actionType == GoogleSheetsActionType.UpdateRowByRange;
        }

        private static List<OptionModel> BuildActionOptions()
        {
            var options = new List<OptionModel>();
            foreach (var action in Enum.GetValues(typeof(GoogleSheetsActionType)).Cast<GoogleSheetsActionType>())
            {
                options.Add(new OptionModel
                {
                    name = FormatActionName(action),
                    value = action.ToString()
                });
            }

            return options;
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
}
