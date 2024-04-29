using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace GameVaultLibrary.Contollers
{
    internal class GameVaultInstallController : InstallController
    {
        private CancellationTokenSource watchToken = null;

        private readonly GameVaultLibraryClient Client;

        public GameVaultInstallController(Game game, GameVaultLibraryClient client) : base(game)
        {
            this.Name = "Install via GameVault";
            this.Client = client;
        }

        public override void Dispose()
        {
            base.Dispose();

            watchToken?.Cancel();
        }

        public override void Install(InstallActionArgs args)
        {
            watchToken?.Cancel();

            Client.InstallGame(Game.GameId);

            _ = WatchInstall();
        }

        private async Task WatchInstall()
        {
            var watchToken = this.watchToken = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                if (!(await Client.EnsureRunning(TimeSpan.FromSeconds(10), watchToken.Token)))
                    return;

                while (!watchToken.IsCancellationRequested)
                {
                    var installed = await Client.TryQueryIsInstalled(Game.GameId, watchToken.Token, ensureRunning: false);

                    if (installed.Success && installed.Result)
                    {
                        var path = await Client.TryQueryInstallDirectory(Game.GameId, watchToken.Token, ensureRunning: false);

                        if (path.Success)
                        {
                            InvokeOnInstalled(new GameInstalledEventArgs
                            {
                                InstalledInfo = new GameInstallationData()
                                {
                                    InstallDirectory = path.Result
                                }
                            });

                            break;
                        }
                    }

                    // Don't get stuck in a dumb loop
                    await Task.Delay(500, watchToken.Token);
                }
            }, watchToken.Token);
        }
    }
}
