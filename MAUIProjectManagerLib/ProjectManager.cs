using System.Diagnostics;
using System.Xml.Linq;

namespace MAUIProjectManagerLib;

public class ProjectManager : IProjectManager
{
    private string? _projectDirectory;
    private bool _isMauiProject;

    public string? ProjectPath { get; private set; }

    public event EventHandler<string>? CommandOutputReceived;
    public event EventHandler<string>? CommandErrorReceived;
    public event EventHandler<string>? CommandStarted;
    public event EventHandler<string>? CommandCompleted;

    public async Task SetProjectDirectory(string projectDirectory)
    {
        if (File.Exists(projectDirectory))
        {
            _projectDirectory = Path.GetDirectoryName(projectDirectory);
        }
        else if (Directory.Exists(projectDirectory))
        {
            _projectDirectory = projectDirectory;
        }
        else
        {
            Directory.CreateDirectory(projectDirectory);
            _projectDirectory = projectDirectory;
        }

        ProjectPath = Directory.GetFiles(_projectDirectory!, "*.csproj").FirstOrDefault();

        await IsApplicationMauiAsync();
    }

    public async Task CreateAsync()
    {
        if (!EnsureProjectDirectoryIsSet()) return;
        await ExecuteCommandAsync("dotnet new maui");
        ProjectPath = Directory.GetFiles(_projectDirectory!, "*.csproj").FirstOrDefault();
        await IsApplicationMauiAsync();
    }

    public async Task BuildAsync()
    {
        if (!EnsureProjectDirectoryIsSet() || !_isMauiProject) return;
        await ExecuteCommandAsync("dotnet build");
    }

    public async Task RestoreAsync()
    {
        if (!EnsureProjectDirectoryIsSet() || !_isMauiProject) return;
        await ExecuteCommandAsync("dotnet restore");
    }

    public async Task RunAsync(string targetFramework)
    {
        if (!EnsureProjectDirectoryIsSet() || !_isMauiProject) return;

        string? com = targetFramework switch
        {
            string tf when tf.Contains("android") => $"dotnet build -t:Run -f {targetFramework} -p:AndroidTarget=emulator-5554",
            string tf when tf.Contains("ios") => "iOS",
            string tf when tf.Contains("maccatalyst") => "MacCatalyst",
            string tf when tf.Contains("windows") => $"dotnet run --framework {targetFramework}",
            _ => null
        };

        if (string.IsNullOrEmpty(com))
        {
            return;
        }

        await ExecuteCommandAsync(com);
    }

    public async Task Deleted()
    {
        if (!EnsureProjectDirectoryIsSet() || !_isMauiProject) return;
        await ExecuteCommandAsync($"dotnet clean");
        Directory.Delete(_projectDirectory!, true);
        await Task.Run(() =>
        {
            if (Directory.Exists(_projectDirectory))
            {
                Directory.Delete(_projectDirectory, true);
            }
            if (!Directory.Exists(_projectDirectory))
            {
                ProjectPath = null;
            }
        });
    }

    public async Task<Dictionary<string, string>> GetTargetPlatformsAsync()
    {
        if (!EnsureProjectDirectoryIsSet() || !_isMauiProject)
        {
            return [];
        }

        string csprojPath = Directory.GetFiles(_projectDirectory!, "*.csproj").First();
        if (string.IsNullOrEmpty(csprojPath))
        {
            return [];
        }

        Dictionary<string, string> result = [];
        var doc = await XDocument.LoadAsync(File.OpenRead(csprojPath), LoadOptions.None, CancellationToken.None);
        var targetFrameworks = doc.Descendants("TargetFrameworks").FirstOrDefault()?.Value;

        if (targetFrameworks is not null)
        {
            var frameworks = targetFrameworks.Split(';');
            foreach (var framework in frameworks)
            {
                AddFrameworkToResult(result, framework);
            }
        }

        // Check for additional TargetFrameworks with conditions
        var conditionalFrameworks = doc.Descendants("TargetFrameworks")
            .Where(e => e.Attribute("Condition") != null)
            .Select(e => e.Value.Split(';'))
            .SelectMany(f => f)
            .Distinct();

        foreach (var framework in conditionalFrameworks)
        {
            AddFrameworkToResult(result, framework);
        }

        return result;
    }

