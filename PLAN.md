# Casual City — Plan

A Cities: Skylines 1 mod that removes the most frustrating parts of the game for casual players: **flooding** and **traffic jams**. One DLL for **Windows and macOS (Intel and Apple Silicon)**. Target game version: **1.21.1-f9**.

## Features

All features have a switch in **Options → Casual City** and apply immediately, mid-game.

| ID | Feature | Default | Status | Patch (verified against decompiled `Assembly-CSharp.dll`) |
|---|---|---|---|---|
| F1 | Buildings never flood: no flood icons, abandonment or collapse | On | Built, untested in game | Postfix on every `CanSufferFromFlood(out bool)` (CommonBuildingAI + 7 overrides) → `false` |
| F2 | Roads never flood; parked cars are safe | On | Built, untested in game | Postfix on `RoadBaseAI.SimulationStep(ushort, ref NetSegment)` clears `Flooded` + flood icons; prefix skips `RoadBaseAI.FloodParkedCars` |
| F3 | Drain pipes don't raise the water level | On | Built, untested in game | Prefix on private `WaterFacilityAI.HandleWaterSource` (output only): keeps the facility water source alive but empty |
| F4 | Traffic slider: car use 10–100% of normal | 50% | Built, untested in game | Postfix on `ResidentAI.GetCarProbability(AgeGroup)` and `TouristAI.GetCarProbability(Vector3)` |
| F5 | Always sunny: no rain, fog or storms | On | Built, untested in game | Postfix on `WeatherManager.SimulationStepImpl` zeroes rain, fog and cloud targets |
| F6 | Remove existing floods: **"Keep the city dry"** switch + **"Drain flood water now"** button | On | Built, untested in game | `FloodDrainer`: finds ground-level buildings and roads under water and places temporary pumps (facility `WaterSource` with an input rate, like pumping trucks). The pump floor `m_target` = the object's ground height, so lakes, rivers and the sea are never lowered below the city's ground level. |

### F6 performance and safety
- The scan runs on the simulation thread (`ThreadingExtensionBase.OnAfterSimulationFrame`). It checks 1/256 of the buildings and roads per frame, the same cadence and cost as the game's own flood check, which F1 turns off. When the switch is off and no "Drain now" is active, it does nothing.
- At most 32 pumps at a time, each about 40 m apart, each living 128 frames. A pump only touches about 30 water cells.
- **Pumps never reach a save file:** a prefix on `WaterSimulation.Data.Serialize` releases them first. Saves run on the simulation thread, so a pump can't be created mid-save. Pumps are tagged (`m_outputPosition.y = -4242`), and any leftovers are removed when a city loads.
- Only buildings placed `OnGround` / `Roadside` / `PathsideOrGround` and only `RoadAI` (ground) roads are drained. Harbors, drain pipes, pumps, dams, bridges and tunnels are skipped.

### Known limits
- **F3** stops *new* water. Use F6 to remove water that has already built up.
- **F6**: a lake that has risen above its banks refills the city a few times. Each drain removes that water for good, so the lake drops until it stays inside its banks. This works best with F3 on.
- **F4** only affects residents and tourists. Trucks, service vehicles and buses are unchanged (use TM:PE vehicle restrictions and Transfer Manager CE for those). Vanilla car chances are already low (adults 20%), so trucks are often a big share of traffic.
- **F2**: a flooded road still looks wet. Flood-pumping trucks may still be dispatched.

## Compatibility
- **TM:PE:** TM:PE's `GetVehicleInfo` prefixes still call the game's `GetCarProbability`, so F4 applies under TM:PE. Its `SimulationStep` prefixes don't skip the original and have no flood logic.
- **Rule:** only small prefixes/postfixes on the narrowest method. Never use transpilers or skip-the-original prefixes on methods TM:PE patches (`SimulationStep`, `GetVehicleInfo`, `UpdateLanes`).
- **Requires:** the Harmony Workshop mod (**2040656402**).

## Repo layout
```
CasualCity.sln
src/CasualCity/
  CasualCity.csproj        net35, game DLL paths per OS, deploys to the local mods folder after build
  Mod.cs                   IUserMod: name, settings UI, enable/disable
  Settings.cs              settings saved in CasualCity.cgs (game settings folder)
  Patcher.cs               applies/removes Harmony patches
  FloodDrainer.cs          F6 scanner and pumps
  Extensions.cs            simulation-frame hook and level load/unload hooks
  Patches/                 one file per patched feature (F1–F5) + SaveGuardPatch (F6)
assets/PreviewImage.png    Workshop thumbnail (644×360)
```

