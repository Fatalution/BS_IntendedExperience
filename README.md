# Intended Experience

Sets up your player settings so GLS maps, Chroma maps and modcharts look the way the mapper made them.

## Install

### <img src="https://raw.githubusercontent.com/Fatalution/BS_Mods/main/docs/encore-logo.svg" height="22" alt=""> Encore

[![Add to Encore](https://img.shields.io/badge/Encore-Add%20repository-orange)](https://fatalution.github.io/BS_Mods/add-to-encore.html)

If the badge doesn't open Encore, paste this into your browser's address bar:

```
encore://add-source?url=https://raw.githubusercontent.com/Fatalution/BS_Mods/main/index.json
```
<img width="1415" height="583" alt="image" src="https://github.com/user-attachments/assets/6fbc9582-47e6-4bbe-be2e-d5548f2d4e9c" />

[What's Encore?](https://encore.scoresaber.com)

### Manually

Alternatively, grab the zip for your game version from [Releases](https://github.com/Fatalution/BS_IntendedExperience/releases) and extract it into your Beat Saber folder. The requirements are on [BSManager](https://www.bsmanager.io) and [BeatMods](https://beatmods.com).

## Why this exists

I got tired of switching settings back and forth between ranked maps with v2 autolights and proper GLS lightshows and modcharts, so I made this mod to do it for me.

- GLS maps (v3+ environments, Weave and newer) use the colors from your color scheme by default. Mappers can't force their colors the way Chroma maps can. And so, with "All" color override, the lightshow looks wrong.
- With effects set to Off or No Flickering, the lightshow will likely be broken or not seen - unless the mapper set up Heck's Settings Setter to switch them on.
- Modcharts (Noodle Extensions, Mapping Extensions or Vivify) have the same problems when they don't come with a Settings Setter.
- Thinking about all of this when choosing a map to play is headache-inducing.

Intended Experience switches the relevant settings for these maps and puts everything back when you move on to another map.

## Example

1. You play a few ranked maps with v2 autolights. Like a lot of players (me included), you keep effects off or dimmed and use your own color scheme.
2. Then you come across a great curated, unranked GLS map.
3. To see it the way it was meant to be played, you'd normally have to change a bunch of settings by hand. IE does that for you.
4. You go back to ranked, and your settings are exactly as you left them.
5. Got a ranked GLS map where you'd rather keep your own settings? Press the IE button next to Play and it's left alone.

## Screenshots

**Settings**

<img width="700" alt="Intended Experience settings" src="https://github.com/user-attachments/assets/9a6d9f85-9a16-4826-a5ca-9e2c166bbf53" />

**Hovering over the Play button** shows what will change.

<img width="700" alt="Play button tooltip listing the changes" src="https://github.com/user-attachments/assets/fb5246e5-6e2e-47a5-a263-c32d16a96593" />

**Optional confirmation window** for maps without a Settings Setter.

<img width="700" alt="Intended Experience confirmation window" src="https://github.com/user-attachments/assets/f18f90eb-6fe8-4bc6-9b31-316af09c7912" />

**Maps with their own Settings Setter** get IE's changes added to it.

<img width="700" alt="Settings Setter window with Intended Experience rows" src="https://github.com/user-attachments/assets/2cd5be44-a752-4f11-b794-ba35ca4c97f3" />

## Supported versions

| Beat Saber | BSIPA | BSML | SiraUtil | SongCore |
|---|---|---|---|---|
| 1.44.1 | 4.3.0+ | 1.14.1+ | 3.3.1+ | 3.16.0+ |
| 1.40.8 | 4.3.0+ | 1.12.5+ | 3.2.1+ | 3.15.3+ |
| 1.29.1 | 4.3.0+ | 1.6.10+ | 3.1.2+ | 3.11.1+ |

1.29.1 is supported by 0.4.0 only and won't get further updates.

Optional: Heck for Settings Setter support, Chroma for Chroma maps, BeatSaberPlus GameTweaker for the brightness option, BeatSaberPlus Multiplayer+ for lobbies.

Every setting and where to find things in game is in [USAGE.md](USAGE.md).

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

For 1.40.8, add `-p:TargetGame=1.40.8 -p:BeatSaberDir="C:/Path/To/Beat Saber 1.40.8"` or set `BeatSaberDir_1_40_8` in the `.csproj.user`. The output goes to `bin/1.40.8/`. 1.29.1 works the same way with `-p:TargetGame=1.29.1` and `bin/1.29.1/`.

## Issues

Report bugs on [GitHub Issues](https://github.com/Fatalution/BS_IntendedExperience/issues), ideally with `Logs/_latest.log` attached.

## AI disclosure

This mod was written with help from AI (Claude). I designed, tested and maintain it myself.

## License

[GPL-3.0](LICENSE) © Fatalution
