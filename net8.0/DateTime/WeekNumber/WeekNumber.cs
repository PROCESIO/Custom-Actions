using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace WeekNumber
{
    [ClassDecorator(Name = "Week Number", Shape = ActionShape.Square, Description = "Returns the week number for the given date." +
         "The week number indicates where the week falls numerically within a year.",
         IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class Weeknum : IAction
    {
        #region Options
        private IEnumerable<OptionModel> CalendarWeekRuleOptions { get; } = new List<OptionModel>()
        {
           new OptionModel(){ name = "FirstDay", value = 0},
           new OptionModel(){ name = "FirstFullWeek", value = 1},
           new OptionModel(){ name = "FirstFourDayWeek", value = 2}
        };

        private IEnumerable<OptionModel> FirstDayOfWeekOptions { get; } = new List<OptionModel>()
        {
           new OptionModel(){ name = "Sunday", value = 0},
           new OptionModel(){ name = "Monday", value = 1}
        };
        #endregion

        #region Properties
        [FEDecorator(Label = "Input Date", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime InputDate { get; set; }

        [FEDecorator(Label = "First Week Rule", Type = FeComponentType.Select, Options = "CalendarWeekRuleOptions", RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int FirstWeekRule { get; set; }

        [FEDecorator(Label = "First Day Of Week", Type = FeComponentType.Select, Options = "FirstDayOfWeekOptions", RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int FirstDay { get; set; }

        [FEDecorator(Label = "Week of Year", Type = FeComponentType.Number, RowId = 4, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int Output { get; set; }
        #endregion

        public Weeknum(IServiceProvider service) { }

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

            DateTimeFormatInfo cultureInfo = DateTimeFormatInfo.CurrentInfo;
            Calendar calendar = cultureInfo.Calendar;

            CalendarWeekRule calendarWeekRule;
            if (FirstWeekRule == 0)
            {
                calendarWeekRule = CalendarWeekRule.FirstDay;
            }
            else if (FirstWeekRule == 1)
            {
                calendarWeekRule = CalendarWeekRule.FirstFourDayWeek;
            }
            else
            {
                calendarWeekRule = CalendarWeekRule.FirstFullWeek;
            }

            DayOfWeek firstDayOfWeek;
            if (FirstDay == 0)
            {
                firstDayOfWeek = DayOfWeek.Sunday;
            }
            else
            {
                firstDayOfWeek = DayOfWeek.Monday;
            }

            Output = calendar.GetWeekOfYear(InputDate, calendarWeekRule, firstDayOfWeek);
        }
    }
    #endregion

}