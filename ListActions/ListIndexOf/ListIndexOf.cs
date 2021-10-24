using com.ncd.ActionLib.Actions.Utils;
using Newtonsoft.Json.Linq;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace com.ncd.ActionLib.Actions.Lists
{
    [ClassDecorator(Name = "List IndexOf", Shape = ActionShape.Circle, Description = "Searching in a list " +
        "for a given object. If found, return index, otherwise -1", IsTestable = true, Classification = Classification.cat1)]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class ListIndexOf : IAction
    {
        #region Properties
        [FEDecorator(Label = "Input list", Type = FeComponentType.Any, RowId = 1, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public IEnumerable<object> InputList { get; set; }

        [FEDecorator(Label = "Input object to be found", Type = FeComponentType.Text, RowId = 2, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public object InputToBeFound { get; set; }

        [FEDecorator(Label = "Result", Type = FeComponentType.Text, RowId = 3, Tab = "Configuration Tab")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int Output { get; set; } = -1;
        #endregion

        public ListIndexOf(IServiceProvider service) { }

        #region Execute
        public async Task Execute()
        {
            // Check if Inputs are null
            if (InputList == null || string.IsNullOrEmpty(InputToBeFound.ToString()))
            {
                throw new Exception("Input was null. Please, fill it.");
            }

            // Check if InputToBeFound can be converted to JObj
            // cannot be converted to JObj, it's not datamodel - can be int, string, bool 
            int index = 0;
            if (InputToBeFound.IsJObject() == false)
            {
                // Check if input it's bool (bool ToString = > True, not true)
                bool successBool = bool.TryParse(InputToBeFound.ToString(), out bool boolToBeFound);

                foreach (var item in InputList)
                {
                    if (InputToBeFound.Equals(item) || InputToBeFound.Equals(item.ToString())
                        || (successBool && boolToBeFound.Equals(item)))
                    {
                        Output = index;
                        return;
                    }

                    index++;
                }
            }
            else  // it's datamodel
            {

                JObject jObjToBeFound = InputToBeFound.ConvertToJObject(); // Convert InputToBeFound to JObject
                foreach (var item in InputList)
                {
                    if (item.IsJObject()) // Check if each element from Input List cand be converted to JObject
                    {
                        JObject jObjFromList = item.ConvertToJObject(); // Convert each element of Input List to JObject
                        if (jObjToBeFound.EqualsJObj(jObjFromList)) // Check if the JObj are equals
                        {
                            Output = index;
                            return;
                        }
                    }

                    index++;
                }
            }

        }
        #endregion
    }
}