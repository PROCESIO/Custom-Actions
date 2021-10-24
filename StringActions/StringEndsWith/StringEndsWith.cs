using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Threading.Tasks;

namespace Actions.Strings
{
    [ClassDecorator(Name = "String EndsWith", Shape = ActionShape.Circle, Description = "Verify if a string ends with a " +
        "specified string", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class StringEndsWith : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input String", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputString { get; set; }

        [FEDecorator(Label = "Input String - End", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputEnd { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Text, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.Boolean)]
        public bool Output { get; set; }
        #endregion

        public StringEndsWith(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            if (InputString == null || InputEnd == null)
            {
                throw new Exception("Input was null. Please, fill it.");
            }

            Output = InputString.EndsWith(InputEnd);
        }
        #endregion
    }
}
