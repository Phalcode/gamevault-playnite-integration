using Microsoft.Win32;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace GameVaultLibrary
{
    public class GameVaultLibraryClient : LibraryClient
    {
        private const string GAMEVAULT_PIPE_NAME = "GameVault";
        private const string GAMEVAULT_PROCESS_NAME = "gamevault";

        public static readonly string IconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.png");

        private static readonly ILogger logger = LogManager.GetLogger(nameof(GameVaultLibraryClient));

        private bool? isInstalled;

        public override bool IsInstalled
        {
            get
            {
                if (isInstalled.HasValue)
                    return isInstalled.Value;

                isInstalled = CheckInstallation();
                return isInstalled.Value;
            }
        }
        private bool CheckInstallation()
        {
            // Check GitHub version
            string executablePath = GetExecutablePath();
            if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath))
                return true;

            // Check Microsoft Store version
            return IsMicrosoftStoreAppInstalled("Phalcode.174950BD81C41");
        }
        private bool IsMicrosoftStoreAppInstalled(string packageName)
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"Get-AppxPackage -Name {packageName}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return !string.IsNullOrWhiteSpace(output);
                }
            }
            catch
            {
                return false;
            }
        }

        public override string Icon { get; } = IconPath;

        public bool IsRunning => Process.GetProcessesByName(GAMEVAULT_PROCESS_NAME).Any();

        public string AppVersion { get; private set; }

        public override void Open()
        {
            OpenClient(minimized: false);
        }

        public override void Shutdown()
        {
            foreach (var process in Process.GetProcessesByName(GAMEVAULT_PROCESS_NAME))
                process.Kill();
        }

        public async Task<bool> EnsureRunning(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (!IsInstalled)
                return false;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                if (!IsRunning)
                {
                    OpenClient(minimized: true);

                    // Wait arbitrary time for the app to start
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                }

                AppVersion = "";

                // Ensure the named pipe is accessible
                do
                {
                    if (!IsRunning || cancellationToken.IsCancellationRequested || stopwatch.Elapsed > timeout)
                        return false;

                    try
                    {
                        AppVersion = await SendPipeMessage("gamevault://query?query=getappversion", cancellationToken, expectsResult: true).ConfigureAwait(false);
                    }
                    catch (Exception) { }

                    if (string.IsNullOrEmpty(AppVersion))
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
                while (string.IsNullOrEmpty(AppVersion));

                return !string.IsNullOrEmpty(AppVersion);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Sends a single message to the main running instance
        /// </summary>
        /// <param name="message">The message to send (which should be an uri form)</param>
        /// <param name="expectsResult">True if we expect a response (such as from a Query)</param>
        /// <returns></returns>
        public async Task<string> SendPipeMessage(string message, CancellationToken cancellationToken, bool expectsResult = false, int timeout = 500)
        {
            string result = null;
            var client = new NamedPipeClientStream(GAMEVAULT_PIPE_NAME);
            StreamWriter writer = null;
            StreamReader reader = null;

            try
            {
                await client.ConnectAsync(timeout, cancellationToken);

                writer = new StreamWriter(client, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };
                await writer.WriteLineAsync(message);

                if (expectsResult)
                {
                    reader = new StreamReader(client, Encoding.UTF8, false, 1024, leaveOpen: true);
                    result = await reader.ReadLineAsync();
                }
            }
            finally
            {
                SafeDispose(writer);
                SafeDispose(reader);
                SafeDispose(client);
            }

            return result;
        }

        /// <summary>
        /// Safely dispose anything without throwing an error if it's already disposed or closed or whatever
        /// </summary>
        /// <param name="disposable">The object to dispose</param>
        private static void SafeDispose(IDisposable disposable)
        {
            if (disposable == null)
                return;

            try
            {
                disposable.Dispose();
            }
            catch (Exception) { }
        }

        public static string GetExecutablePath() => GetExecutablePathFromUriSchemaCommand();

        private static readonly Regex GetExecutableFromOpenCommand = new Regex(@"^""?(.+gamevault\.exe)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static string GetExecutablePathFromUriSchemaCommand()
        {
            const string registryKeyPath = @"Classes\gamevault\shell\open\command";

            string openCommand;

            openCommand = Registry.GetValue($@"HKEY_CURRENT_USER\SOFTWARE\{registryKeyPath}", "", null) as string;

            if (string.IsNullOrEmpty(openCommand))
                openCommand = Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\{registryKeyPath}", "", null) as string;

            if (string.IsNullOrEmpty(openCommand))
                openCommand = Registry.GetValue($@"HKEY_CURRENT_USER\SOFTWARE\Wow6432Node\{registryKeyPath}", "", null) as string;

            if (string.IsNullOrEmpty(openCommand))
                openCommand = Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\{registryKeyPath}", "", null) as string;

            if (string.IsNullOrEmpty(openCommand))
                return null;

            var match = GetExecutableFromOpenCommand.Match(openCommand);

            if (!match.Success)
                return null;

            return match.Groups[1].Value;
        }

        public void OpenClient(bool minimized) => Playnite.Common.ProcessStarter.StartUrl($"gamevault://show?minimized={minimized}");
        public void StartGame(string gameId) => Playnite.Common.ProcessStarter.StartUrl($"gamevault://start?gameid={gameId}");
        public void InstallGame(string gameId) => Playnite.Common.ProcessStarter.StartUrl($"gamevault://install?gameid={gameId}");
        public void UninstallGame(string gameId) => Playnite.Common.ProcessStarter.StartUrl($"gamevault://uninstall?gameid={gameId}");

        public class QueryResult<T>
        {
            public readonly bool Success;
            public readonly T Result;

            public static QueryResult<T> Failed = new QueryResult<T>(false, default(T));

            public QueryResult(bool success, T result)
            {
                this.Success = success;
                this.Result = result;
            }
        }

        public async Task<QueryResult<string>> TryQueryInstallDirectory(string gameId, CancellationToken cancellationToken = default, bool ensureRunning = true)
        {
            try
            {
                if (!ensureRunning || !await EnsureRunning(TimeSpan.FromSeconds(10), cancellationToken))
                {
                    var result = await SendPipeMessage($"gamevault://query?query=getinstalldirectory&gameid={gameId}", cancellationToken, expectsResult: true);

                    if (!string.IsNullOrEmpty(result))
                        return new QueryResult<string>(true, result);
                }
            }
            catch (Exception ex) { logger.Error(ex, "Failed to query install directory"); }

            return QueryResult<string>.Failed;
        }

        public async Task<QueryResult<bool>> TryQueryIsInstalled(string gameId, CancellationToken cancellationToken = default, bool ensureRunning = true)
        {
            try
            {
                if (!ensureRunning || !await EnsureRunning(TimeSpan.FromSeconds(10), cancellationToken))
                {
                    var result = await SendPipeMessage($"gamevault://query?query=installed&gameid={gameId}", cancellationToken, expectsResult: true);

                    if (bool.TryParse(result, out var resultBool))
                        return new QueryResult<bool>(true, resultBool);
                }
            }
            catch (Exception ex) { logger.Error(ex, "Failed to query install status"); }

            return QueryResult<bool>.Failed;
        }
    }
}