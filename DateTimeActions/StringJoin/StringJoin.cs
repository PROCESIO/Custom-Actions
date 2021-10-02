using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Actions
{
    [ClassDecorator(Name = "String Join", Shape = ActionShape.Circle, Description = "Concatenates the members of a " +
        "collection, using the specified separator between each member.", IsTestable = true,
        Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class StringJoin : IAction
    {
        #region Properies
        [FEDecorator(Label = "Input String List", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public IEnumerable<string> InputListOfStrings { get; set; }

        [FEDecorator(Label = "Input Separator", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputSeparator { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Text, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string Output { get; set; }
        #endregion

        #region Execute
        public async Task Execute()
        {
            if (InputListOfStrings == null)
            {
                throw new Exception("Input list was null. Please add item to it.");
            }

            Output = string.Join(InputSeparator, InputListOfStrings);
        }
        #endregion
    }
}
