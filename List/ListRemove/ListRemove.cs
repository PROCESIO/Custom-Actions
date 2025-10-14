using Newtonsoft.Json.Linq;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace lists.remove
{
    [ClassDecorator(Name = "List Remove", Shape = ActionShape.Circle, Description = "Remove a given object from a list ",
        IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    class ListRemove : IAction
    {
        #region Properties
        [FEDecorator(Label = "List from which the object is removed", Type = FeComponentType.Text, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.InputOutput)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> InputList { get; set; }

        [FEDecorator(Label = "Input ObjectToBeRemoved", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public object InputObjectToBeRemoved { get; set; }
        #endregion

        public ListRemove(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            // Check if Inputs are null
            if (InputList == null || string.IsNullOrEmpty(InputObjectToBeRemoved.ToString()))
            {
                throw new System.Exception("Input was null. Please, fill it");
            }

            // Check if InputObjectToBeRemoved can be converted to JObj
            // cannot be converted to JObj, it's not datamodel - can be int, string, bool 
            var InputListAux = InputList.ToList();
            if (InputObjectToBeRemoved.IsJObject() == false)
            {
                // Check if input it's bool (bool ToString = > True, not true)
                bool successBool = bool.TryParse(InputObjectToBeRemoved.ToString(), out bool boolToBeFound);

                foreach (var item in InputList)
                {
                    if (InputObjectToBeRemoved.Equals(item) || InputObjectToBeRemoved.Equals(item.ToString())
                        || (successBool && boolToBeFound.Equals(item)))
                    {
                        InputListAux.Remove(item);
                        InputList = InputListAux;
                        return;
                    }
                }
            }
            else  // it's datamodel
            {
                JObject jObjToBeRemoved = InputObjectToBeRemoved.ConvertToJObject(); // Convert InputObjectToBeRemoved to JObject
                foreach (var item in InputListAux)
                {
                    if (item.IsJObject()) // Check if each element from Input List cand be converted to JObject
                    {
                        JObject jObjFromList = item.ConvertToJObject(); // Convert each element of Input List to JObject

                        if (jObjToBeRemoved.EqualsJObj(jObjFromList)) // Check if the JObj are equals
                        {
                            InputListAux.Remove(item); // Remove the item from the list
                            InputList = InputListAux;
                            return;
                        }
                    }
                }
            }

        }
        #endregion
    }
}
