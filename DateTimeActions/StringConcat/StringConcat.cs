using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Threading.Tasks;

namespace Actions
{
    [ClassDecorator(Name = "String Concat", Shape = ActionShape.Circle, Description = "Concatenates two strings",
        IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    class StringConcat : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input String 1", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputString1 { get; set; }

        [FEDecorator(Label = "Input String 2", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputString2 { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Text, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string Output { get; set; }
        #endregion

        #region Execute
        public async Task Execute()
        {
            Output = string.Concat(InputString1, InputString2);
        }
        #endregion
    }
}
