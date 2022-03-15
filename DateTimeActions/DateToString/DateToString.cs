using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DateToString
{
    [ClassDecorator(Name = "DateTime to String", Shape = ActionShape.Square, Description = "Converts DateTime to string.",
           IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class DateToString : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input DateTime: ", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Configuration Tab",
            Tooltip = "Your DateTime value.")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public DateTime Input { get; set; }

        [FEDecorator(Label = "Select Language: ", Type = FeComponentType.Select, RowId = 2, Tab = "Configuration Tab",
            Options = "CultureOptionsList", Tooltip = "Choose your preferred language.")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public string Culture { get; set; }

        #region CultureOptions
        private IEnumerable<OptionModel> CultureOptionsList { get; } = new List<OptionModel>()
        {
            new OptionModel()
            {
                name = "Română(RO)",
                value = "ro-RO"
            },
            new OptionModel(){
                name = "English(UK)",
                value = "en-GB"
            },
            new OptionModel(){
                name = "English(US)",
                value = "en-US"
            },
            new OptionModel(){
                name = "Ελληνικά(GR)",
                value = "el-GR"
            },
            new OptionModel(){
                name = "Español(ES)",
                value = "es-ES"
            },
            new OptionModel(){
                name = "中国人(CN)",
                value = "zh-CN"
            },
            new OptionModel(){
                name = "Pусский(RU)",
                value = "ru-RU"
            },
           new OptionModel(){
                name = "Deutsch(DE)",
                value = "de-DE"
            },
            new OptionModel(){
                name = "हिन्दी(IN)",
                value = "hi-IN"
            },
            new OptionModel(){
                name = "عربي(UAE)",
                value = "ar-AE"
            }
        };
        #endregion

        [FEDecorator(Label = "Select Date Format: ", Type = FeComponentType.Select, RowId = 3, Tab = "Configuration Tab",
            Options = "FormattingOptionsList", Tooltip = "Choose how the result will be formatted.")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public string Format { get; set; }

        #region FormattingOptions
        private IEnumerable<OptionModel> FormattingOptionsList { get; } = new List<OptionModel>()
        {
            new OptionModel()
            {
                name = "6/15/2009",
                value = "d"
            },
            new OptionModel()
            {
                name = "Monday, June 15, 2009",
                value = "D"
            },
            new OptionModel()
            {
                name = "Monday, June 15, 2009 1:45 PM",
                value = "f"
            },
            new OptionModel()
            {
                name = "Monday, June 15, 2009 1:45:30 PM",
                value = "F"
            },
            new OptionModel()
            {
                name = "6/15/2009 1:45 PM",
                value = "g"
            },
            new OptionModel()
            {
                name = "6/15/2009 1:45:30 PM",
                value = "G"
            },
            new OptionModel()
            {
                name = "June 15",
                value = "m"
            },
            new OptionModel()
            {
                name = "2009-06-15T13:45:30.0000000-07:00",
                value = "o"
            },
            new OptionModel()
            {
                name = "Mon, 15 Jun 2009 20:45:30 GMT",
                value = "r"
            },
            new OptionModel()
            {
                name = "2009-06-15T13:45:30",
                value = "s"
            },
            new OptionModel()
            {
                name = "1:45 PM",
                value = "t"
            },
            new OptionModel()
            {
                name = "1:45:30 PM",
                value = "T"
            },
            new OptionModel()
            {
                name = "2009-06-15 13:45:30Z",
                value = "u"
            },
            new OptionModel()
            {
                name = "Monday, June 15, 2009 8:45:30 PM",
                value = "U"
            },
            new OptionModel()
            {
                name = "June 2009",
                value = "y"
            },
        };
        #endregion

        [FEDecorator(Label = "Formatted Date: ", Type = FeComponentType.DataType, RowId = 4, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string Output { get; set; }

        #endregion

        #region Constructors
        public DateToString() { }
        public DateToString(IServiceProvider service) { }
        #endregion

        #region Execute
        public async Task Execute()
        {
            if (Input == DateTime.MinValue)
            {
                throw new Exception("Input Error: Please provide a non-empty DateTime !");
            }
            if (!CultureOptionsList.Any(opt => opt.value.Equals(Culture)))
            {
                throw new Exception("Language Error: Please choose a language from the provided list !");
            }
            if (!FormattingOptionsList.Any(opt => opt.value.Equals(Format)))
            {
                throw new Exception("Format Error: Please choose a format from the provided list !");
            }

            Output = Input.ToString(Format, CultureInfo.CreateSpecificCulture(Culture));
        }
        #endregion
    }
}
