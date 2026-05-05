# LinkLauncher

LinkLauncher is a desktop Minecraft launcher built with **.NET 10** and **Avalonia UI**. The project brings version management, profiles, RAM settings, Microsoft login, and mod loader installation into a single application, with support for **Fabric**, **Forge**, **NeoForge**, and **Quilt**.

The goal is straightforward: provide a clean, modern foundation for launching Minecraft with multiple configurations without relying on the official launcher for the full day-to-day experience.

## Overview

The launcher is split into two main parts:

- `LinkLauncher.App`: desktop UI, window state, commands, and user interaction.
- `LinkLauncher.Core`: authentication, persistence, Java detection, Minecraft integration, and mod loader installation.

In practice, the app starts, loads saved settings, fetches available Minecraft versions, lets the user choose an installation, and prepares everything needed to launch the game.

## What the project does

- Supports **offline** and **online** launch modes.
- Uses Microsoft login through **device code flow**.
- Manages **multiple installations** with create, duplicate, and delete actions.
- Lets the user choose Minecraft versions with filters for `release`, `snapshot`, `old beta`, and `old alpha`.
- Configures **RAM per installation**.
- Supports **Vanilla**, **Fabric**, **Forge**, **NeoForge**, and **Quilt**.
- Shows an internal console with launcher status and Minecraft process logs.
- Saves local settings and profiles to disk.

## How it works

### 1. Application startup

When the app starts, `App.axaml.cs` creates the main services, loads saved settings, and fetches the available Minecraft versions. `MainWindowViewModel` receives that data and updates the UI.

### 2. Profiles and installations

Each installation is represented by a `LauncherProfile`. A profile stores:

- installation name
- Minecraft version
- RAM amount
- player name
- selected loader and loader version

The launcher creates an isolated instance folder per profile while still sharing the main game folder for libraries, assets, and versions.

### 3. Microsoft login

Online login uses **device code flow**. The app displays the code/message returned by Microsoft, the user completes authentication in the browser, and the launcher receives a valid session to launch Minecraft with a Microsoft account.

### 4. Launch preparation

Before Minecraft starts, the launcher:

1. loads the selected profile
2. builds a `LaunchRequest`
3. resolves the final version name to launch
4. finds a compatible Java installation
5. ensures the selected loader is installed, if any
6. creates the Minecraft process through `CmlLib.Core`

If the selected profile uses `Forge` or `NeoForge`, the project also prepares the required vanilla installation before running the loader installer.

### 5. Progress and logs

During downloads, verification, and launch, the app receives progress updates and process output. Those events feed:

- the status area in the main window
- the progress bars
- the internal console
- the error and event list

## Project structure

```text
LinkLauncher.slnx
|
+-- LinkLauncher.App/
|   +-- App.axaml
|   +-- MainWindow.axaml
|   +-- Program.cs
|   +-- ViewModels/
|   +-- Services/
|   \-- Assets/
\-- LinkLauncher.Core/
    +-- Auth/
    +-- Launch/
    +-- Models/
    +-- ModLoaders/
    +-- Storage/
    \-- Utilities/
```

## Technologies used

- **Avalonia UI** for the cross-platform desktop interface
- **CmlLib.Core** for Minecraft integration
- **CmlLib.Core.Auth.Microsoft** for Microsoft authentication
- **XboxAuthNet.Game.Msal** for the login flow
- **Newtonsoft.Json** for settings persistence

## Local data and persistence

By default, the project uses:

```text
%LocalAppData%\LinkLauncher
```

Inside it, the main data is organized like this:

```text
LinkLauncher/
+-- settings.json
+-- GameData/
\-- Instances/
    \-- <profile-id>/
```

### What each folder stores

- `settings.json`: global settings, selected profile, and installation list
- `GameData/`: libraries, assets, versions, and shared game data
- `Instances/<profile-id>/`: isolated folder for the selected instance

## Supported loaders

The project currently supports:

- Vanilla
- Fabric
- Forge
- NeoForge
- Quilt

Loader versions are fetched from the official sources for each ecosystem. The final version name used for launch is built automatically from the selected profile.

## Build and run

From the project root:

```powershell
dotnet build .\LinkLauncher.App\LinkLauncher.App.csproj
dotnet run --project .\LinkLauncher.App\LinkLauncher.App.csproj
```

If you prefer to work directly inside the app folder:

```powershell
cd .\LinkLauncher.App
dotnet build
dotnet run
```

## Requirements

- **.NET SDK 10**
- internet access to fetch metadata, loaders, and assets
- a **Java** installation compatible with the selected Minecraft version

The launcher tries to detect the best Java version automatically before launch.

## Current state

The project already covers the main flow of a functional launcher:

- load settings
- manage installations
- authenticate with Microsoft
- prepare mod loaders
- launch Minecraft
- track progress and logs

It is a solid base for continuing to improve the app with more options, better UX, packaging, and distribution.
