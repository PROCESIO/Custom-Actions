using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Threading.Tasks;

namespace IsWeekend
{
    [ClassDecorator(Name = "Is Weekend", Shape = ActionShape.Square, Description = "Verify if it's weekend, by giving " +
           "the date. Returns true or false.", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class IsWeekend : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input Date", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime InputDate { get; set; }

        [FEDecorator(Label = "Is it weekend?", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.Boolean)]
        public bool Output { get; set; }
        #endregion

        public IsWeekend(IServiceProvider service) { }

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
                Output = true;
            }
            else
            {
                Output = false;
            }
        }
    }
    #endregion

}