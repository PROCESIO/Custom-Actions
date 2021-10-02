using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Threading.Tasks;

namespace Actions
{
    [ClassDecorator(Name = "String IndexOf", Shape = ActionShape.Circle, Description = "Reports the zero-based index of the " +
        "first occurrence of the specified character in this string", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    class StringIndexOf : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input String", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputString { get; set; }

        [FEDecorator(Label = "Input Value to look for", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputValueToLookFor { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Number, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int Output { get; set; }
        #endregion

        #region Execute
        public async Task Execute()
        {
            if (InputString == null || InputValueToLookFor == null)
            {
                throw new System.Exception("Input was null. Please, fill it.");
            }
            Output = InputString.IndexOf(InputValueToLookFor);
        }
        #endregion
    }
}
