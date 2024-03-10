using GameVaultLibrary.Contollers;
using GameVaultLibrary.Models;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GameVaultLibrary
{
    public class GameVaultLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GameVaultLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("fab8be77-18ab-4e6c-ad3d-89097b492d74");

        // Change to something more appropriate
        public override string Name { get; } = "GameVault library integration";

        public override string LibraryIcon { get; } = GameVaultLibraryClient.IconPath;

        // Implementing Client adds ability to open it via special menu in playnite.
        public override LibraryClient Client => GameVaultClient;

        public GameVaultLibraryClient GameVaultClient { get; } = new GameVaultLibraryClient();

        public GameVaultLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new GameVaultLibrarySettingsViewModel(this);

            Properties = new LibraryPluginProperties
            {
                HasSettings = true,
                CanShutdownClient = true
            };

            try
            {
                var extensionYaml = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "extension.yaml");

                if (System.IO.File.Exists(extensionYaml))
                {
                    var content = System.IO.File.ReadAllText(extensionYaml);

                    if (!string.IsNullOrEmpty(content))
                    {
                        var versionMatch = System.Text.RegularExpressions.Regex.Match(content, @"version\D+(\d[\.\d]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                        if (versionMatch.Success)
                        {
                            var version = versionMatch.Groups[1].Value;

                            if (settings.Settings.Version != version)
                            {
                                settings.Settings.Version = version;
                                settings.EndEdit(); // save it
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get version");
            }
        }

        public override ISettings GetSettings(bool firstRunSettings) => settings;

        public override UserControl GetSettingsView(bool firstRunSettings) => new GameVaultLibrarySettingsView(settings);

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args) => GetGamesAsync(args).GetAwaiter().GetResult();

        public async Task<IEnumerable<GameMetadata>> GetGamesAsync(LibraryGetGamesArgs args)
        {
            List<GameMetadata> gameMetadatas = new List<GameMetadata>();

            if (string.IsNullOrWhiteSpace(settings.Settings.ServerUrl))
            {
                API.Instance.Dialogs.ShowMessage("GameVault Server URL is not set in the settings", "GameVault Import Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }
            else if (string.IsNullOrWhiteSpace(settings.Settings.Username) || string.IsNullOrWhiteSpace(settings.Settings.Password))
            {
                API.Instance.Dialogs.ShowMessage("GameVault Username or Password is not set in the settings", "GameVault Import Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }

            // Get all the games from the server
            using (HttpClient client = new HttpClient() { MaxResponseContentBufferSize = int.MaxValue - 1, Timeout = TimeSpan.FromSeconds(60) })
            {
                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Playnite", API.Instance.ApplicationInfo.ApplicationVersion.ToString()));

                client.DefaultRequestHeaders.Add("X-Playnite-PluginId", Id.ToString());
                client.DefaultRequestHeaders.Add("X-Playnite-PluginName", Name);
                client.DefaultRequestHeaders.Add("X-Playnite-PluginVersion", settings.Settings.Version);

                var authenticationString = $"{settings.Settings.Username}:{settings.Settings.Password}";
                var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

                HttpResponseMessage response;

                try
                {
                    response = await client.GetAsync($"{settings.Settings.ServerUrl}/api/games?limit=0", args.CancelToken);
                }
                catch (Exception ex) 
                { 
                    API.Instance.Dialogs.ShowMessage($"Error: {ex}", "GameVault Import Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return null;
                }

                string content = "Unknown error";
                
                try
                {
                    content = await response.Content?.ReadAsStringAsync();
                }
                catch (Exception) { }

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        API.Instance.Dialogs.ShowMessage("Your username or password is incorrect", "GameVault Import Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                    else
                    {
                        API.Instance.Dialogs.ShowMessage($"Server error ({response.StatusCode}, {response.ReasonPhrase}): {content}", "GameVault Import Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }

                    return null;
                }

                var serverGames = Playnite.SDK.Data.Serialization.FromJson<Paginated<GameVaultServerGame>>(content);

                if (serverGames?.data?.Any() != true)
                {
                    API.Instance.Dialogs.ShowMessage("No games found", "GameVault Import Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return null;
                }

                foreach (var serverGame in serverGames.data)
                {
                    var gameMetadata = new GameMetadata()
                    {
                        GameId = serverGame.id.ToString(),
                        Name = serverGame.title,
                        Version = serverGame.version,
                        InstallSize = serverGame.size,
                    };

                    gameMetadatas.Add(gameMetadata);
                }
            }

            bool isRunning = await GameVaultClient.EnsureRunning(TimeSpan.FromSeconds(10), args.CancelToken);

            // If the client is not running, we can't get the installed status of games
            if (isRunning)
            {
                try
                {
                    foreach (var gameMetadata in gameMetadatas)
                    {
                        var installed = await GameVaultClient.TryQueryIsInstalled(gameMetadata.GameId, args.CancelToken, ensureRunning: false);

                        if (installed.Success)
                            gameMetadata.IsInstalled = installed.Result;
                    }
                }
                catch (Exception ex) 
                { 
                    API.Instance.Dialogs.ShowMessage($"Error checking install status: {ex}", "GameVault Import Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }

            return gameMetadatas;
        }

        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            if (args.NewValue == null)
                return;

            var gamevaultGames = args.NewValue.FindAll(x => x.PluginId == Id);

            if (gamevaultGames.Count == 0)
                return;

            _ = Task.Run(async () => await RefreshGameInformationAsync(gamevaultGames));
        }

        private CancellationTokenSource refreshGamesCts = null;

        private async Task RefreshGameInformationAsync(List<Game> gamevaultGames)
        {
            refreshGamesCts?.Cancel();
            refreshGamesCts = new CancellationTokenSource();

            if (!await GameVaultClient.EnsureRunning(TimeSpan.FromSeconds(10), refreshGamesCts.Token))
                return;

            using (API.Instance.Database.BufferedUpdate())
            {
                foreach (var game in gamevaultGames)
                {
                    // get installed status
                    var installed = await GameVaultClient.TryQueryIsInstalled(game.GameId, refreshGamesCts.Token, ensureRunning: false);
                    var changed = false;

                    if (installed.Success)
                    {
                        if (installed.Result != game.IsInstalled)
                        {
                            game.IsInstalled = installed.Result;
                            changed = true;
                        }

                        if (installed.Result)
                        {
                            var installPath = await GameVaultClient.TryQueryInstallDirectory(game.GameId, refreshGamesCts.Token, ensureRunning: false);

                            if (installPath.Success && game.InstallDirectory != installPath.Result)
                            {
                                game.InstallDirectory = installPath.Result;
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                        API.Instance.Database.Games.Update(game);
                }
            }
        }

        public override IEnumerable<InstallController> GetInstallActions(GetInstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            yield return new GameVaultInstallController(args.Game, this.GameVaultClient);
        }

        public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            yield return new GameVaultUninstallController(args.Game, this.GameVaultClient);
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            yield return new GameVaultPlayController(args.Game, this.GameVaultClient);
        }
    }
}