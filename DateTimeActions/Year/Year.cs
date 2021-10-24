using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Actions.DateAndTime
{
    [ClassDecorator(Name = "Return Year", Shape = ActionShape.Circle, Description = "Retuns the year of the date which was input by the user", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class Year : IAction
    {
        #region Properties

        [FEDecorator(Label = "Select Date", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime InputDate { get; set; }

        [FEDecorator(Label = "Output Year", Type = FeComponentType.Number, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public int OutputYear { get; set; }

        #endregion

        public Year(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            OutputYear = InputDate.Year;
        }
        #endregion
    }
}
