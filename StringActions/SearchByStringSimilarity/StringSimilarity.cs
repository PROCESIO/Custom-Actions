using com.ncd.ActionLib.Actions.Strings.Models;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using SimMetrics.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Actions.Strings
{
    [ClassDecorator(Name = "Search by String Similarity", Shape = ActionShape.Square, Description = "Search a string in a list of strings by estimating the similarity", Classification = Classification.cat1, IsTestable = true, Tooltip = "")]
    [FEDecorator(Label = "Configuration", Type = FeComponentType.Side_pannel, Tab = "Details", RowId = 1, Parent = "Configuration")]
    [Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
    public class StringSimilarity : IAction
    {
        private IEnumerable<OptionModel> SimMetricTypeOptionsList { get; } = new List<OptionModel>() {
            new OptionModel(){ name = "BlockDistance", value = (int)SimMetricType.BlockDistance + 1 },
            new OptionModel(){ name = "ChapmanLengthDeviation", value = (int)SimMetricType.ChapmanLengthDeviation + 1 },
            new OptionModel(){ name = "ChapmanMeanLength", value = (int)SimMetricType.ChapmanMeanLength + 1 },
            new OptionModel(){ name = "CosineSimilarity", value = (int)SimMetricType.CosineSimilarity + 1 },
            new OptionModel(){ name = "EuclideanDistance", value = (int)SimMetricType.EuclideanDistance + 1 },
            new OptionModel(){ name = "JaccardSimilarity", value = (int)SimMetricType.JaccardSimilarity + 1 },
            new OptionModel(){ name = "DiceSimilarity", value = (int)SimMetricType.DiceSimilarity + 1 },
            new OptionModel(){ name = "Jaro", value = (int)SimMetricType.Jaro + 1 },
            new OptionModel(){ name = "JaroWinkler", value = (int)SimMetricType.JaroWinkler + 1 },
            new OptionModel(){ name = "MatchingCoefficient", value = (int)SimMetricType.MatchingCoefficient + 1 },
            new OptionModel(){ name = "MongeElkan", value = (int)SimMetricType.MongeElkan + 1 },
            new OptionModel(){ name = "Levenstein", value = (int)SimMetricType.Levenstein + 1 },
            new OptionModel(){ name = "NeedlemanWunch", value = (int)SimMetricType.NeedlemanWunch + 1 },
            new OptionModel(){ name = "OverlapCoefficient", value = (int)SimMetricType.OverlapCoefficient + 1 },
            new OptionModel(){ name = "QGramsDistance", value = (int)SimMetricType.QGramsDistance + 1 },
            new OptionModel(){ name = "SmithWaterman", value = (int)SimMetricType.SmithWaterman + 1 },
            new OptionModel(){ name = "SmithWatermanGotoh", value = (int)SimMetricType.SmithWatermanGotoh + 1 },
            new OptionModel(){ name = "SmithWatermanGotohWindowedAffine", value = (int)SimMetricType.SmithWatermanGotohWindowedAffine + 1 }
        };

        [FEDecorator(Label = "List of strings to search into", Type = FeComponentType.Text, RowId = 1, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true)]
        public IEnumerable<string> ListOfStringsToCompare { get; set; }

        [FEDecorator(Label = "Similarity algorithm", Type = FeComponentType.Select, RowId = 2, Parent = "Configuration", Options = "SimMetricTypeOptionsList", DefaultValue = (int)SimMetricType.SmithWaterman + 1)]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = false)]
        public int SimMetricTypeOption { get; set; }

        [FEDecorator(Label = "Similarity threshold [0-1]", Type = FeComponentType.Number, RowId = 3, Parent = "Configuration", DefaultValue = 0.85,
                    Tooltip = "The strings with a similarity below the threshold will not be returned in the output list")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Decimal_Number)]
        public double SimilarityThreshold { get; set; }

        [FEDecorator(Label = "String to search for", Type = FeComponentType.Text, RowId = 4, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.String)]
        public string StringToCompare { get; set; }

        [FEDecorator(Label = "Limit number of search results", Type = FeComponentType.Number, RowId = 5, Parent = "Configuration", DefaultValue = 100,
                    Tooltip = "If your input is 0, it will return all results")]
        [BEDecorator(IOProperty = Direction.Input)]
        [Validator(IsRequired = true, Expects = ExpectedType.Number)]
        public int NumberOfResults { get; set; }

        [FEDecorator(Label = "Output list of search results", Type = FeComponentType.Text, RowId = 6, Parent = "Configuration")]
        [BEDecorator(IOProperty = Direction.Output)]
        [Validator(IsRequired = true)]
        public IEnumerable<string> ListOfSimilarStrings { get; set; }

        public StringSimilarity(IServiceProvider service)
        {

        }


        public async Task Execute()
        {
            SimMetricType selectedSimilarityType = SimMetricType.SmithWaterman;
            if (Enum.IsDefined(typeof(SimMetricType), SimMetricTypeOption))
            {
                selectedSimilarityType = (SimMetricType)SimMetricTypeOption - 1;
            }

            List<Similarity> listOfResults = new List<Similarity>();

            if (ListOfStringsToCompare != null)
            {
                foreach (string str in ListOfStringsToCompare)
                {
                    double similarityScore = str.ApproximatelyEquals(StringToCompare, selectedSimilarityType);
                    if (similarityScore >= SimilarityThreshold)
                    {
                        listOfResults.Add(new Similarity { SimilarityScore = similarityScore, ComparedString = str });
                    }
                }
            }

            List<Similarity> sortedListOfResults = new List<Similarity>();

            if (NumberOfResults > 0)
            {
                sortedListOfResults = listOfResults.OrderByDescending(o => o.SimilarityScore).Take(NumberOfResults).ToList();
            }
            else
            {
                sortedListOfResults = listOfResults.OrderByDescending(o => o.SimilarityScore).ToList();
            }


            ListOfSimilarStrings = sortedListOfResults.Select(x => x.ComparedString).ToList();
        }
    }
}
