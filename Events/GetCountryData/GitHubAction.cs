using Newtonsoft.Json.Linq;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;
using Ringhel.Procesio.Action.Core.Utils;

namespace CountryAction;

[ClassDecorator(Name = "GitHub Connector", Shape = ActionShape.Square,
    Description = "Connect and interact with GitHub.",
    Classification = Classification.cat1, IsTestable = true)]
[Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
public class GitHubAction : IAction
{
    #region Properties

    [FEDecorator(Label = "GitHub Configuration", Type = FeComponentType.Credentials_Rest,
        RowId = 1, Tab = "Github",
        CustomCredentialsTypeGuid = "62b726d4-89f4-490e-8287-064542c7295f")] //GitHub Predefined CredentialTemplate Gid
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = true)]
    public APICredentialsManager? Credentials { get; set; }

    [FEDecorator(Label = "Select Repository", Type = FeComponentType.Select, RowId = 2, Tab = "Github",
        Options = nameof(RepositoryList), Tooltip = "Choose your organization repository.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [Validator(IsRequired = true)]
    [DependencyDecorator(Tab = "Github", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    public string? Repository { get; set; }
    private IList<OptionModel> RepositoryList { get; } = new List<OptionModel>() { };

    [FEDecorator(Label = "Select Commit", Type = FeComponentType.Select, RowId = 3, Tab = "Github",
        Options = nameof(CommitList), Tooltip = "Choose your commit.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [Validator(IsRequired = true)]
    [DependencyDecorator(Tab = "Github", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Github", Control = nameof(Repository), Operator = Operator.NotEquals, Value = null)]
    public string? Commit { get; set; }
    private IList<OptionModel> CommitList { get; } = new List<OptionModel>() { };

    [FEDecorator(Label = "Commit Details", Type = FeComponentType.DataType, RowId = 4, Tab = "Github")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public object? CommitDetail { get; set; }

    #endregion

    public GitHubAction() { }

    public async Task Execute()
    {
        ValidateCredentials();
        ValidateRepository();
        ValidateCommit();

        var endpoint = $"repos/{Repository}/commits/{Commit}";
        var apiResponse = await Credentials!.Client.GetAsync(endpoint, new(), new());

        ValidateApiResult(apiResponse);

        try
        {
            CommitDetail = await apiResponse.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            throw new Exception($"Unable to interpret Http result: {apiResponse.Content}");
        }
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Credentials), OutputTarget = OutputTarget.Options,
        InputControls = [nameof(Credentials)], OutputControls = [nameof(Repository)])]
    public async Task OnCredentialChange()
    {
        ValidateCredentials();

        var endpoint = "user/repos";
        var apiResponse = await Credentials!.Client.GetAsync(endpoint, new(), new());

        ValidateApiResult(apiResponse);

        try
        {
            var response = await apiResponse.Content.ReadAsStringAsync();
            var repos = JArray.Parse(response);

            foreach (var r in repos)
            {
                var name = r.Value<string>("name");
                var fullName = r.Value<string>("full_name");
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(fullName))
                {
                    RepositoryList.Add(new OptionModel { name = name, value = fullName });
                }
            }
        }
        catch (Exception)
        {
            throw new Exception($"Unable to interpret Http result: {apiResponse.Content}");
        }
    }

    [ControlEventHandler(EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Repository), OutputTarget = OutputTarget.Options,
        InputControls = [nameof(Credentials), nameof(Repository)], OutputControls = [nameof(Commit)])]
    public async Task OnRepositoryChange()
    {
        ValidateCredentials();
        ValidateRepository();

        var endpoint = $"repos/{Repository}/commits";
        var apiResponse = await Credentials!.Client.GetAsync(endpoint, new(), new());

        ValidateApiResult(apiResponse);

        try
        {
            var response = await apiResponse.Content.ReadAsStringAsync();
            var commits = JArray.Parse(response);

            foreach (var c in commits)
            {
                var sha = c.Value<string>("sha");
                var message = c["commit"]?["message"]?.ToString();
                if (!string.IsNullOrWhiteSpace(sha) && !string.IsNullOrWhiteSpace(message))
                {
                    CommitList.Add(new OptionModel { name = message!, value = sha });
                }
            }
        }
        catch (Exception)
        {
            throw new Exception($"Unable to interpret Http result: {apiResponse.Content}");
        }
    }

    #region Validations

    private void ValidateCredentials()
    {
        if (Credentials?.Client is null || Credentials.CredentialConfig is null)
        {
            throw new Exception("Invalid Credentials.");
        }
    }

    private void ValidateRepository()
    {
        if (string.IsNullOrWhiteSpace(Repository))
        {
            throw new Exception("Invalid Repository.");
        }
    }

    private void ValidateCommit()
    {
        if (string.IsNullOrWhiteSpace(Commit))
        {
            throw new Exception("Invalid Commit.");
        }
    }

    private static void ValidateApiResult(HttpResponseMessage? apiResponse)
    {
        if (apiResponse is null)
        {
            throw new Exception("Http result is null.");
        }

        if (!apiResponse.IsSuccessStatusCode())
        {
            throw new Exception($"Http result is not successful. Status code: {apiResponse.StatusCode}");
        }
    }

    #endregion
}
