# Awl

A small local Windows desktop overlay in dark teal (#06231F) and pale mint (#E8FFE2), matching your wallpaper. Uses the Windows .NET Framework runtime; no package download, service, administrator rights, or Explorer modification.

## Controls

- **Start.cmd** starts the bars. Only one instance can run.
- Move the pointer to the **bottom center, just above the screen edge**, to reveal the dock. It hides approximately 0.7 seconds after you move away. The native taskbar is suppressed while Awl runs.
- Pin icon opens a vertical, icon-only menu of your existing taskbar shortcut pins. Hover an icon for its app name. Current entries: Discord, File Explorer, Modrinth App, Obsidian, Opera GX Browser, Visual Studio Code - Insiders. Pin/unpin apps through Windows to update this list.
- Single divider → visible app windows → double divider → 3×3 Start grid.
- Click an app icon to activate its window; minimized windows are restored. Multiple windows may have separate icons.
- Top bar: percentage and proportional charge fill inside battery outline; output-volume level; default microphone mute status; Wi-Fi; Bluetooth; notification bell. Click indicators for their Windows settings panels.
- Center: Lagos temperature in °C, local Windows date and time. Weather refreshes every 15 minutes, retrying after five minutes if offline.
- Right: title and progress only when the current Windows media session reports playing. Some browsers and players do not publish media sessions or duration.
- Right-click the dock or the Awl tray icon to exit and restore the original taskbar auto-hide setting.

## Startup and undo

Startup is enabled with a single **Awl.lnk** in your current user's Startup folder. Keep this folder in place while using startup.

**Restore.cmd** exits and restores the original taskbar setting for this session. It will start again at your next sign-in.

**Undo.cmd** also removes the startup shortcut, so it stays off after sign-in. Files remain here for reuse. **Enable-Startup.ps1** re-enables sign-in startup.

The initial taskbar auto-hide state is saved in `original-taskbar-state.txt`. It was already enabled on this PC. Exiting therefore preserves auto-hide. The native taskbar is shown again on exit. While Awl runs, use its Start grid or the Windows key for Start. A small companion process waits for Awl to exit and restores the native taskbar after a crash as well.

## Limits

This is a primary-monitor overlay, not a patched Windows taskbar. The native taskbar is explicitly hidden while the dock runs and checked every 200 ms in case Explorer reveals it again. The top bar hides whenever a visible, non-minimized application window intersects its upper 34 logical pixels, including maximized, snapped, and borderless windows. Hover at the top edge for about 0.35 seconds to reveal it temporarily; it hides again after you move away. It stays visible over the desktop. It does not reserve a permanent strip.

Bluetooth reports unavailable (—) when Windows does not expose the radio; it is not an assertion that Bluetooth is off. This PC's native and WinRT radio queries currently do not provide a radio state. Click it for Bluetooth settings.

Microphone status describes the default communications input endpoint, not whether a particular app is recording or has muted itself. The bell opens notifications; it does not inspect or count them. Taskbar pins represented only in Windows' private pin database, without shortcut files, will not appear in the launcher.

The dock is translucent with rounded corners. The top bar has a transparent background. No blur, visualizer animation, or browser runtime is used. The original main-process idle measurement was approximately 97 MB working memory and 0.6% of one CPU core. This revision adds a mostly idle recovery companion; usage varies with app windows and activity.

## Source and verification

`Awl.cs` is the full source. `Build.ps1` uses the existing .NET compiler and the installed Windows SDK 10.0.26100 metadata. No SDK or runtime was installed. The executable is approximately 40 KB.

`verification.json` records the local live-status check. The preview PNGs are renders of the actual UI controls, with transparent backgrounds; they are not desktop screenshots. Computer Use did not expose the overlay windows for screenshot interaction. The user confirmed that dock activation worked. The updated pinned-menu controls were rendered to pins-preview.png. verification.json checks live suppression and top-bar overlap. recovery-verification.json confirms the native taskbar returned after a simulated unexpected main-process exit.

Weather data: [Open-Meteo](https://open-meteo.com/), using its [forecast API](https://open-meteo.com/en/docs). Only the configured Lagos coordinates are sent, with standard HTTPS request metadata. [Microsoft taskbar auto-hide API](https://learn.microsoft.com/en-us/windows/win32/shell/abm-setstate) and [media-session API](https://devblogs.microsoft.com/oldnewthing/20231108-00/?p=108980) provide the Windows integration.

