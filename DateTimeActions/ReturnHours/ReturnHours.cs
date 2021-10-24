using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Actions.DateAndTime
{
    [ClassDecorator(Name = "Return Hours", Shape = ActionShape.Circle, Description = "Retuns the hours of the time interval which was input by the user", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ReturnHours : IAction
    {
        #region Properties

        [FEDecorator(Label = "Input time interval", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime InputTime { get; set; }

        [FEDecorator(Label = "Output Hours", Type = FeComponentType.Number, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public int OutputHour { get; set; }

        #endregion

        public ReturnHours(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            OutputHour = InputTime.Hour;
        }
        #endregion
    }
}
