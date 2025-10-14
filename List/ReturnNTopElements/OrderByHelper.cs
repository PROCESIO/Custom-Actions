using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace list.returnntopelements
{

    public static class OrderByHelper
    {
        public static IEnumerable<object> GetSortedDesc(IEnumerable<object> inputList)
        {

            var intList = new List<int>();
            intList = GetConvertedIntegerList(inputList);
            if (intList != null)
            {
                return intList.OrderByDescending(s => s).Cast<object>();
            }

            var doubleList = new List<double>();
            doubleList = GetConvertedDoubleList(inputList);
            if (doubleList != null)
            {
                return doubleList.OrderByDescending(s => s).Cast<object>();
            }

            var stringList = new List<string>();
            stringList = GetConvertedStringList(inputList);
            if (stringList != null)
            {
                return stringList.OrderByDescending(s => s);
            }
            return null;
        }

        public static IEnumerable<object> GetSortedAsc(IEnumerable<object> inputList)
        {

            var intList = new List<int>();
            intList = GetConvertedIntegerList(inputList);
            if (intList != null)
            {
                return intList.OrderBy(s => s).Cast<object>();

            }

            var doubleList = new List<double>();
            doubleList = GetConvertedDoubleList(inputList);
            if (doubleList != null)
            {
                return doubleList.OrderBy(s => s).Cast<object>();
            }

            var stringList = new List<string>();
            stringList = GetConvertedStringList(inputList);
            if (stringList != null)
            {
                return stringList.OrderBy(s => s);
            }
            return null;
        }

        private static List<string> GetConvertedStringList(IEnumerable<object> inputList)
        {
            try
            {
                return inputList.ToList().ConvertAll(el => el.ToString());
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static List<double> GetConvertedDoubleList(IEnumerable<object> inputList)
        {
            try
            {
                return inputList.ToList().ConvertAll(el => double.Parse(el.ToString()));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static List<int> GetConvertedIntegerList(IEnumerable<object> inputList)
        {
            try
            {
                return inputList.ToList().ConvertAll(el => int.Parse(el.ToString()));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}