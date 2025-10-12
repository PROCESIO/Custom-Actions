using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.ncd.ActionLib.Actions.Lists
{
    [ClassDecorator(Name = "List Max Return", Shape = ActionShape.Circle, Description = "Returns the maximum value from a list", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ListMax : IAction
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

        public ListMax(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {

            double max;
            if (InputList == null)
            {
                throw new Exception("The input was null.");
            }
            try
            {
                max = InputList.Max(item => Convert.ToDouble(item));
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

            if (int.TryParse(max.ToString(), out int integerMax))
            {
                Result = integerMax;
            }
            else
            {
                Result = max;
            }

        }
        #endregion
    }
}
