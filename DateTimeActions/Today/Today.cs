using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Threading.Tasks;

namespace Today
{
   [ClassDecorator(Name = "Today", Shape = ActionShape.Square, Description = "Displays a string that represent the " +
        "current date", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class Today : IAction
    {
        #region Properties
        [FEDecorator(Label = "Output Date", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public DateTime OutputDate { get; set; }
        #endregion

        public Today(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            OutputDate = DateTime.Today;
        }
    }
    #endregion

}