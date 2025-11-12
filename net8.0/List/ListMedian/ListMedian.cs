using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.ncd.ActionLib.Actions.Lists
{
    [ClassDecorator(Name = "List Median Value Return", Shape = ActionShape.Square, Description = "Returns the median value of a list", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ListMedian : IAction
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

        public ListMedian(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            if (InputList == null || InputList.Any() == false || InputList.ToList().Count == 0)
            {
                throw new Exception("The input was null.");
            }
            List<double> doubleList;
            try
            {
                doubleList = InputList.Select(item => Convert.ToDouble(item)).ToList();
                doubleList.Sort();
            }
            catch (Exception)
            {
                throw new Exception("The input list has invalid numerical value");
            }
            if (doubleList.Count % 2 != 0)
            {
                var firstCaseResult = doubleList[(int)(doubleList.Count - 1) / 2];
                if (int.TryParse(firstCaseResult.ToString(), out int integerResult))
                {
                    Result = integerResult;
                }
                else
                {
                    Result = firstCaseResult;
                }
            }
            else
            {
                double doubleResult = (doubleList[(int)(doubleList.Count / 2) - 1] + (doubleList[(int)(doubleList.Count / 2)])) / 2;
                if (int.TryParse(doubleResult.ToString(), out int integerResult))
                {
                    Result = integerResult;
                }
                else
                {
                    Result = doubleResult;
                }
            }
        }
        #endregion
    }
}
