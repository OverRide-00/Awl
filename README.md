# Awl

Awl is a customizable Windows shell with a standalone widget builder and an isolated development mode.

[Download the latest Awl.exe](https://github.com/OverRide-00/Awl/releases/latest/download/Awl.exe)

## Applications

- `dist/Awl.exe` — standalone end-user shell. It stores user state in `%LOCALAPPDATA%\Awl` and can migrate the prior `%LOCALAPPDATA%\TealShell` profile once.
- `dist/Awl.WidgetBuilder.exe` — standalone visual widget builder.
- `dist/Awl.DevMode.exe` — development shell using the same Awl UI and runtime with an isolated temporary profile. Its Developer tab builds `dist/Awl.exe` from the in-built templates kept in the Dev Mode library.

## Architecture

- `src/Awl.Core` — shell runtime, taskbar, start menu, widgets, capture, templates, settings, and Windows integration.
- `src/Awl.WidgetBuilder` — visual builder, logic graph, packaging, and desktop widget runtime.
- `src/Awl.DevMode` — isolated developer entry point and build controls.
- `assets` — SVGs, application icons, and embedded capture runtime.
- `templates` — source-controlled in-built, imported, public, and personal template folders plus the template catalog.
- `plugins` — Core, Visual, and combined plugin catalog sources, manifests, and authoring examples.
- `defaults` — clean fallback files used when no build profile is supplied.
- `build-profile` — clean default profile for command-line builds.
- `scripts` — build and launcher scripts.
- `dist` — standalone executables only.
- `artifacts` — archived legacy output, previews, and historical test files.

## Build

Run `Build.ps1` from the project root. It creates all three standalone executables in `dist`.

Dev Mode's **Build standalone Awl** button builds the normal `Awl.exe` with only the library's **Inbuilt Templates** embedded. Its first-run config comes from clean Awl defaults: no location, pins, active template, or enabled plugins. Each saved template stores its enabled plugin IDs; plugins required by an in-built template are included as inactive assets and activate when that template is applied. A missing required plugin is downloaded from the plugin catalog with visible progress when the template is applied. Dev Mode templates can be created, updated, and deleted in Config, and those library changes persist across Dev Mode sessions.

## Updates

Awl checks GitHub Releases for updates. The General tab lets users choose Release, Beta, or Alpha builds, review the release notes, and install a SHA-256-verified executable.

Dev Mode's **Push update to GitHub** action asks for a channel, semantic version, and `index.html` What's New file. It builds the current Dev Mode configuration, archives the version locally, and publishes a new GitHub Release without replacing older downloads.

Dev Mode's **Developer → Version switcher** lists published releases alongside the current development build. Select a release to read its notes and compare its source with local changes. **Test this version** verifies or downloads its executable, opens it with temporary Dev Mode settings, and returns to the current development build when the test closes. Downloaded versions stay cached in the Dev Mode library.

## Keyboard shortcuts

**General → Keyboard shortcuts** lets you record or clear Awl's global bindings. The Windows key remains fixed for opening Start; its app-search modifier defaults to Ctrl. Other defaults are Ctrl+Esc for app search, Ctrl+Alt+Space for Quick Search, Alt+Shift+R for Capture, Alt+Tab for Awl's app switcher, Ctrl+Win+W for Widget Builder, Ctrl+Alt+H for Hidden Mode, and Ctrl+Alt+Q to quit Awl. Hidden Mode hides both Awl bars until the shortcut is pressed again. The separate **Replace native Print Screen** toggle sends an unmodified Print Screen press to Awl Capture instead of Windows Snipping Tool; it is off by default.
