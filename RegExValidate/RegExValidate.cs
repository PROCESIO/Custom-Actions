using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Actions
{
    [ClassDecorator(Name = "RegEx validate", Shape = ActionShape.Square, Description = "Validate string based on RegEx expression", Classification = Classification.cat1, IsTestable = true)]
    [FEDecorator(Label = "RegEx pattern", Type = FeComponentType.Side_pannel, Tab = "Details", RowId = 1, Parent = "Configuration")]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class RegExValidate : IAction
    {
        [FEDecorator(Label = "Value to validate", Type = FeComponentType.Text, RowId = 1, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string String2Process { get; set; }

        [FEDecorator(Label = "RegEx expression", Type = FeComponentType.Text, RowId = 2, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string RegEx_Expression { get; set; }

        [FEDecorator(Label = "Ignore case", Type = FeComponentType.Check_box, RowId = 4, DefaultValue = true, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Boolean)]
        public bool Option_IgnoreCase { get; set; }

        [FEDecorator(Label = "Validation result", Type = FeComponentType.Text, RowId = 3, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.Boolean)]
        public bool ValidationResult { get; set; } = false;


        public async Task Execute()
        {
            if (!String.IsNullOrEmpty(String2Process))
            {
                RegexOptions RegExOptionsVar = RegexOptions.None;
                if (Option_IgnoreCase) { RegExOptionsVar = RegExOptionsVar | RegexOptions.IgnoreCase; }

                ValidationResult = Regex.IsMatch(String2Process, RegEx_Expression, RegExOptionsVar);
            }
        }
    }
}