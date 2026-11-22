# Intended Experience

Makes GLS maps and modcharts look the way the mapper made them.

GLS maps (v3+ environments, Weave and newer) take their light colors from your color scheme, so mappers can't force their colors the way Chroma maps can. With an "All" color override, or effects set to Off or No Flickering, you end up seeing a different lightshow. Modcharts (Noodle Extensions, Mapping Extensions or Vivify) run into the same thing.

Intended Experience switches the relevant settings when you start one of these maps and puts everything back for the next one.

## Supported versions

| Beat Saber | BSIPA | BSML | SiraUtil | SongCore |
|---|---|---|---|---|
| 1.44.1 | 4.3.0+ | 1.14.1+ | 3.3.1+ | 3.16.0+ |
| 1.40.8 | 4.3.0+ | 1.12.5+ | 3.2.1+ | 3.15.3+ |

Optional: Heck for Settings Setter support, BeatSaberPlus GameTweaker for the brightness option.

## Install

Grab the zip for your game version from [Releases](https://github.com/Fatalution/BS_IntendedExperience/releases) and extract it into your Beat Saber folder. The requirements are on [BSManager](https://www.bsmanager.io) and [BeatMods](https://beatmods.com).

## Settings

- Enable All Effects: switches Off / No Flickering to All Effects.
- Use Intended Colors: switches an All color override to Notes Only and turns on the map's SongCore environment colors. Maps that set both environment color pairs keep your override, as their colors already cover it. Note and wall colors stay the same.
- Set Effects Brightness to 100%: resets BeatSaberPlus GameTweaker's brightness override.

GLS maps and modcharts each have their own set of these. Modcharts on GLS environments use the modchart set.

## In game

- Mod Settings → Intended Experience: settings and excluded maps.
- IE button next to Play: excludes the selected map (crossed out when excluded).
- Orange Play button: IE will change something for this map. Hover over it to see what.
- Gameplay Setup → Mods → Intended Experience: exclude toggle and the list of changes.

## Heck Settings Setter

Anything a map's Settings Setter already sets is left to it. IE adds its own rows to that window, so you can accept or decline everything in one place. For maps without a Settings Setter, turn on "Show changes in Settings Setter" to get the same kind of window for IE's changes.

## How it works

IE hooks the level start and hands the game edited copies of your settings for that map. SongCore's environment colors and the BeatSaberPlus brightness get swapped in memory for the map and restored afterwards, so nothing is written to your settings files.

## Building

Create `IntendedExperience/IntendedExperience.csproj.user` pointing at your game:

```xml
<Project>
  <PropertyGroup>
    <BeatSaberDir>C:/Path/To/Beat Saber</BeatSaberDir>
  </PropertyGroup>
</Project>
```

Then run `dotnet build`, or `dotnet build -c Release` for a zip.

For 1.40.8, add `-p:TargetGame=1.40.8 -p:BeatSaberDir="C:/Path/To/Beat Saber 1.40.8"` or set `BeatSaberDir_1_40_8` in the `.csproj.user`. The output goes to `bin/1.40.8/`.

## Issues

Report bugs on [GitHub Issues](https://github.com/Fatalution/BS_IntendedExperience/issues), ideally with `Logs/_latest.log` attached.

## License

[MIT](LICENSE) © Fatalution
