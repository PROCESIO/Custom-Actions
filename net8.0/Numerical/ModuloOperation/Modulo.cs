using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Threading.Tasks;

namespace Actions
{
    [ClassDecorator(Name = "Modulo operation", Shape = ActionShape.Circle, Description = "Modulo operation for two integers",
        IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class Modulo : IAction
    {
        #region Properties
        [FEDecorator(Label = "First number", Type = FeComponentType.Number, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int FirstInput { get; set; }

        [FEDecorator(Label = "Second number", Type = FeComponentType.Number, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int SecondInput { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Number, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int Result { get; set; }
        #endregion

        #region Execute
        public async Task Execute()
        {
            if (string.IsNullOrEmpty(FirstInput.ToString()) || string.IsNullOrEmpty(SecondInput.ToString()))
            {
                throw new Exception("Input was null. Please, fill it.");
            }

            Result = FirstInput % SecondInput;
        }
        #endregion
    }
}
