# TeleZone — CS2 CounterStrikeSharp Plugin

A standalone CS2 plugin that lets admins define **teleporter zones** and **kill zones** on any map. All zones persist across rounds and map restarts via JSON files.

> No dependencies on SharpTimer or any other plugin.
> Created by **Torment**

---

## Features

- **Teleporter zones** — define a box (Zone A) and a destination point (Zone B). Any player who walks into Zone A is instantly teleported to Zone B with the saved orientation.
- **Kill zones** — place a spherical zone at any position. Any player who enters it dies instantly.
- **Per-map persistence** — zones are saved to JSON files and loaded automatically on every map start.
- **Multiple zones per map** — unlimited teleporter pairs and kill zones per map.
- **In-game admin tool** — numbered center-screen menu; no freezing, no external config needed.
- **Live wireframe preview** — cyan beam box shown while placing a teleporter zone.

---

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) **≥ 1.0.369**
- .NET 10 SDK (only needed to compile from source)

---

## Installation

1. Download `TeleZone.dll` from the [latest release](../../releases/latest).
2. Place it inside your server at:
   ```
   csgo/addons/counterstrikesharp/plugins/TeleZone/TeleZone.dll
   ```
3. Restart the server or run `css_plugins reload` in the server console.

Zone data files are created automatically at:
```
csgo/cfg/TeleZone/MapData/<mapname>.json
```

---

## Permissions

Both commands require the `@css/cheats` flag.

---

## Commands

### `!telezones` — Teleporter zone menu

Opens the center-screen menu. Type the option number in chat to select. Type `0` to close.

```
TELEZONE TOOL
1. Marcar Zona A (esquina 1/2)
2. Fijar Destino B (pos + angulo)
3. Guardar par
4. Listar pares
5. Eliminar par...
6. Recargar zonas
```

**Workflow to create a teleporter:**

| Step | Action |
|------|--------|
| 1 | Stand at one corner of the entry zone → `!telezones` → option **1** |
| 2 | Walk to the opposite corner (cyan wireframe follows you) → `!telezones` → option **1** again |
| 3 | Move to the destination point and face the desired direction → `!telezones` → option **2** |
| 4 | `!telezones` → option **3** to save — the pair is active immediately |

---

### `!killzones` — Kill zone menu

```
KILLZONE TOOL
1. Añadir zona (radio 50)
2. Añadir zona (radio 100)
3. Añadir zona (radio 200)
4. Listar zonas
5. Eliminar zona...
```

Stand at the center of where you want the kill zone, open `!killzones`, and pick a radius. The zone activates instantly.

---

## Data format

All zones for a map are stored in a single JSON file:

```json
{
  "Pairs": [
    {
      "Id": 1,
      "SourceC1": "100.0 200.0 -128.0",
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

You can edit this file manually and use **option 6** (`Recargar zonas`) to apply changes without restarting.

---

## Building from source

```bash
git clone https://github.com/josesilvaruiz/TeleZone.git
cd TeleZone
dotnet build -c Release
```

The compiled DLL will be in `bin/Release/TeleZone.dll`.

---

## Author

Made by **Torment**

## License

MIT
