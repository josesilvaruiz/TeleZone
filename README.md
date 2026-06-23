# TeleZone — CS2 CounterStrikeSharp Plugin

A standalone CS2 plugin that lets admins define **teleporter zones** and **kill zones** on any map. All zones persist across rounds and map restarts via JSON files saved per map.

> No dependencies on SharpTimer or any other plugin.  
> Created by **Torment**

---

## Features

- **Teleporter zones** — define a rectangular area (Zone A) and a destination point (Zone B). Any player who steps into Zone A is instantly teleported to Zone B facing the saved direction.
- **Kill zones** — place a spherical zone anywhere. Any player who enters it dies instantly.
- **Per-map persistence** — zones are saved to JSON files and loaded automatically on every map start.
- **Unlimited zones** — as many teleporter pairs and kill zones per map as you need.
- **Noclip-friendly placement** — teleporter zones use 2D detection (X,Y only), so you can mark corners from noclip at any height and they will still catch players on the ground.
- **Live wireframe preview** — cyan beam box shown while marking the second corner of a teleporter zone.
- **Root-only** — both commands require `@css/root`.

---

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) **≥ 1.0.369**

---

## Installation

1. Download **`TeleZone.zip`** from the [latest release](../../releases/latest).
2. Extract and place the `TeleZone/` folder inside:
   ```
   game/csgo/addons/counterstrikesharp/plugins/
   ```
   Result:
   ```
   game/csgo/addons/counterstrikesharp/plugins/TeleZone/TeleZone.dll
   ```
3. Restart the server or run `css_plugins reload` in the server console.

Zone data is saved automatically at:
```
game/csgo/cfg/TeleZone/MapData/<mapname>.json
```

---

## Permissions

Both commands require the **`@css/root`** flag (server root admin).

---

## Commands

### `!css_telezones` — Teleporter zone admin menu

```
━━━━ TELEZONE TOOL ━━━━
!1  Mark Zone A (corner 1/2)
!2  Set Destination B (pos+angle)
!3  Save pair
!4  List pairs
!5  Remove pair...
!6  Reload zones
!0  Close
```

**How to create a teleporter:**

| Step | What to do |
|------|-----------|
| 1 | Go to one corner of the entry area (works from noclip). Type `!css_telezones` → `!1` |
| 2 | Walk/fly to the opposite corner. Type `!css_telezones` → `!1` again |
| 3 | Move to the destination point and face the desired direction. Type `!css_telezones` → `!2` |
| 4 | Type `!css_telezones` → `!3` to save. The pair is active immediately, no restart needed |

---

### `!css_killzones` — Kill zone admin menu

```
━━━━ KILLZONE TOOL ━━━━
!1  Add kill zone (radius 50)
!2  Add kill zone (radius 100)
!3  Add kill zone (radius 200)
!4  List kill zones
!5  Remove kill zone...
!0  Close
```

Stand at the center of where you want the kill zone, open `!css_killzones`, pick a radius. Active immediately.

---

## Data format

```json
{
  "Pairs": [
    {
      "Id": 1,
      "SourceC1": "100.0 200.0 0.0",
      "SourceC2": "300.0 400.0 0.0",
      "DestPos":  "1500.0 200.0 -64.0",
      "DestAng":  "0 90 0"
    }
  ],
  "KillZones": [
    {
      "Id": 1,
      "Center": "500.0 300.0 -64.0",
      "Radius": 50.0
    }
  ]
}
```

You can edit these files manually and use **option 6** (`!6 Reload zones`) to apply changes without restarting.

---

## Building from source

```bash
git clone https://github.com/josesilvaruiz/TeleZone.git
cd TeleZone
dotnet build -c Release
```

DLL output: `bin/Release/TeleZone.dll`

---

## Author

Made by **Torment**

## License

MIT
