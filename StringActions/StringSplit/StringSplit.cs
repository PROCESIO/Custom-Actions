using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Actions
{
    [ClassDecorator(Name = "String Split", Shape = ActionShape.Circle, Description = "Returns a list of strings that " +
        "contains the substrings in this instance that are delimited by element of a specified string",
        IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class StringSplit : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input String", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputString { get; set; }

        [FEDecorator(Label = "Input Separator", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputSeparator { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Text, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public IEnumerable<string> Output { get; set; }
        #endregion

        #region Execute
        public async Task Execute()
        {
            if (InputString == null)
            {
                throw new System.Exception("Input was null. Please fill it.");
            }

            Output = InputString.Split(InputSeparator);

        }
        #endregion
    }
}