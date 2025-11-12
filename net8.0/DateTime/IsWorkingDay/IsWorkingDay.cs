using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Threading.Tasks;

namespace IsWorkingDay
{
    [ClassDecorator(Name = "Is Working Day", Shape = ActionShape.Square, Description = "Verify if it's working day, by giving " +
         "the date", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class IsWorkingDay : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input Date", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime InputDate { get; set; }

        [FEDecorator(Label = "Is it a working day?", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.Boolean)]
        public bool Output { get; set; }
        #endregion

        public IsWorkingDay(IServiceProvider service) { }

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

            var day = InputDate.DayOfWeek;

            if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
            {
                Output = false;
            }
            else
            {
                Output = true;
            }
        }
    }
    #endregion

}