using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace Actions.DateAndTime
{
    [ClassDecorator(Name = "Date Difference", Shape = ActionShape.Square, Description = "Returns the difference between the second and the first date", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class DateDiff : IAction
    {
        private IEnumerable<OptionModel> UserOptions { get; } = new List<OptionModel>()
        {
           new OptionModel(){ name = "Second", value = 1},
           new OptionModel(){ name = "Minute", value = 2},
           new OptionModel(){ name = "Hour", value = 3},
           new OptionModel(){ name = "Day", value = 4},
           new OptionModel(){ name = "Week", value = 5},
           new OptionModel(){ name = "Month", value = 6},
           new OptionModel(){ name = "Quarter", value = 7},
           new OptionModel(){ name = "Year", value = 8}
        };

        #region Properties
        [FEDecorator(Label = "Select the result you wish to receive", Type = FeComponentType.Select, Options = "UserOptions", RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int UserChoice { get; set; }

        [FEDecorator(Label = "Input first date", Tooltip = "e.g 2010/05/10", Type = FeComponentType.Date_Time, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime FirstInputDate { get; set; }

        [FEDecorator(Label = "Input second date", Tooltip = "e.g 2010/05/10", Type = FeComponentType.Date_Time, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime SecondInputDate { get; set; }

        [FEDecorator(Label = "Difference result", Type = FeComponentType.Number, RowId = 4, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public object DifferenceResult { get; set; }
        #endregion
        public DateDiff(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {

            if (FirstInputDate == null || SecondInputDate == null)
            {
                throw new Exception("Input was null");
            }

            if (UserChoice == 1)
            {
                DifferenceResult = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Second, FirstInputDate, SecondInputDate);
            }
            else if (UserChoice == 2)

            {
                DifferenceResult = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Minute, FirstInputDate, SecondInputDate);

            }
            else if (UserChoice == 3)
            {
                DifferenceResult = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Hour, FirstInputDate, SecondInputDate);
            }
            else if (UserChoice == 4)
            {
                DifferenceResult = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Day, FirstInputDate, SecondInputDate);
            }
            else if (UserChoice == 5)
            {
                DifferenceResult = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.WeekOfYear, FirstInputDate, SecondInputDate);

            }
            else if (UserChoice == 6)
            {
                DifferenceResult = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Month, FirstInputDate, SecondInputDate);
            }
            else if (UserChoice == 7)
            {
                DifferenceResult = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Quarter, FirstInputDate, SecondInputDate);

            }
            else
            {
                DifferenceResult = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Year, FirstInputDate, SecondInputDate);
            }

        }
        #endregion
    }
}
