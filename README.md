# Intended Experience

See GLS lightshows and modcharts the way the mapper made them.

Maps for v3+ environments (**Weave and newer**) use the group lighting system (GLS), where the lights take their colors from the color scheme. Chroma colors each event directly, but a GLS mapper has no way to enforce the colors they intended. If your colors override is set to **All**, or your environment effects are set to **Off** or **No Flickering**, the lightshow you see isn't the one the mapper designed.

**Modcharts**, meaning maps that require Noodle Extensions, Mapping Extensions or Vivify, get the same treatment.

Intended Experience applies the settings these maps need when you launch them, **for that map only**. Your saved settings are never changed, so the next map plays with your normal settings.

**Game version:** 1.44.1

## Features

- **Enable All Effects**: if your environment effects are set to **Off** or **No Flickering**, the map is played with **All Effects**.
- **Use Intended Colors**: if your colors override is set to **All**, the map is played with **Notes Only**, so the lights keep the map's colors. If the map has its own environment colors, they're turned on through SongCore. If it sets both environment color pairs (normal and boost), those already replace your override, so it isn't switched to Notes Only. Note and wall colors stay yours.
- **Set Effects Brightness to 100%**: if [BeatSaberPlus](https://github.com/hardcpp/BeatSaberPlus) GameTweaker overrides your light brightness, the map is played at 100%. Your BeatSaberPlus setting isn't changed.
- **Per-map exclusions**: keep your own settings on maps you choose.

## Install

1. Install the [requirements](#requirements); all of them are available in [BSManager](https://www.bsmanager.io) and on [BeatMods](https://beatmods.com).
2. Download the latest release from [Releases](https://github.com/Fatalution/IntendedExperience/releases).
3. Extract it into your Beat Saber folder, so that `IntendedExperience.dll` ends up in `Plugins`.

### Requirements

- BSIPA ^4.3.0
- BeatSaberMarkupLanguage ^1.14.1
- SiraUtil ^3.3.1
- SongCore ^3.16.0

Optional: BeatSaberPlus GameTweaker, for the brightness setting.

## Usage

- **Settings → Mod Settings → Intended Experience**: turn the mod on or off, set the rules separately for GLS maps and modcharts, and manage excluded maps. A map that is both uses the modchart rules.
- **IE button** (next to Play, shown on GLS maps and modcharts only): click it to exclude the selected map or include it again. A struck-out "IE" means the map is excluded.
- **Gameplay Setup → Mods → Intended Experience** shows what the next launch will change and has the same exclude toggle.

## How it works

The mod hooks the level start and hands the game modified copies of your settings for that one launch. It works like Chroma's settings setter, but without the pop-up. Nothing is written to your player data, so there's nothing to restore afterwards, even if the game crashes mid-map.

SongCore's environment colors setting is turned on only while the level loads, and back off straight after. That change stays in memory, so SongCore's config file is never touched.

The BeatSaberPlus brightness works the same way. IE replaces the value GameTweaker loads at level start with 100%, for that map and its restarts only, and never changes GameTweaker's config.

### Heck Settings Setter

If a map uses Heck's Settings Setter, that pop-up is the mapper's own call, so IE stays out of its way:

- If the Settings Setter sets **environment effects**, Enable All Effects isn't applied.
- If it sets **Override Default Colors**, the colors override isn't switched to Notes Only.

This applies whether you accept or decline the pop-up. Everything the Settings Setter doesn't set still applies, such as the map's SongCore environment colors and the BeatSaberPlus brightness.

#### Show changes in Settings Setter (optional)

Turn this on under Menu in IE's settings to have IE list its own changes in Heck's Settings Setter window when you press Play, for example `[Intended Experience] Color Override: All -> Notes Only`.

- If the map has its own Settings Setter, IE's rows appear next to the mapper's. This happens even with the option off, because that window appears anyway.
- If it doesn't, the window appears with only IE's rows.
- **Accept** applies IE's changes for that map. **Decline** applies none of them.
- Launches that skip the window apply IE as usual. That includes built-in maps and songs started for you by BeatSaberPlus Multiplayer+.

## Building

1. Create `IntendedExperience/IntendedExperience.csproj.user` that points at a Beat Saber install with the requirements installed:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <Project>
     <PropertyGroup>
       <BeatSaberDir>C:/Path/To/Beat Saber</BeatSaberDir>
     </PropertyGroup>
   </Project>
   ```
2. Run `dotnet build`. [BSMT](https://github.com/Zingabopp/BeatSaberModdingTools) copies the plugin into `<BeatSaberDir>/Plugins`.
3. For a release, run `dotnet build -c Release`. This also produces a release zip in `IntendedExperience/bin/Release/net472/zip`.

## Issues

Report bugs and suggestions on [GitHub Issues](https://github.com/Fatalution/IntendedExperience/issues). Attach `Logs/_latest.log` from your Beat Saber folder when you can.

## License

[MIT](LICENSE) © Fatalution
