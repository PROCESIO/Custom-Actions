namespace GoogleSheetConnector.Models;

public enum GoogleSheetsActionType
{
    CreateSpreadsheet = 1,
    DeleteSpreadsheet,
    AppendRow,
    AppendOrUpdateRow,
    ClearRange,
    CreateSheet,
    DeleteSheet,
    DeleteDimension,
    GetRows,
    UpdateRowByRange
}
