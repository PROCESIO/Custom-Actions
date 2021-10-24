using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace com.ncd.ActionLib.Actions.DateAndTime
{
    [ClassDecorator(Name = "Return Month", Shape = ActionShape.Circle, Description = "Retuns the month of the date which was input by the user", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class Month : IAction
    {
        #region Properties

        [FEDecorator(Label = "Select Date", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime InputDate { get; set; }

        [FEDecorator(Label = "Output Month", Type = FeComponentType.Number, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public int OutputMonth { get; set; }

        #endregion

        public Month(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            OutputMonth = InputDate.Month;
        }
        #endregion
    }
}
