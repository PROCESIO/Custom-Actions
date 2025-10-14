using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace list.sort
{
    [ClassDecorator(Name = "List Sort", Shape = ActionShape.Circle, Description = "Sorting a list in descending or " +
        "ascending order", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ListSort : IAction
    {
        #region Options
        private IEnumerable<OptionModel> OrderOptions { get; } = new List<OptionModel>()
        {
           new OptionModel(){ name = "Ascending Order", value = "ASC"},
           new OptionModel(){ name = "Descending Order", value = "DESC"}
        };
        #endregion

        #region Properties
        [FEDecorator(Label = "List to be sorted", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> InputList { get; set; }

        [FEDecorator(Label = "Input Order (A/D)", Type = FeComponentType.Select, Options = "OrderOptions", RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string InputOrder { get; set; }
        #endregion

        public ListSort(IServiceProvider service) { }

        #region Methods
        private IEnumerable<object> Sort(IEnumerable<object> list, string order)
        {
            if (order == "DESC")
            {
                var result = from obj in list
                             orderby obj descending
                             select obj;
                return result;
            }
            else
            {
                var result = from obj in list
                             orderby obj
                             select obj;
                return result;
            }

        }
        #endregion

        #region Execute
        public async Task Execute()
        {
            if (InputList == null)
            {
                throw new System.Exception("Input list was null. Please add items to it.");
            }

            bool hasDataModels = false, hasPrimitives = false;
            foreach (var item in InputList)
            {
                if (hasDataModels && hasPrimitives)
                {
                    throw new System.Exception("The list was not fill right. Check the data types of the items.");
                }

                if (item.IsJObject()) // Check if the elements from Input List can be converted to JObj
                {
                    hasDataModels = true;
                }
                else
                {
                    hasPrimitives = true;
                }
            }
            if (hasPrimitives) // If the elements from Input List are not data models
            {
                InputList = Sort(InputList, InputOrder); // Sort Input List 
            }
            else if (hasDataModels)
            {
                var InputListAux = new List<string>();
                foreach (var item in InputList)
                {
                    // Convert to string each item from the Input List and add it to another list
                    InputListAux.Add(item.ToString());
                }
                InputList = Sort(InputListAux, InputOrder); // Sort Input List after string representation
            }
            else
            {
                throw new System.Exception("Input list was not fill right. You can not add data models " +
                    "and primitives to the same list.");
            }
        }
        #endregion
    }
}
