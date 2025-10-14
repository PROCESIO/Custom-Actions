using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace list.returnnlastelements
{
    [ClassDecorator(Name = "Return Last N Elements", Shape = ActionShape.Square, Description = "Returns the last N elements", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ReturnNLastElements : IAction
    {
        private IEnumerable<OptionModel> UserOptions { get; } = new List<OptionModel>()
        {
            new OptionModel(){ name = "Unsorted", value = 1},
            new OptionModel(){ name = "Sorted ascendingly", value = 2},
            new OptionModel(){ name = "Sorted descendingly", value = 3}
        };
        #region Properties
        [FEDecorator(Label = "Input a list", Type = FeComponentType.Number, Tooltip = "E.g [1,2,3]", RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> InputList { get; set; }

        [FEDecorator(Label = "Input the number of elements to receive", Type = FeComponentType.Number, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public int ElementsN { get; set; }

        [FEDecorator(Label = "Sorted:", Type = FeComponentType.Select, Options = "UserOptions", RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int UserChoice { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Number, RowId = 4, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> Result { get; set; }
        #endregion

        public ReturnNLastElements(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            if (InputList == null)
            {
                throw new Exception("Input list was null.");
            }
            if (UserChoice == 1)
            {
                Result = InputList.TakeLast(ElementsN);
            }
            else if (UserChoice == 3)
            {
                Result = OrderByHelper.GetSortedDesc(InputList.TakeLast(ElementsN));
            }
            else if (UserChoice == 2)
            {
                Result = OrderByHelper.GetSortedAsc(InputList.TakeLast(ElementsN));
            }
            else
            {
                throw new Exception("Choice is invalid.");
            }
        }
        #endregion
    }
}
