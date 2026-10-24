# city_skylines_casual_city

**Casual City** is a Cities: Skylines 1 mod for relaxed play. It works on Windows and macOS (including Apple Silicon).

- **Buildings and roads never flood**
- **Drain pipes don't raise the water level**
- **Remove existing floods**: a "Drain flood water now" button and a "Keep the city dry" switch
- **Traffic slider**: fewer residents and tourists drive (10–100% of normal)
- **Always sunny**: no rain, fog or storms

Every feature can be switched on or off in **Options → Casual City**. Requires the [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402) mod. Works alongside TM:PE.

## Build

```
dotnet build -c Release
```

This builds `CasualCity.dll` and copies it to the game's local mods folder. See [PLAN.md](PLAN.md) for prerequisites, paths, testing and publishing.
