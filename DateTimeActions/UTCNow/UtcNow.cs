using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Threading.Tasks;

namespace UTCNow
{
    [ClassDecorator(Name = "Display UTC", Shape = ActionShape.Circle, Description = "Displays the current UTC", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class UtcNow : IAction
    {
        #region Properties
        [FEDecorator(Label = "Output UTC", Type = FeComponentType.Date_Time, RowId = 1, Tab = "Details")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public DateTime OutputModifiedTime { get; set; }
        #endregion
        #region Execute
        public async Task Execute()
        {
            OutputModifiedTime = DateTime.UtcNow;
        }
        #endregion
    }
}