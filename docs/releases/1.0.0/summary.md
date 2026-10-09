# Awl 1.0.0 — Make Awl your own

[Download the screenshot-based What's New page](https://github.com/OverRide-00/Awl/releases/download/v1.0.0/index.html) and open `index.html` in a browser. The page works offline and shows real screenshots of the plugin browser, templates, colors, shortcuts, and bar controls.

## New additions since 0.2.1

- **Plugin system:** Install, browse, enable, update, and remove Core and Visual plugins. Core plugins can add actions, tabs, toolbar modules, services, and Quick Search calculators. Visual plugins can restyle Awl and supply motion profiles.
- **Quick Search:** Find apps, files, and settings; calculate expressions; convert units and currency; and open file conversion and compression tools. Default shortcut: Ctrl+Alt+Space.
- **Capture:** Redesigned compact bar for region, window, and full-screen screenshots and video. Recording controls show time and quality, with pause/resume, review/export, and an optional last-30-seconds replay buffer.
- **Customization:** Import icon packs, choose icon styles, optionally replace application icons, and edit individual component colors.
- **Motion and layout:** Motion API, visual motion profiles, animated taskbar ordering, and Ratio layout service.
- **Templates and Dev Mode:** Browse template groups, preserve Dev Mode templates, include template-required plugin assets in builds, and switch between verified published versions for testing.
- **Controls and tools:** Configurable global shortcuts, Hidden Mode, optional Print Screen capture, and Quick Search file tools.
- **App and window controls:** Open app locations, launch registered uninstallers, and adjust Awl surface opacity, scale, corners, shadows, snapping, and cursor style.
- **Wallpaper handoff:** Return to Wallpaper Engine and restore the selected project after startup when available.

## Bug fixes and refinements

- Recording frames and the elapsed-time display use a shared clock, including pause and resume.
- The replay buffer is opt-in and stops when disabled.
- Window effects and taskbar tracking are cleaned up when Awl exits.
- Template builds carry required plugins without enabling them for every new user.
- Interrupted or disabled visual effects restore controls to their normal state.
- Awl waits for its video wallpaper to be ready before stopping Wallpaper Engine, and resumes Wallpaper Engine on playback failure.
- Release publishing now handles UTF-8 metadata, large asset uploads, and retries without closing Dev Mode.
