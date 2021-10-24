using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace com.ncd.ActionLib.Actions.Lists
{
    [ClassDecorator(Name = "List Minimum Return", Shape = ActionShape.Circle, Description = "Returns the minimum value from a list of numbers", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ListMin : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input a list of numbers", Type = FeComponentType.Number, Tooltip = "E.g [1,2,3]", RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> InputList { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Number, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public object Result { get; set; }
        #endregion

        public ListMin(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            double min;
            if (InputList == null)
            {
                throw new Exception("The input was null.");
            }
            try
            {
                min = InputList.Min(item => Convert.ToDouble(item));
            }
            catch (Exception ex)
            {
                if (ex is FormatException)
                {
                    throw new Exception("The input was not a number. Please try again.");
                }
                else if (ex is OverflowException)
                {
                    throw new Exception("The input is out of scope.");
                }
                else
                {
                    throw new Exception("The input was not valid, please revise it.");
                }

            }

            if (int.TryParse(min.ToString(), out int integerMin))
            {
                Result = integerMin;
            }
            else
            {
                Result = min;
            }
        }
        #endregion
    }
}
