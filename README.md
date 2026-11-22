# Intended Experience

Play GLS maps and modcharts the way the mapper made them.

GLS maps (v3+ environments, Weave and newer) take their light colors from the color scheme, so the mapper can't lock them in like Chroma can. An **All** color override, or effects set to **Off** or **No Flickering**, changes the lightshow. Modcharts (maps requiring Noodle Extensions, Mapping Extensions or Vivify) have the same problem.

Intended Experience fixes the settings when you launch one of these maps, **for that map only**. Your saved settings never change.

## Supported versions

| Beat Saber | BSIPA | BSML | SiraUtil | SongCore |
|---|---|---|---|---|
| 1.44.1 | 4.3.0+ | 1.14.1+ | 3.3.1+ | 3.16.0+ |
| 1.40.8 | 4.3.0+ | 1.12.5+ | 3.2.1+ | 3.15.3+ |

Optional: **Heck** (Settings Setter support) and **BeatSaberPlus GameTweaker** (brightness setting).

## Install

Download the zip for your game version from [Releases](https://github.com/Fatalution/IntendedExperience/releases) and extract it into your Beat Saber folder. You can get the requirements from [BSManager](https://www.bsmanager.io) or [BeatMods](https://beatmods.com).

## What it changes

- **Enable All Effects**: Off / No Flickering becomes All Effects.
- **Use Intended Colors**: an All color override becomes Notes Only, and the map's SongCore environment colors are turned on. If the map sets both environment color pairs, your override stays, since the map's colors already replace it. Note and wall colors stay yours.
- **Set Effects Brightness to 100%**: resets BeatSaberPlus GameTweaker's brightness override.

GLS maps and modcharts have separate toggles. A modchart on a GLS environment uses the modchart ones.

## Using it

- **Mod Settings → Intended Experience**: toggles and the list of excluded maps.
- **IE button** next to Play: exclude or include the selected map. Crossed out means excluded.
- **Orange Play button**: IE will change something. Hover over it to see what.
- **Gameplay Setup → Mods → Intended Experience**: the same exclude toggle and the list of changes.

## Heck Settings Setter

If a map's Settings Setter sets effects or the color override itself, IE leaves that setting alone.

IE can also list its changes in the Settings Setter window (**Show changes in Settings Setter** in the settings). Maps with their own Settings Setter always list them there. Accept applies them, Decline skips them.

## How it works

IE hooks the level start and passes the game modified copies of your settings. SongCore's environment colors and BeatSaberPlus brightness are only changed in memory for that map. Nothing is saved, so there's nothing to undo if the game crashes.

## Building

Create `IntendedExperience/IntendedExperience.csproj.user` pointing at your game:

```xml
<Project>
  <PropertyGroup>
    <BeatSaberDir>C:/Path/To/Beat Saber</BeatSaberDir>
  </PropertyGroup>
</Project>
```

Then run `dotnet build` (or `dotnet build -c Release` for a zip).

For 1.40.8, add `-p:TargetGame=1.40.8 -p:BeatSaberDir="C:/Path/To/Beat Saber 1.40.8"`, or set `BeatSaberDir_1_40_8` in the `.csproj.user`. Output goes to `bin/1.40.8/`.

## Issues

Report bugs on [GitHub Issues](https://github.com/Fatalution/IntendedExperience/issues) and attach `Logs/_latest.log` if you can.

## License

[MIT](LICENSE) © Fatalution
