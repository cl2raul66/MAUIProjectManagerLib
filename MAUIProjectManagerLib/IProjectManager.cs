
namespace MAUIProjectManagerLib
{
    public interface IProjectManager
    {
        string? ProjectPath { get; }

        event EventHandler<string>? CommandCompleted;
        event EventHandler<string>? CommandErrorReceived;
        event EventHandler<string>? CommandOutputReceived;
        event EventHandler<string>? CommandStarted;

        Task BuildAsync();
        Task CreateAsync();
        Task Deleted();
        Task<Dictionary<string, string>> GetTargetPlatformsAsync();
        Task RestoreAsync();
        Task RunAsync(string targetFramework);
        Task SetProjectDirectory(string projectDirectory);
    }
}