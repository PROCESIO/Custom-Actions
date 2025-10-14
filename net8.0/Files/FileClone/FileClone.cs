using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;

namespace FileClone;

[ClassDecorator(Name = "Custom Template", Shape = ActionShape.Square, Description = "test",
        Classification = Classification.cat1)]
[Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
public class FileClone : IAction
{
    //Input file
    [FEDecorator(Label = "file in", Type = FeComponentType.File, Tab = "Configuration")]
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = false)]
    public FileModel Input1 { get; set; }

    // Output file
    [FEDecorator(Label = "file out", Type = FeComponentType.File, Tab = "Configuration")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public FileModel Output1 { get; set; }

    public async Task Execute()
    {
        Output1 = Input1.Clone();
    }
}
