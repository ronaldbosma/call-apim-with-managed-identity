﻿using dotenv.net;
using System.Diagnostics;
using System.Text.Json;

namespace IntegrationTests.Configuration;

/// <summary>
/// Helper class to locate the .env file for the default azd environment.
/// </summary>
internal static class AzdDotEnv
{
    /// <summary>
    /// Locates the .env file for the default azd environment and loads the environment variables.
    /// </summary>
    /// <param name="optional">Indication if loading the azd .env file is optional.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown when the .azure directory is not found and <paramref name="optional"/> is false.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the .env file is not found and <paramref name="optional"/> is false.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the default environment cannot be determined and <paramref name="optional"/> is false.</exception>
    public static void Load(bool optional)
    {
        var azdEnvFilePath = LocateEnvFileOfDefaultAzdEnvironment(optional);
        DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: optional, envFilePaths: [azdEnvFilePath]));
    }

    private static string LocateEnvFileOfDefaultAzdEnvironment(bool optional)
    {
        try
        {
            var azureDirectory = GetAzureDirectory(AppContext.BaseDirectory);
            var defaultEnvironmentName = GetDefaultEnvironmentName(azureDirectory);
            return GetEnvFileForEnvironment(azureDirectory, defaultEnvironmentName);
        }
        catch (Exception ex)
        {
            if (optional)
            {
                Trace.WriteLine($"Unable to locate .env file for default azd environment. Continuing without. Error: {ex}");
                return string.Empty;
            }
            throw;
        }
    }

    private static string GetAzureDirectory(string startingDirectory)
    {
        var currentDirectory = new DirectoryInfo(startingDirectory);

        while (currentDirectory != null)
        {
            var azureDirectory = Path.Combine(currentDirectory.FullName, ".azure");
            if (Directory.Exists(azureDirectory))
            {
                return azureDirectory;
            }
            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find .azure directory in parent directories of {startingDirectory}");
    }

    private static string GetDefaultEnvironmentName(string azureDirectory)
    {
        var configFile = Path.Combine(azureDirectory, "config.json");
        if (!File.Exists(configFile))
        {
            throw new FileNotFoundException($"Unable to determine default environment. Could not find config.json file in directory: {azureDirectory}");
        }

        var configJson = File.ReadAllText(configFile);
        using var document = JsonDocument.Parse(configJson);
        if (!document.RootElement.TryGetProperty("defaultEnvironment", out var defaultEnvironmentElement))
        {
            throw new InvalidOperationException($"Property 'defaultEnvironment' not found in: {configFile}");
        }

        var defaultEnvironment = defaultEnvironmentElement.GetString();
        if (string.IsNullOrWhiteSpace(defaultEnvironment))
        {
            throw new InvalidOperationException($"Value of 'defaultEnvironment' is null or empty in: {configFile}");
        }

        return defaultEnvironment;
    }

    private static string GetEnvFileForEnvironment(string azureDirectory, string environmentName)
    {
        var envFile = Path.Combine(azureDirectory, environmentName, ".env");
        if (!File.Exists(envFile))
        {
            throw new FileNotFoundException($"Could not find .env file for environment '{environmentName}' in directory: {azureDirectory}");
        }

        return envFile;
    }
}
