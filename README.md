# TRCC to InfoPanel Theme Converter

Converts [TRCC](https://github.com/emaspa/TRCCCAP) (Thermalright) LCD theme files (`config1.dc`) to [InfoPanel](https://github.com/habibrehmansg/infopanel) profile format (XML).

## What it does

TRCC stores themes as binary `.dc` files with sensor overlays, clocks, dates, and text labels positioned over a background image. This tool parses those files and generates InfoPanel-compatible XML profiles with the same layout, fonts, colors, and sensor bindings.

### Element mapping

| TRCC Element | InfoPanel Element | Notes |
|---|---|---|
| Sensor data (CPU/GPU/Memory/HDD/Network/Fan) | `SensorDisplayItem` | Placeholder LibreHardwareMonitor sensor IDs — reassign in InfoPanel to match your hardware |
| Time display | `ClockDisplayItem` | 12h/24h format preserved |
| Day of week | `CalendarDisplayItem` | `dddd` format |
| Date display | `CalendarDisplayItem` | Date format preserved (yyyy/MM/dd, dd/MM/yyyy, etc.) |
| Custom text | `TextDisplayItem` | |
| Image/icon | `ImageDisplayItem` | |
| Background (00.png, 01.png) | `ImageDisplayItem` | Copied to assets directory |

Font names (Chinese to English mapping), colors (ARGB), positions, bold/italic styles are all preserved.

## Requirements

- .NET 8.0 SDK or later

## Build

```bash
dotnet build TrccToInfoPanel
```

## Usage

```
TrccToInfoPanel <input> [options]
```

### Convert a single theme

```bash
# Point to a config1.dc file directly
TrccToInfoPanel C:\TRCCCAP\Data\USBLCD\Theme800480\Theme1\config1.dc

# Or point to a theme directory containing config1.dc
TrccToInfoPanel C:\TRCCCAP\Data\USBLCD\Theme800480\Theme1
```

### Convert all themes in a resolution folder

```bash
TrccToInfoPanel C:\TRCCCAP\Data\USBLCD\Theme800480 --all
```

### Batch convert everything

```bash
TrccToInfoPanel --batch C:\TRCCCAP\Data\USBLCD
```

### Inspect a theme without converting

```bash
TrccToInfoPanel C:\TRCCCAP\Data\USBLCD\Theme800480\Theme1 --dump
```

Example output:

```
  Canvas:     800x480
  SysInfo:    True
  Background: True
  Direction:  0°
  Elements:   7

  [0] Mode=4: Text: "CPU"
      Pos=(103,310) Font="微软雅黑" Size=36 Color=#FFFFFFFF
  [1] Mode=0: Sensor (main=0, sub=1)
      Pos=(103,370) Font="微软雅黑" Size=36 Color=#FFFFFFFF
  [2] Mode=0: Sensor (main=0, sub=2)
      Pos=(343,370) Font="微软雅黑" Size=36 Color=#FFFFFFFF
  ...
```

### Options

| Option | Description |
|---|---|
| `-o, --output <dir>` | Output directory (default: `./output`) |
| `--all` | Convert all Theme1-5 in a resolution folder |
| `--batch` | Convert everything under a USBLCD directory |
| `--dump` | Dump parsed theme info without converting |

## Output structure

The converter produces files matching InfoPanel's expected layout:

```
output/
├── profiles.xml                        # Profile metadata (resolution, name, colors)
├── profiles/
│   └── {guid}.xml                      # Display items (sensors, text, clocks, images)
└── assets/
    └── {guid}/
        ├── 00.png                      # Background image
        ├── 01.png                      # Alternate image
        └── Theme.png                   # Theme preview
```

To use the converted profiles, copy the output contents into `%LOCALAPPDATA%/InfoPanel/`.

## Supported resolutions

240x240, 240x320, 320x240, 320x320, 360x360, 480x480, 480x640, 480x800, 480x854, 640x172, 640x480, 800x480, 854x480, 960x320, 960x540, 540x960, 1280x480, 1600x720, 1920x440, 1920x462

## Limitations

- Only supports TRCC format version 221 (0xDD) — the current format. Legacy format 220 is not supported.
- Sensor IDs are placeholders (Intel CPU / NVIDIA GPU defaults). You must reassign sensors in InfoPanel to match your actual hardware.
- TRCC image elements that reference .NET assembly resources cannot be extracted automatically.

## License

MIT
