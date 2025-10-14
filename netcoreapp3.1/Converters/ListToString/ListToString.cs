using Newtonsoft.Json;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListToString
{
    [ClassDecorator(Name = "List ToString", Shape = ActionShape.Square, Description = "Convert any list of objects to a string", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ListToString : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input list", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration",
            Tooltip = "Input value which can be a list of any type.")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> Input { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.DataType, RowId = 2, Tab = "Configuration")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string Output { get; set; }
        #endregion

        public ListToString(IServiceProvider service)
        {
        }

        public ListToString()
        {
        }

        public async Task Execute()
        {
            if (Input == null)
            {
                Output = string.Empty;
                return;
            }

            Output = JsonConvert.SerializeObject(Input);
        }

    }
}