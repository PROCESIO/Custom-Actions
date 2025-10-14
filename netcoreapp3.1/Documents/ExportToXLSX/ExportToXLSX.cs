using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace ExportToXLSX
{
    [ClassDecorator(Name = "Export to XLSX", Shape = ActionShape.Square,
        Description = "Exports a list (of primitives/simple or of data model) to a .xlsx file",
        Classification = Classification.cat1, IsTestable = true)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ExportToXLSX : IAction
    {
        [FEDecorator(Label = "Object to export", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> ObjectToExport { get; set; }

        [FEDecorator(Label = "Export header", Type = FeComponentType.Check_box, RowId = 2, Tab = "Configuration",
            DefaultValue = true)]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = false, Expects = ExpectedType.Boolean)]
        public bool ExportHeader { get; set; }

        [FEDecorator(Label = "File name", Type = FeComponentType.Text, RowId = 3, Tab = "Configuration",
            Tooltip = "The .xlsx file extension will be added automatically")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string FileName { get; set; }

        [FEDecorator(Label = "Output file", Type = FeComponentType.File, RowId = 4, Tab = "Configuration")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public FileModel OutputFile { get; set; }

        public ExportToXLSX(IServiceProvider service)
        {
        }

        public ExportToXLSX()
        {
        }

        public async Task Execute()
        {
            if (ObjectToExport == null || !ObjectToExport.Any())
            {
                throw new Exception("Input list is null. Please add items to it.");
            }

            //Remove leading & trailing white spaces
            FileName = FileName.Trim();

            //If the file name does not end with .xlsx we add the file extension
            if (!FileName.EndsWith(".xlsx"))
            {
                FileName = Path.ChangeExtension(FileName, ".xlsx");
            }

            await using MemoryStream stream = ExportInputList(ObjectToExport, ExportHeader);
            stream.Position = 0;
            OutputFile = new FileModel(stream, FileName);
        }

        private static JObject? ConvertToJObject(object value)
        {
            if (value is JObject jObject)
            {
                return jObject;
            }

            try
            {
                var result = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(value));
                return result;
            }
            catch
            {
                return null;
            }
        }

        private MemoryStream ExportInputList(IEnumerable<object> inputList, bool withHeader)
        {
            var sheetData = new SheetData();
            var firstRow = true;

            foreach (var item in inputList)
            {
                var itemJson = ConvertToJObject(item);
                Row? row;
                if (itemJson != null)
                {
                    row = GetRowDataFromJson(itemJson, withHeader, ref firstRow);
                    if (row is null) continue;
                }
                else
                {
                    row = GetRowDataFromObject(item, withHeader, ref firstRow);
                }
                sheetData.AppendChild(row);
            }

            MemoryStream stream = new MemoryStream();

            using var workbook = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
            var workbookPart = workbook.AddWorkbookPart();
            if (workbook.WorkbookPart == null)
            {
                throw new Exception("Could not create the workbook part.");
            }
            workbook.WorkbookPart.Workbook = new Workbook();

            var sheetPart = workbook.WorkbookPart.AddNewPart<WorksheetPart>();
            sheetPart.Worksheet = new Worksheet(sheetData);

            Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet { Id = workbookPart.GetIdOfPart(sheetPart), SheetId = 1, Name = "Sheet1" };
            sheets.Append(sheet);

            workbookPart.Workbook.Save();

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private static Row GetRowDataFromObject(object item, bool withHeader, ref bool firstRow)
        {
            //Add Header data
            if (withHeader && firstRow)
            {
                var headerRow = new Row();
                var cellH = new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue("Column")
                };
                headerRow.AppendChild(cellH);
                firstRow = false;
                return headerRow;
            }

            //Add values
            var row = new Row();
            var cellR = new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(JsonConvert.SerializeObject(item))
            };
            row.AppendChild(cellR);
            return row;
        }

        private static Row? GetRowDataFromJson(JObject itemJson, bool withHeader, ref bool firstRow)
        {
            //Flatten JSON
            var flattenJsonDict = itemJson.ToObject<Dictionary<string, object>>();
            if (flattenJsonDict is null) return null;

            //Add Header data only once if required
            if (withHeader && firstRow)
            {
                var headerRow = new Row();
                foreach (var cellH in flattenJsonDict
                    .Select(flat => new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(flat.Key)
                    }))
                {
                    headerRow.AppendChild(cellH);
                }
                firstRow = false;
                return headerRow;
            }

            //Add values
            var row = new Row();
            foreach (var cellR in flattenJsonDict
                .Select(flat => new Cell
                {
                    DataType = CellValues.String,
                    CellValue = CreateCellValue(flat.Value)
                }))
            {
                row.AppendChild(cellR);
            }

            return row;
        }

        private static CellValue? CreateCellValue(object? value)
        {
            return value switch
            {
                null => null,
                string v => new CellValue(v),
                bool b => new CellValue(b),
                double d => new CellValue(d),
                decimal de => new CellValue(de),
                DateTime dt => new CellValue(dt),
                _ => new CellValue(JsonConvert.SerializeObject(value))
            };
        }
    }
}