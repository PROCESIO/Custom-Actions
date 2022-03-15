using CsvHelper;
using ExportToCSV.Helpers;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportToCSV
{
    [ClassDecorator(Name = "Export to CSV", Shape = ActionShape.Square,
       Description = "Exports a list (of primitives/simple or of data model) to a .csv file, with export options",
       Classification = Classification.cat1, IsTestable = true)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ExportToCSV : IAction
    {
        [FEDecorator(Label = "Object to export", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> ObjectToExport { get; set; }

        [FEDecorator(Label = "Column delimiter", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration",
            DefaultValue = ",", Tooltip = "Recommended values: ',' (comma) ';' (semicolon) '|' (pipe) or \\t (TAB)")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string Delimiter { get; set; }

        [FEDecorator(Label = "Export header", Type = FeComponentType.Check_box, RowId = 3, Tab = "Configuration",
            DefaultValue = true)]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = false, Expects = ExpectedType.Boolean)]
        public bool ExportHeader { get; set; }

        [FEDecorator(Label = "File name", Type = FeComponentType.Text, RowId = 4, Tab = "Configuration",
            Tooltip = "The .csv file extension will be added automatically")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string FileName { get; set; }

        [FEDecorator(Label = "Output file", Type = FeComponentType.File, RowId = 5, Tab = "Configuration")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public FileModel OutputFile { get; set; }

        public ExportToCSV(IServiceProvider service)
        {
        }

        public ExportToCSV()
        {
        }

        public async Task Execute()
        {
            if (ObjectToExport == null)
            {
                throw new Exception("Input list is null. Please add items to it.");
            }

            if (ObjectToExport.Count() == 0)
            {
                throw new Exception("Input list is empty. Please add items to it.");
            }

            //Replace \t string with TAB character
            if (Delimiter == "\\t") { Delimiter = "\t"; }

            //Remove leading & trailing white spaces
            FileName = FileName.Trim();

            //If the file name does not end with .csv we add the file extension
            if (!FileName.EndsWith(".csv"))
            {
                FileName = Path.ChangeExtension(FileName, ".csv");
            }

            await using var stream = new MemoryStream();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);

            CsvHelper.Configuration.CsvConfiguration csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture);
            csvConfig.Delimiter = Delimiter;
            csvConfig.HasHeaderRecord = ExportHeader;

            await using var csv = new CsvWriter(writer, csvConfig);

            foreach (var item in ObjectToExport)
            {
                var rObj = new System.Dynamic.ExpandoObject() as IDictionary<string, Object>;
                if (item.IsJObject())
                {
                    //Flaten JSON
                    Dictionary<string, object> flatenJSONdict = item.ToString().DeserializeAndFlatten();

                    foreach (var flat in flatenJSONdict)
                    {
                        rObj.Add(flat.Key, flat.Value);
                    }

                    csv.WriteRecord(rObj);
                    csv.NextRecord();
                }
                else
                {
                    rObj.Add("Column", item);

                    csv.WriteRecord(rObj);
                    csv.NextRecord();
                }
            }

            writer.Flush();

            stream.Seek(0, SeekOrigin.Begin);
            OutputFile = new FileModel(stream, FileName);
        }
    }
}