    #region Extra
    bool EnsureProjectDirectoryIsSet()
    {
        if (string.IsNullOrEmpty(_projectDirectory))
        {
            OnCommandErrorReceived("El directorio del proyecto no está establecido. Llama a SetProjectDirectory antes de ejecutar comandos.");
            return false;
        }

        if (!Directory.Exists(_projectDirectory))
        {
            OnCommandErrorReceived($"El directorio especificado no existe: {_projectDirectory}");
            return false;
        }

        return true;
    }

    async Task IsApplicationMauiAsync()
    {
        if (!EnsureProjectDirectoryIsSet())
        {
            _isMauiProject = false;
            return;
        }

        if (string.IsNullOrEmpty(ProjectPath))
        {
            _isMauiProject = false;
            return;
        }

        var doc = await XDocument.LoadAsync(File.OpenRead(ProjectPath!), LoadOptions.None, CancellationToken.None);
        var useMaui = doc.Descendants("UseMaui").FirstOrDefault()?.Value;
        var outputType = doc.Descendants("OutputType").FirstOrDefault()?.Value;

        _isMauiProject = useMaui == "true" && outputType == "Exe";
    }

    async Task ExecuteCommandAsync(string command)
    {
        try
        {
            var processInfo = CreateProcessStartInfo(command);
            OnCommandStarted(command);

            using var process = Process.Start(processInfo);
            if (process is null)
            {
                OnCommandErrorReceived("No se pudo iniciar el proceso.");
                return;
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                OnCommandErrorReceived($"El comando falló con el código de salida {process.ExitCode}: {error}");
                return;
            }

            OnCommandOutputReceived(output);
            OnCommandCompleted(command);
        }
        catch (InvalidOperationException ex)
        {
            OnCommandErrorReceived($"Error de operación: {ex.Message}");
        }
        catch (System.ComponentModel.Win32Exception ex) when (!OperatingSystem.IsMacOS())
        {
            OnCommandErrorReceived($"Error de Windows: {ex.Message}");
        }
        catch (Exception ex)
        {
            OnCommandErrorReceived($"Error al ejecutar el comando: {ex.Message}");
        }
    }

    ProcessStartInfo CreateProcessStartInfo(string command)
    {
        if (OperatingSystem.IsMacOS())
        {
            return new ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _projectDirectory
            };
        }
        else
        {
            return new ProcessStartInfo("powershell", $"-Command {command}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _projectDirectory
            };
        }
    }

    void AddFrameworkToResult(Dictionary<string, string> result, string framework)
    {
        if (framework.Contains("net"))
        {
            if (framework.Contains("android"))
            {
                result["Android"] = framework;
            }
            else if (framework.Contains("ios"))
            {
                result["iOS"] = framework;
            }
            else if (framework.Contains("maccatalyst"))
            {
                result["MacCatalyst"] = framework;
            }
            else if (framework.Contains("windows"))
            {
                result["Windows"] = framework;
            }
            else if (framework.Contains("tizen"))
            {
                result["Tizen"] = framework;
            }
        }
    }

    protected virtual void OnCommandOutputReceived(string output)
    {
        CommandOutputReceived?.Invoke(this, output);
    }

    protected virtual void OnCommandErrorReceived(string error)
    {
        CommandErrorReceived?.Invoke(this, error);
    }

    protected virtual void OnCommandStarted(string command)
    {
        CommandStarted?.Invoke(this, command);
    }

    protected virtual void OnCommandCompleted(string command)
    {
        CommandCompleted?.Invoke(this, command);
    }

    #endregion
}
