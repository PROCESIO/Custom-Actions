using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.ncd.ActionLib.Actions.Lists
{
    [ClassDecorator(Name = "Return List Element Count", Shape = ActionShape.Circle, Description = "Returns the list's number of elements", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ListCount : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input a list of elements", Type = FeComponentType.Number, Tooltip = "E.g [1,2,3]", RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> InputList { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Number, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public object Result { get; set; }
        #endregion

        public ListCount(IServiceProvider service) { }

        #region Execute

        public async Task Execute()
        {
            if (InputList == null)
            {
                throw new Exception("The input was null.");
            }

            Result = InputList.Count();

        }

        #endregion
    }
}
