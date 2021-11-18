using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Text;
using System.Threading.Tasks;

namespace com.ncd.ActionLib.Actions.Converters
{
    [ClassDecorator(Name = "String ToBase64", Shape = ActionShape.Circle, Description = "Convert string value to Base64",
        IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    class ToBase64 : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input String", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string Input { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Text, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string Output { get; set; }
        #endregion

        public ToBase64(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(Input);
            Output = Convert.ToBase64String(plainTextBytes);
        }
        #endregion
    }
}