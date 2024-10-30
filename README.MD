# GameVault Playnite Integration

This Playnite extension allows you to integrate GameVault with Playnite using GameVault Plus’s exclusive CLI and URI interface. It won't work if you are not subscribed to GameVault+.

## Acknowledgments

- Special thanks to [@Zallist](https://github.com/Zallist) for originally developing this integration.

## Development Setup

To set up this project for development:

1. Initialize submodules:
   ```bash
   git submodule update --init
   ```
2. Restore NuGet packages.
3. Install or restore the Playnite SDK Extension via NuGet.
4. Build a release version.
5. In Playnite, navigate to `Settings -> For Developers`, then click `+` to add the built `GameVaultLibrary` folder as a custom extension. 