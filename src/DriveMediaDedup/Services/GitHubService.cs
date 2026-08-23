using Octokit;

namespace DriveMediaDedup.Services;

public class GitHubService
{
    private readonly GitHubClient _client;
    private readonly string _owner;
    private readonly string _repoName;
    private readonly string _description;

    public GitHubService(string token, string owner, string repoName, string description)
    {
        _client = new GitHubClient(new ProductHeaderValue("DriveMediaDedup"))
        {
            Credentials = new Credentials(token)
        };
        _owner = owner;
        _repoName = repoName;
        _description = description;
    }

    // Cria o repositório no GitHub
    public async Task<string> CreateRepositoryAsync()
    {
        var newRepo = new NewRepository(_repoName)
        {
            Description = _description,
            Private = false,
            AutoInit = true
        };

        var repo = await _client.Repository.Create(newRepo);
        return repo.CloneUrl;
    }

    // Verifica se o repositório já existe
    public async Task<bool> RepositoryExistsAsync()
    {
        try
        {
            var repo = await _client.Repository.Get(_owner, _repoName);
            return repo != null;
        }
        catch
        {
            return false;
        }
    }
}