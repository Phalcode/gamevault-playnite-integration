using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace GameVaultLibrary.Contollers
{
    internal class GameVaultUninstallController : UninstallController
    {
        private CancellationTokenSource watchToken = null;

        private readonly GameVaultLibraryClient Client;

        public GameVaultUninstallController(Game game, GameVaultLibraryClient client) : base(game)
        {
            this.Name = "Uninstall via GameVault";
            this.Client = client;
        }

        public override void Dispose()
        {
            base.Dispose();

            watchToken?.Cancel();
        }

        public override void Uninstall(UninstallActionArgs args)
        {
            watchToken?.Cancel();

            Client.UninstallGame(Game.GameId);

            _ = WatchUninstall();
        }

        private async Task WatchUninstall()
        {
            var watchToken = this.watchToken = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                if (!(await Client.EnsureRunning(TimeSpan.FromSeconds(10), watchToken.Token)))
                    return;

                while (!watchToken.Token.IsCancellationRequested)
                {
                    var installed = await Client.TryQueryIsInstalled(Game.GameId, watchToken.Token, ensureRunning: false);

                    if (installed.Success && !installed.Result)
                    {
                        InvokeOnUninstalled(new GameUninstalledEventArgs());
                        break;
                    }

                    // Don't get stuck in a dumb loop
                    await Task.Delay(1000, watchToken.Token);
                }
            }, watchToken.Token);
        }
    }
}
