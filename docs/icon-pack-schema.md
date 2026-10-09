# Icon pack schema

All built-in Awl icons have stable string IDs. A pack maps IDs in `icons.json` under `Icons`; IDs are never numeric.

Common shell IDs include `startmenu-sidebar-config`, `startmenu-sidebar-quick`, `startmenu-sidebar-widgets`, `startmenu-sidebar-apps`, `startmenu-sidebar-general`, `startmenu-sidebar-system`, `startmenu-sidebar-bars`, `startmenu-sidebar-interface`, `startmenu-sidebar-settings`, `startmenu-sidebar-about`, and `startmenu-power-main`.

Interface card IDs include `startmenu-interface-backgrounds-main`, `startmenu-interface-motion-main`, `startmenu-interface-windows-main`, `startmenu-interface-cursor-main`, and `startmenu-interface-accessibility-main`.

The SVG component library also exposes semantic IDs such as `camera`, `record`, `screenshot`, `play`, `pause`, `stop`, `download`, `search`, `cog`, `wrench`, `history`, `ratio`, `region`, `window`, `fullscreen`, `microphone`, `battery`, `slider`, `progress`, `database`, `sun`, `moon`, `style`, `grid`, `save`, and `external-link`.

For `Apps`, keys may be an executable name, a filename, or an app name such as `discord`. Application mappings are only consulted when **Also replace application icons** is enabled.
