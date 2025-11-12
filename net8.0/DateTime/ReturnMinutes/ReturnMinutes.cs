using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Actions.DateAndTime
{
    [ClassDecorator(Name = "Return Minutes", Shape = ActionShape.Circle, Description = "Retuns the minutes of the time interval which was input by the user", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ReturnMinutes : IAction
    {

        #region Properties
        [FEDecorator(Label = "Input time interval", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime InputTime { get; set; }

        [FEDecorator(Label = "Output Minutes", Type = FeComponentType.Number, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public int OutputMinutes { get; set; }

        #endregion

        public ReturnMinutes(IServiceProvider service) { }


        #region Execute
        public async Task Execute()
        {

            OutputMinutes = InputTime.Minute;
        }
        #endregion
    }
}
