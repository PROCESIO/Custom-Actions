using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Actions
{
    [ClassDecorator(Name = "List Add", Shape = ActionShape.Circle, Description = "Adding a given " +
       "object into a list", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ListAdd : IAction
    {
        #region Properties
        [FEDecorator(Label = "List to which the object is added", Type = FeComponentType.Any, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> InputList { get; set; }

        [FEDecorator(Label = "Input object to be added", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public object InputToBeAdded { get; set; }
        #endregion

        #region Execute
        public async Task Execute()
        {
            if (InputList == null)
            {
                throw new System.Exception("Input list was null. Please fill it.");
            }

            List<object> InputListAux = InputList.ToList(); // A copy list of the Input List
            InputListAux.Add(InputToBeAdded); // Add the object into list

            InputList = InputListAux; // Update the Input List
        }
        #endregion
    }
}
