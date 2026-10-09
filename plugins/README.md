# Awl plugin repository

This folder is the source catalog for Awl plugins. Packages use the `.zip` or `.awlplugin` extension and are ZIP archives with `manifest.json` at the archive root. Every published package also carries `icon.png` plus optional sequential preview images (`0.png`, `1.png`, and so on). The Awl browser reads these directly from the package.

- `Core/` — DLL or script based plugins that can add tabs, actions, toolbar buttons, calculations, and other behavior.
- `Visual/` — data only plugins that modify Awl design tokens through `style.css` and optional procedural motion through `motion.json`.
- `Both/` — packages containing Core and Visual extensions.
- `catalog.json` — searchable metadata used by the future in-app catalog.
- `examples/` — unpacked authoring examples. Run `scripts/Pack-Plugin.ps1` to package one.

Core plugins execute code with the same Windows permissions as Awl. Visual plugins never load an assembly.

## Manifest

```json
{
  "format": "Awl Plugin",
  "manifestVersion": 1,
  "id": "author.plugin-name",
  "name": "Plugin name",
  "version": "1.0.0",
  "type": "Core",
  "entryAssembly": "Plugin.dll",
  "entryType": "Example.Plugin",
  "style": "style.css",
  "categories": ["Productivity"],
  "tags": ["search", "tools"]
}
```

Core DLLs implement `Awl.IAwlCorePlugin`. The public host supports tabs, toolbar actions, Config actions, and Quick Search calculators. Script-only Core plugins can declare manifest `actions` whose target is a file inside the package. PowerShell scripts are launched with `-NoProfile`.

Visual CSS currently supports these custom properties: `--background`, `--text`, `--accent`, `--panel`, `--card`, `--muted`, `--card-radius`, and `--card-padding`.
