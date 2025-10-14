using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;

namespace GetFileElementFromList;

[ClassDecorator(
    Name = "GetFileElementFromList",
    Shape = ActionShape.Square,
    Description = "Returns the file of the given index from the list.",
    Classification = Classification.cat1)]
[Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
public class GetFileElementFromList : IAction
{
    //Input file
    [FEDecorator(
        Label = "Input Files List",
        Type = FeComponentType.File,
        Tab = "Configuration",
        RowId = 1)]
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = false)]
    public IEnumerable<FileModel> InputFileList { get; set; }

    [FEDecorator(
        Label = "Input the index of the file element",
        Tooltip = "Starting from zero.",
        Type = FeComponentType.Number,
        RowId = 2,
        Tab = "Configuration")]
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = true)]
    public int ElementIndex { get; set; }

    // Output file
    [FEDecorator(
        Label = "Output File",
        Type = FeComponentType.File,
        Tab = "Configuration",
        RowId = 3)]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = true)]
    public FileModel OutputFile { get; set; }

    public GetFileElementFromList()
    { }

    public Task Execute()
    {
        if (InputFileList == null)
        {
            throw new Exception("Input was null");
        }

        if (InputFileList.Count() - 1 < ElementIndex)
        {
            throw new Exception("Index exceeds the list's size.");
        }

        OutputFile = InputFileList!.ToList()[ElementIndex];

        return Task.CompletedTask;
    }
}