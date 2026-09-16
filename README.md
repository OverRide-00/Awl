# Awl

Awl is a customizable Windows shell with a standalone widget builder and an isolated development mode.

[Download the latest Awl.exe](https://github.com/OverRide-00/Awl/releases/latest/download/Awl.exe)

## Applications

- `dist/Awl.exe` — standalone end-user shell. It stores user state in `%LOCALAPPDATA%\Awl` and can migrate the prior `%LOCALAPPDATA%\TealShell` profile once.
- `dist/Awl.WidgetBuilder.exe` — standalone visual widget builder.
- `dist/Awl.DevMode.exe` — development shell. Every launch uses a unique temporary profile that is removed on exit. Its Developer tab can build `dist/Awl.exe` with the current temporary configuration and templates.

## Architecture

- `src/Awl.Core` — shell runtime, taskbar, start menu, widgets, capture, templates, settings, and Windows integration.
- `src/Awl.WidgetBuilder` — visual builder, logic graph, packaging, and desktop widget runtime.
- `src/Awl.DevMode` — isolated developer entry point and build controls.
- `assets` — SVGs, application icons, and embedded capture runtime.
- `templates` — source-controlled in-built, imported, public, and personal template folders.
- `defaults` — clean fallback files used when no build profile is supplied.
- `build-profile` — clean default profile for command-line builds.
- `scripts` — build and launcher scripts.
- `dist` — standalone executables only.
- `artifacts` — archived legacy output, previews, and historical test files.

## Build

Run `Build.ps1` from the project root. It creates all three standalone executables in `dist`.

Dev Mode's **Build standalone Awl** button builds only `Awl.exe`, embedding that running Dev Mode session's config, widgets, and in-built templates. Changes remain temporary unless explicitly captured by a build.

## Updates

Awl checks GitHub Releases for updates. The General tab lets users choose Release, Beta, or Alpha builds, review the release notes, and install a SHA-256-verified executable.

Dev Mode's **Push update to GitHub** action asks for a channel, semantic version, and `index.html` What's New file. It builds the current Dev Mode configuration, archives the version locally, and publishes a new GitHub Release without replacing older downloads.
