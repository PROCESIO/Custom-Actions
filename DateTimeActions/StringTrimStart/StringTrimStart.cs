using System;
using System.Threading.Tasks;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;

namespace Actions
{
    [ClassDecorator(Name = "String Trim", Shape = ActionShape.Circle, Description = "The white spaces are removed from a " +
        "string, at the beginning of the string", IsTestable = true,
        Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class StringTrimStart : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input String", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputString { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string Output { get; set; }
        #endregion

        #region Execute
        public async Task Execute()
        {
            if (InputString == null)
            {
                throw new Exception("Input was null. Please, fill it.");
            }

            Output = InputString.TrimStart();
        }
        #endregion
    }
}