## Build

### Why one DLL runs everywhere
The game loads mods as managed .NET 3.5 DLLs on Mono on every OS. On M1 the game runs as x86_64 under Rosetta 2, and the DLL is CPU-independent. Building on Windows or on Mac gives the same DLL.

### Prerequisites
| | Windows | macOS |
|---|---|---|
| .NET SDK 8+ | `winget install Microsoft.DotNet.SDK.10` (system-wide; a user-folder SDK is hidden by `C:\Program Files\dotnet` on PATH) | arm64 installer from dot.net |
| Game DLLs (default path) | `C:\Program Files (x86)\Steam\steamapps\common\Cities_Skylines\Cities_Data\Managed` | `~/Library/Application Support/Steam/steamapps/common/Cities_Skylines/Cities.app/Contents/Resources/Data/Managed` |
| Local mods folder | `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods` | `~/Library/Application Support/Colossal Order/Cities_Skylines/Addons/Mods` |

Other install locations: `dotnet build -p:ManagedDir="..." -p:ModsDir="..."`.

### Commands
```
dotnet build -c Release                     # build + copy to <ModsDir>/CasualCity
dotnet build -c Release -p:DeployMod=false  # build only
```
Game DLLs are referenced, never copied or committed. `*.dll` is git-ignored.

### Code rules (so it keeps working on Mac)
1. .NET 3.5 APIs only (no `Task`, `async`, `string.IsNullOrWhiteSpace`, `Enum.HasFlag`, …). Newer C# syntax is fine.
2. No native code, P/Invoke, registry or Windows-only assemblies.
3. Paths via `Path.Combine` and the game's `DataLocation`; never hard-coded.
4. UI only through the game's `UIHelperBase` / ColossalFramework.UI.
5. Log with `Debug.Log("[CasualCity] ...")`. On Windows the log is `Cities_Data/output_log.txt`; on Mac it's `~/Library/Logs/Unity/Player.log`.

## Test (in game)
1. `dotnet build -c Release`, start the game, then **Content Manager → Mods → Casual City → enable**. Harmony must be subscribed.
2. **F1/F2:** in a test city, put buildings and a road below the water level (Landscaping tool). Expect no flood icons and no collapse. Turn the switches off and expect flooding to return.
3. **F3:** place several drain pipes in a small lake. The water level should stay flat over a few in-game weeks.
4. **F4:** compare the cars on a busy road at 100% and at 50%, with TM:PE on and off.
5. **F5:** the weather clears within a few in-game days and stays clear.
6. **F6:** load a flooded city with "Keep the city dry" off, then press "Drain flood water now". The water on buildings and roads should disappear within seconds, while lakes and the sea stay. Save, reload, and confirm no pumps remain (no water vanishing anywhere with F6 off). Compare FPS and simulation speed with F6 on and off.
7. **Mac:** copy `Mods/CasualCity/` to the Mac's mods folder, or subscribe to the Workshop item (below). Repeat a quick check.

## Install on another computer (e.g. the Mac) without the Workshop
A Release build also creates `dist/CasualCity.zip` (the same DLL works on Windows and Mac).
1. Copy the zip to the Mac (USB stick, OneDrive/Google Drive, email) and unzip it, which gives a `CasualCity` folder.
2. In Finder: **Go → Go to Folder…**, paste `~/Library/Application Support/Colossal Order/Cities_Skylines/Addons/Mods` (create `Addons/Mods` if missing), and drop the `CasualCity` folder in.
3. **Harmony** is also needed. If the Mac already runs Harmony-based mods (TM:PE, Move It, …), it's already there. If not, copy the Harmony mod folder (Workshop item 2040656402, under `steamapps/workshop/content/255710/2040656402` on the PC) into the same `Mods` folder as `Harmony`.
4. Start the game: **Content Manager → Mods**, enable Casual City (and Harmony).
5. To update, replace the `CasualCity` folder with the new one.

## Publish (Steam Workshop)
1. Build on Windows, which fills `Addons/Mods/CasualCity/` (DLLs + `PreviewImage.png`).
2. In game: **Content Manager → Mods → Casual City → Share**. Add a title, description and the Mod tag.
3. Set visibility to Hidden first, test on the Mac, then make it Public.
4. Add **Harmony (2040656402)** as a required item on the Workshop page.
5. To update later: rebuild, then **Share** again on the same item.

## Next
- [ ] In-game test of F1–F6 on Windows (with and without TM:PE)
- [ ] Mac test
- [ ] Publish v0.1 (hidden → public)
- [ ] Choose a license (MIT suggested)
