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

LinkLauncher is designed around a simple flow:

- load saved settings and available game versions
- let the user pick an installation profile
- prepare the selected game setup and dependencies
- launch Minecraft and report progress back to the interface

Each installation keeps its own profile-specific settings while still allowing shared game data where appropriate.

## Technologies used

- **Avalonia UI** for the cross-platform desktop interface
- **CmlLib.Core** for Minecraft integration
- Microsoft authentication support
- **Newtonsoft.Json** for settings persistence

## Local data and persistence

The launcher stores local settings and instance data in the user application data directory. This includes saved preferences, selected installations, and game-related local files needed for launch.

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
