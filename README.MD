# GameVault Playnite Integration

This Playnite extension allows you to integrate GameVault with Playnite using [GameVault+](https://gamevau.lt/gamevault-plus)’s exclusive CLI and URI interface. It won't work if you are not subscribed to [GameVault+](https://gamevau.lt/gamevault-plus).

## Usage

1. [Set Up GameVault Plus in your GameVault Client](https://gamevau.lt/docs/gamevault-plus/introduction#get-started).
2. Install the latest version of this extension using Playnite's Addon Browser or by adding the `.pext` file from the Releases.
3. Configure the integration in Playnite:
   - Go to `Library -> Configure Integrations... -> GameVault`.
   - Enter your GameVault server URL (include `http://` or `https://`).
   - Enter your GameVault user credentials.
4. To sync your library, select `Update Game Library -> GameVault`.

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
5. In Playnite, navigate to `Settings -> For Developers`, then click `Add` to add the built `GameVaultLibrary` folder inside `bin` as a custom extension. 

## Releasing

1. Update the application version in the sources.
2. Build the extension.
3. Package the extension using the Playnite Toolbox from the included Playnite submodule.
   ``.\Toolbox.exe pack "`Path\TO\gamevault-playnite-integration\source\Libraries\GameVaultLibrary" "."``
4. Create a new release on GitHub:
   - Create the corresponding GitHub tag.
   - Add a changelog in the release notes.
   - Upload the `.pext` file and copy its link.
5. Add a new entry in the [manifest file](manifests/installer.yaml).
