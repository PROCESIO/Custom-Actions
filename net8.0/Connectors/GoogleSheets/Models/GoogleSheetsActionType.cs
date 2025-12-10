namespace GoogleSheetsAction.Models;

public enum GoogleSheetsActionType
{
    CreateSpreadsheet = 1,
    DeleteSpreadsheet,
    CreateSheet,
    DeleteSheet,
    GetRows,
    AppendRow,
    AppendOrUpdateRow,
    UpdateRow,
    ClearRange,
    DeleteDimension,
}
