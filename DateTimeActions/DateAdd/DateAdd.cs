using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Actions.DateAndTime
{
    [ClassDecorator(Name = "Date Addition", Shape = ActionShape.Circle, Description = "Returns the date after being modified by the user input", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class DateAdd : IAction
    {
        private IEnumerable<OptionModel> UserOptions { get; } = new List<OptionModel>()
        {
           new OptionModel(){ name = "Year", value = 1},
           new OptionModel(){ name = "Month", value = 2},
           new OptionModel(){ name = "Day", value = 3},
           new OptionModel(){ name = "Hours", value = 4},
           new OptionModel(){ name = "Minutes", value = 5},
           new OptionModel(){ name = "Seconds", value = 6}
        };
        #region Properties
        [FEDecorator(Label = "Select date", Tooltip = "e.g 2010/05/10", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime InputDate { get; set; }
        [FEDecorator(Label = "Select the result you wish to receive", Type = FeComponentType.Select, Options = "UserOptions", RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int UserChoice { get; set; }

        [FEDecorator(Label = "Input changes ", Type = FeComponentType.Number, DefaultValue = 0, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = false)]
        public int UserChanges { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Date_Time, RowId = 4, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public DateTime AdditionResult { get; set; }
        #endregion

        public DateAdd(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            if (UserChoice == 1)
            {
                AdditionResult = InputDate.AddYears(UserChanges);
            }
            else if (UserChoice == 2)
            {
                AdditionResult = InputDate.AddMonths(UserChanges);
            }
            else if (UserChoice == 3)
            {
                AdditionResult = InputDate.AddDays(UserChanges);
            }
            else if (UserChoice == 4)
            {
                AdditionResult = InputDate.AddHours(UserChanges);
            }
            else if (UserChoice == 5)
            {
                AdditionResult = InputDate.AddMinutes(UserChanges);
            }
            else if (UserChoice == 6)
            {
                AdditionResult = InputDate.AddSeconds(UserChanges);
            }
            else
            {
                throw new Exception("Invalid input");
            }
        }
        #endregion

    }
}
