using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Threading.Tasks;

namespace Actions
{
    [ClassDecorator(Name = "Add operation", Shape = ActionShape.Circle, Description = "Add operation for two objects",
        IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class Add : IAction
    {
        #region Properties
        [FEDecorator(Label = "First number", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public object FirstInput { get; set; }

        [FEDecorator(Label = "Second number", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public object SecondInput { get; set; }

        [FEDecorator(Label = "Addition Result", Type = FeComponentType.Text, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public object Sum { get; set; }
        #endregion

        #region Execute
        public async Task Execute()
        {
            if (string.IsNullOrEmpty(FirstInput.ToString()) || string.IsNullOrEmpty(SecondInput.ToString()))
            {
                throw new Exception("Input was null. Please, fill it.");
            }

            // Convert Input Object to Double
            bool success1 = double.TryParse(FirstInput.ToString(), out double result1Double);
            bool success2 = double.TryParse(SecondInput.ToString(), out double result2Double);
            if (success1 == true && success2 == true)
            {
                Sum = result1Double + result2Double;
            }

        }
        #endregion

    }
}
