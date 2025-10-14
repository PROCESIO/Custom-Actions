using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Weekday
{
    [ClassDecorator(Name = "Weekday", Shape = ActionShape.Square, Description = "Displays an integer that represents the day " +
    "of the week, by giving the date", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class Weekday : IAction
    {
        #region Options
        private IEnumerable<OptionModel> WeekNumberingOptions { get; } = new List<OptionModel>()
        {
           new OptionModel(){ name = "Week begins on Sunday (1) and ends on Saturday (7)", value = 1},
           new OptionModel(){ name = "Week begins on Monday (1) and ends on Sunday (7)", value = 2},
           new OptionModel(){ name = "Week begins on Monday (0) and ends on Sunday (6)", value = 3}
        };
        #endregion

        #region Properties
        [FEDecorator(Label = "Input Date", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime InputDate { get; set; }

        [FEDecorator(Label = "Week Numbering Options", Type = FeComponentType.Select, Options = "WeekNumberingOptions", RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int WeekdayReturnType { get; set; }

        [FEDecorator(Label = "Output Weekday", Type = FeComponentType.Number, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int OutputWeekday { get; set; }
        #endregion

        public Weekday(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            if (InputDate == null)
            {
                throw new Exception("Input was null. Please, fill it.");
            }

            if (InputDate.Year <= 0 || InputDate.Month <= 0 || InputDate.Day <= 0 || InputDate.Day > 31 || InputDate.Month > 12)
            {
                throw new Exception("Invalid values.");
            }

            // (int)DayOfWeek returns a value from 0(Sunday) to 6(Saturday)
            if (WeekdayReturnType == 1)
            {
                OutputWeekday = (int)InputDate.DayOfWeek + 1;
            }
            else if (WeekdayReturnType == 3)
            {
                OutputWeekday = ((int)InputDate.DayOfWeek + 6) % 7;
            }
            else
            {
                OutputWeekday = ((int)InputDate.DayOfWeek + 6) % 7 + 1;
            }

        }
        #endregion

    }
}