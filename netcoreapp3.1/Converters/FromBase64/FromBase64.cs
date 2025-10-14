using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Text;
using System.Threading.Tasks;

namespace com.ncd.ActionLib.Actions.Converters
{
    [ClassDecorator(Name = "Base64 ToString", Shape = ActionShape.Circle, Description = "Convert Base64 string to a string",
        IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    class FromBase64 : IAction
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

        public FromBase64(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            var base64EncodedBytes = Convert.FromBase64String(Input);
            Output = Encoding.UTF8.GetString(base64EncodedBytes);
        }
        #endregion
    }
}
