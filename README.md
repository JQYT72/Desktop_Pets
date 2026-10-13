# Desktop Pets

A Windows desktop pet MVP built with .NET 8 and WPF.

## What Is In This MVP

- Transparent, borderless, topmost WPF pet window.
- Built-in sample pet image.
- Mouse drag to move the pet.
- Click bounce feedback and idle pseudo-animation.
- Drag and drop image import for PNG, JPG, JPEG, and WEBP files.
- Drag and drop Codex pet folders or `pet.json` files generated under `.codex/pets/<pet-id>/`.
- Imported assets are copied into managed user data.
- The last pet and window position are restored on restart.
- Right-click menu with Exit.

## Requirements

- Windows 10/11.
- .NET 8 SDK.

The SDK can be installed with:

```powershell
winget install --id Microsoft.DotNet.SDK.8 --exact
```

## Run

```powershell
dotnet run --project src/DesktopPets.App/DesktopPets.App.csproj
```

## Use The Manager UI

Right-click the pet and choose `管理桌宠` or `导入桌宠包`.

The manager window supports:

- selecting `pet.json`
- selecting `spritesheet.webp`
- importing both files and playing the pet
- previewing every Codex animation state with action buttons
- dragging a pet folder into the window
- opening `%APPDATA%/DesktopPets`

Right-click the pet and open `Animations` to trigger the same states directly from the pet window:

- `Idle`
- `Run Right`
- `Run Left`
- `Wave`
- `Jump`
- `Failed`
- `Waiting`
- `Working`
- `Review`

## Interaction Behavior

- Move the mouse over the pet to trigger a waving greeting.
- Click the pet repeatedly to cycle through every Codex animation state.
- Drag left or right to play the matching directional running animation.
- When Auto Walk is enabled, animated pets slowly roam left and right around the last dropped position.
- The manager window includes an `Auto Walk` toggle and a `Back To Idle` button.

## Test

```powershell
dotnet build
dotnet test
```

## Publish A Single EXE

Create a self-contained Windows x64 executable:

```powershell
dotnet publish src/DesktopPets.App/DesktopPets.App.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish/win-x64 /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None /p:DebugSymbols=false
```

The output is:

```text
artifacts/publish/win-x64/DesktopPets.App.exe
```

This executable includes the .NET runtime, so most Windows users can run it by double-clicking without installing the SDK.

## User Data

Long-lived user data is stored outside the install directory:

```text
%APPDATA%/DesktopPets/
  config.json
  pets/
    generated/
```

Each imported pet is stored as:

```text
pets/generated/<pet-id>/
  pet.json
  source/input.<ext>
  sprites/idle.<ext>
```

The app never depends on the original dragged file path after import.

## Codex Animated Pets

Codex-generated pets can be imported directly. Drag either the whole pet folder or its `pet.json` file into the pet window:

```text
C:/Users/Administrator/.codex/pets/guagua-cat/
  pet.json
  spritesheet.webp
```

The app copies the pet package into `%APPDATA%/DesktopPets/pets/generated/` and plays the standard Codex spritesheet rows:

You can also drag `pet.json` and `spritesheet.webp` into the window together. They do not have to already be in the same folder, as long as `pet.json` references the spritesheet filename.

- `idle` loops while standing.
- `waving` plays on click.
- `running-right` / `running-left` play while dragging.
