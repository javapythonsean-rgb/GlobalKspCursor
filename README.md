# Global KSP 2 Cursor v1.2.0

A Redux/SpaceWarp 2 mod that uses KSP 2's own `cursor_arrow@2x` cursor across the entire UI.

The arrow stays stock at rest, turns green while the left mouse button is held,
and grey while the right button is held. Left press wins if both are held.
Pressed textures are tinted in memory from the installed game's cursor; no
KSP 2 artwork is included in the download.

In the VAB's translate/rotate tool, KSP 2 keeps control of the cursor so the
tool's own manipulation cursor does not flicker against the global arrow.

The mod reads the cursor from the player's installed `oab_assets_all.bundle` at runtime. The download contains no KSP 2 artwork or other game assets, and it does not edit stock files.

Install Redux/SpaceWarp 2, then copy the `GlobalKspCursor` folder into `<KSP2>/mods/` while the game is closed. Restart KSP 2. To uninstall, remove that folder.

The code uses only SpaceWarp2's `MonoBehaviourMod` entry point and public Unity APIs; it has no Harmony patches or Redux internals. It does inspect the KSP 2 OAB prefab to find its cursor, so a future **KSP 2 asset-layout** change could require an update even though routine Redux updates should not.

Source build: run `./build.ps1` in PowerShell. The included MIT license applies to this mod's authored code, not to KSP 2's stock cursor or game assets.
