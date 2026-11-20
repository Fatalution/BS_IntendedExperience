# Intended Experience

A Beat Saber mod that plays every map the way its mapper intended. When you launch a map, it applies the right lighting and color settings **for that map only**. Your saved settings are never changed, so the next map plays with your normal settings.

Targets **Beat Saber 1.44.1**.

## What it does

The rules apply to maps made for v3+ (group lighting) environments, meaning **Weave and newer**:

| Setting | When | What happens for that map |
|---|---|---|
| **Enable All Effects** | Your environment effects are reduced or off | All effects are turned on |
| **Use Intended Colors** | Your colors override is set to **All** | **Notes Only** is used, so the lights keep the map's colors |
| | The map has its own environment colors and SongCore's environment colors are off | SongCore's environment colors are turned on. Note and wall colors are untouched |

### How it stays temporary

The mod changes the settings the game receives when the level starts, not the settings it saves. It works the same way as Chroma's settings setter, minus the pop-up. There's nothing to restore afterwards, even if the game crashes mid-map.

## In game

- **Settings → Mod Settings → Intended Experience**: a master switch, the two settings above, the Play button tint, and the list of excluded maps, each with a Remove button.
- **Excluding a map**: mark a map so it always plays with your own settings, for example a ranked map you know by heart or one with a color scheme you dislike. You can do it in two places:
  - the **IE** button next to Play (struck out = excluded)
  - **Gameplay Setup → Mods → Intended Experience**, which also lists what the launch will change
- **Play button**: turns orange (only the hue changes) when the launch will apply intended settings.

## Requirements

- [BSIPA](https://github.com/nike4613/BeatSaber-IPA-Reloaded) ^4.3.0
- [BeatSaberMarkupLanguage](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) ^1.14.1
- [SiraUtil](https://github.com/Auros/SiraUtil) ^3.3.1
- [SongCore](https://github.com/Kylemc1413/SongCore) ^3.16.0

## Building

1. Create `IntendedExperience/IntendedExperience.csproj.user` that points at your Beat Saber install:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <Project>
     <PropertyGroup>
       <BeatSaberDir>C:/Path/To/Beat Saber</BeatSaberDir>
     </PropertyGroup>
   </Project>
   ```
2. Run `dotnet build`. [BSMT](https://github.com/Zingabopp/BeatSaberModdingTools) copies the plugin into `<BeatSaberDir>/Plugins`. A Release build also produces a zip.
