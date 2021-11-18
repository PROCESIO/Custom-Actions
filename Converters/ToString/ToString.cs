using Newtonsoft.Json;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Threading.Tasks;

namespace com.ncd.ActionLib.Actions.Converters
{
    [ClassDecorator(Name = "ToString", Shape = ActionShape.Circle, Description = "Convert any object to a string",
        IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    class ToString : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input String", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public object Input { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string Output { get; set; }
        #endregion

        public ToString(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            Output = JsonConvert.SerializeObject(Input);
        }
        #endregion
    }
}
