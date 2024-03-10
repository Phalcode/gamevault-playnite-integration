using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace GameVaultLibrary.Contollers
{
    internal class GameVaultPlayController : PlayController
    {
        private Playnite.Common.ProcessMonitor processMonitor = new Playnite.Common.ProcessMonitor();
        private Stopwatch stopwatch = new Stopwatch();

        private readonly GameVaultLibraryClient Client;

        public GameVaultPlayController(Game game, GameVaultLibraryClient client) : base(game)
        {
            this.Name = "Play using GameVault";
            this.Client = client;
        }

        public override void Dispose()
        {
            base.Dispose();

            processMonitor?.Dispose();
        }

        public override void Play(PlayActionArgs args)
        {
            processMonitor?.Dispose();

            if (!string.IsNullOrEmpty(Game.InstallDirectory) && Directory.Exists(Game.InstallDirectory))
            {
                processMonitor = new Playnite.Common.ProcessMonitor();
                processMonitor.TreeStarted += ProcessMonitor_TreeStarted;
                processMonitor.TreeDestroyed += ProcessMonitor_TreeDestroyed;
                _ = processMonitor.WatchDirectoryProcesses(Game.InstallDirectory, false);
            }
            else
            {
                InvokeOnStopped(new GameStoppedEventArgs());
            }

            Client.StartGame(Game.GameId);
        }

        private void ProcessMonitor_TreeStarted(object sender, Playnite.Common.ProcessMonitor.TreeStartedEventArgs args)
        {
            stopwatch = Stopwatch.StartNew();
            InvokeOnStarted(new GameStartedEventArgs() { StartedProcessId = args.StartedId });
        }

        private void ProcessMonitor_TreeDestroyed(object sender, EventArgs args)
        {
            stopwatch.Stop();
            InvokeOnStopped(new GameStoppedEventArgs(Convert.ToUInt64(stopwatch.Elapsed.TotalSeconds)));
        }
    }
}
