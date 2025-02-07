# MAUI Project Manager Library

A .NET library designed to manage .NET MAUI projects programmatically. This library provides a simple and efficient way to create, build, run, and manage .NET MAUI applications across different platforms.

## Features

- Create new MAUI projects
- Build existing MAUI projects
- Restore NuGet packages
- Run applications on different platforms (Windows, Android, iOS, MacCatalyst)
- Get available target platforms for the project
- Delete projects and clean up resources
- Cross-platform support (Windows and macOS)
- Event-based command execution tracking

## Installation

You can install the package via NuGet:

```bash
dotnet add package MAUIProjectManagerLib
```

## Usage

Here's a basic example of how to use the MAUI Project Manager:

```csharp
var projectManager = new ProjectManager();

// Set up event handlers
projectManager.CommandStarted += (s, cmd) => Console.WriteLine($"Command started: {cmd}");
projectManager.CommandCompleted += (s, cmd) => Console.WriteLine($"Command completed: {cmd}");
projectManager.CommandErrorReceived += (s, error) => Console.WriteLine($"Error: {error}");
projectManager.CommandOutputReceived += (s, output) => Console.WriteLine(output);

// Set project directory
await projectManager.SetProjectDirectory("C:\\MyMauiProject");

// Create a new MAUI project
await projectManager.CreateAsync();

// Build the project
await projectManager.BuildAsync();

// Get available target platforms
var platforms = await projectManager.GetTargetPlatformsAsync();
foreach (var platform in platforms)
{
    Console.WriteLine($"Platform: {platform.Key}, Framework: {platform.Value}");
}

// Run on specific platform
await projectManager.RunAsync("net8.0-windows10.0.19041.0");
```

## Platform Support

The library supports managing MAUI projects for the following platforms:
- Windows
- Android
- iOS
- MacCatalyst
- Tizen (when enabled)

## Requirements

- .NET 8.0 or higher
- .NET MAUI workload installed
- Required platform SDKs for the target platforms you want to build for

## Error Handling

The library provides comprehensive error handling through events:
- `CommandErrorReceived`: Triggered when an error occurs during command execution
- `CommandOutputReceived`: Provides command output information
- `CommandStarted`: Indicates when a command starts executing
- `CommandCompleted`: Signals command completion

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

```
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/
```

## Acknowledgments

- Built with .NET MAUI
- Supports cross-platform development
- Designed for programmatic project management