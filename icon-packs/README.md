# Awl icon packs

An icon pack is a `.iconpack` ZIP. Its root contains `icon.png`, `icons.json`, an `Icons/` directory, and optional `0.png` and `1.png` previews.

`icons.json` has `Icons` for Awl UI IDs and `Apps` for application file/name matches. Example:

```json
{
  "id": "nova-line",
  "name": "Nova Line Icons",
  "Icons": {
    "startmenu-interface-motion-main": "Icons/new_main_motion_icon.svg"
  },
  "Apps": {
    "discord": "Icons/discord.png"
  }
}
```

SVG UI assets scale to their assigned Awl surface. PNG UI assets retain their natural pixels and only participate in Awl GUI scaling. App replacements are applied only when the user enables **Also replace application icons**.
