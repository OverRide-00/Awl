# Latest controls

## 2026-09-14 reliability and portability

- Release builds no longer embed the authoring machine's config, widget state, pins, app usage, startup preference, or weather location. The three built-in template archives were sanitized too.
- App discovery covers both Start menus, Windows App Paths, Desktop, and Downloads. Search ignores punctuation and separators, includes paths, ranks close name matches first, and includes a rescan button.
- App **Recent** and **Most used** orders now have deterministic fallback ordering when Awl has little launch history.
- Launcher redraws restore the active tab's scroll position. Switches, segmented options, and dropdown labels repaint immediately.
- Windows-key events are suppressed before Windows Start receives them; Windows shortcuts are replayed while a bare key toggles Awl with debounce protection.
- System sorting no longer restores stale rows after a filter click. A Awl usage card shows its CPU, memory, and disk I/O with an **Open Task Manager** button.

## 2026-09-12 release executables

- Double-clicking `Awl.exe` opens the custom menu. A second launch activates the running instance, while Windows startup uses `--background` to remain unobtrusive.
- Widget switches repaint immediately when clicked, including switches in expanded widget cards.
- `Awl.exe` embeds every ZIP in `Templates/Inbuilt Templates`; these presets are restored into the in-built section at runtime.
- Both executables embed all Widget Builder SVG icons and carry their own Windows file icon.
- A copied `Awl.exe` opens its Widget Builder from an internal process mode, so it does not require `WidgetBuilder.exe` beside it.
- App discovery and startup registration use built-in C# code rather than companion PowerShell scripts.
- Runtime preferences, templates, imports, and widget state live in `%LocalAppData%\Awl`, keeping the folder containing either EXE clean and writable without administrator elevation.
- The temporary **Save as in-built** button has been removed.
- `WidgetBuilder.exe` is a separate executable. The Widgets page and **Win + Ctrl + W** launch it, and adding a widget asks the running shell to reload the shared desktop-widget state.

- Run Open-Launcher.cmd in your normal Windows session to load the update.
- System popups use the dock background, border and rounded styling. Clicking outside dismisses them.
- Wi-Fi: radio on/off, scan, disconnect, signal strength, connected marker, saved-profile switching, new open/WPA2-Personal connections. New WPA2 passwords are passed directly to the Windows API using a temporary connection profile, not written to Awl files. Enterprise/WPA3 connections require an existing Windows profile. Hidden SSIDs are not listed. The first Wi-Fi adapter is used.
- Windows may require location consent for network scanning. Awl displays access failures; it does not change privacy settings.
- Connection requests are asynchronous; scan again to verify the connected marker. These live network operations have not been tested in this sandbox.
- Launcher opens with an upward slide and subtle bounce. Dock slides from its configured edge with a subtle bounce. Interface → Smooth pop animations disables these effects.
- Sidebar buttons now use pill corners.
- Native tray relocation remains unimplemented. No tray shortcut reveals the base taskbar. Exiting Awl still restores Windows' taskbar intentionally.

Build validation: compiler passed; WLAN_AVAILABLE_NETWORK is 628 bytes and WLAN_PHY_RADIO_STATE is 12 bytes on x64.

## Popup and layout fixes

- System popups are anchored 8 logical pixels below their originating icon. Wi-Fi refresh and reopening reuse that anchor rather than the pointer position.
- Clicking the same icon closes its popup. Switching icons replaces the popup.
- A temporary global mouse hook handles outside left, right and middle clicks, without consuming the click. It is removed when the popup closes. Events from an older popup cannot dismiss its replacement.
- Sidebar pill radius is exactly half the button height, producing semicircular ends instead of ovals.
- Sidebar and Start-menu icons are larger/bolder. Light and Dark icons are centered. Each of the nine palette buttons places its icon at the top left and its label at the bottom right.

Validation: compilation and 28 focused WPF checks passed (see popup-checks.txt), including mouse-hook registration, same-icon toggling, hit testing, stable repeated Wi-Fi coordinates and control geometry. Physical mouse-click delivery on the user's desktop remains unverified because Computer Use could not access the test window. The launcher preview is a render from the isolated UI test.

## Reference tab redesign
- Apps: one Pinned grid, then All apps with Name / Recent / Most used sorting. Right-click an app to pin/unpin. Usage statistics count launches through Awl only. The secondary line shows the available launch target, not an invented installation directory.
- General: Startup & session, Language & region, Updates, Notifications, Backup & reset cards. Export/import/reset are connected to the local configuration. No update service/version is fabricated.
- Bars: module visibility, side placement and drag ordering; top-bar height/radius; taskbar size, spacing, alignment and labels. The top bar remains fully transparent, so its radius does not create a visible filled background. Multiple-monitor mirroring is unavailable.
- Interface: Motion, Windows, Cursor, Sound & accessibility sections. System-wide cursor styling, window effects, notification delivery, session restoration and update services are visibly disabled.
- Build and 38 isolated popup/layout checks passed. Production startup generated diagnostics successfully; Windows media services are unavailable under the sandbox account. Live network changes were not tested.
- Load the new build in your Windows session using Open-Launcher.cmd.

## Apps and Quick follow-up
- Apps search filters names and launch targets. Paths display 24 characters followed by ...; full targets remain in tooltips.
- App context menus show Pin to taskbar first. Pin changes save immediately and refresh an open pinned menu. Context menus are protected from outside-click dismissal.
- General > Weather location searches cities using Open-Meteo/GeoNames. Choose a result to save its coordinates and refresh the temperature. Lagos remains the default.
- Quick uses a sliders icon and four card backgrounds for Bar & Screen.
- Build and eight focused checks passed, including search, pin persistence and immediate menu refresh. Live weather search was not tested in the sandbox.

## System and shell controls
- System: native CPU/memory/system-drive readings, real history, top-bar toggles, sortable running windowed processes and normal app close. Per-process I/O is transfer activity, not installed disk space. Protected processes may be inaccessible.
- Six tabs use vector icons. Settings/About PC share the footer; Power offers Sleep, Restart and Shut down with confirmation. Power operations were not executed during tests.
- Apps search receives focus on opening. Top-bar buttons have rectangular nonzero-alpha hit areas. TopItemSpacing separates the items. QuickOptionIconSize controls larger Quick option icons.
- ReplaceWindowsStart defaults to true: bare Windows key and Ctrl+Esc open Awl while it runs. Windows-key chords and Ctrl+Shift+Esc remain available. A fallback redirects the native Start window. Set the preference to false to disable interception.
- Supported defaults are now included in config.json, preserving existing settings. Previous file: config.json.before-system-controls.bak. AppSort, SystemSort, BarSettingsTab and LaunchOnSignIn also persist.
- Build and 14 focused System/control checks passed. Physical keyboard behavior and actual power operations were not tested in the normal Windows session.

## Date & Time widget, pin overflow and glass
- Pinned taskbar strip shows at most six apps. If there are more, its first button is an ellipsis that opens a centered, scrollable grid of every pin.
- Sidebar tabs are slimmer (42px). Widgets contains only Date & Time; no other widgets or import functionality is implemented yet.
- Date & Time editor: Background, Time text, Date text and automatic Sun/Moon layers; per-layer position, visibility, font, size, width, weight, alignment and color. Widget position/size, opacity, corners, border, formats and seconds persist under DateTimeWidget in config.json.
- Drag the desktop widget to move it. Double-click or right-click to edit. In the editor, drag text/icon layers to reposition them, drag the background to change screen position, and drag the corner handle to resize. Done saves and returns it to the desktop. Sun runs from 06:00 through 17:59 local time, moon otherwise.
- The widget hides on intersection with an ordinary visible app window and returns when clear; it does not activate when shown. Coverage scan excludes minimized/cloaked windows, desktop/taskbar surfaces, tool windows and Awl windows. Physical multi-monitor placement/coverage has not been verified in the normal user session.
- Quick > Wallpaper colors shows the ten sampled colors as miniature taskbar presets with coverage/hex tooltips. Selecting a preset applies that color and contrasting icon text.
- Quick > Wallpaper colors > Glass effect controls translucent gradient highlights on Start, the taskbar and the widget. This is lightweight simulated glass, not native backdrop blur. GlassEffect and SelectedWallpaperColor are saved in config.json. Widget opacity remains independently adjustable.
- Build and 19 isolated widget/preset checks passed. These cover overflow counts, editor layers, resizing, saved dimensions, coverage-scan execution, ten presets and glass on/off. Previous config: config.json.before-widgets-glass.bak.

## Color picker and independent materials
- All editable color values now use current-color swatch boxes. Clicking opens a shared picker with wallpaper presets, saved custom presets, an HSV color wheel, brightness and #RRGGBB entry. Use color applies; closing cancels. Save preset persists a reusable color.
- Quick > Wallpaper colors has separate glass toggles for Start menu, taskbar, pinned bar, pinned grid and Date & Time. The widget editor also exposes its own glass toggle. Pinned menus share the taskbar's base color, opacity and transparency while keeping independent glass settings.
- Widget Time, Date and Icon layers each have Color schedule: Fixed, Day / night, or 6-hour slots. Day is 06:00–18:00 local time. Detailed slots are 00:00–06:00, 06:00–12:00, 12:00–18:00 and 18:00–24:00. Foreground colors update on live ticks and in the preview.
- Config uses StartMenuGlass, TaskbarGlass, PinnedBarGlass, PinnedGridGlass, DateTimeWidget.Glass, CustomColors and per-layer ColorMode/DayColor/NightColor/SlotColors. Legacy GlassEffect only supplies migration defaults; there is no global glass toggle in the UI.
- Existing colors were preserved. Previous config: config.json.before-independent-colors.bak.
- Build passed; 21 color checks and the 19 widget/preset regression checks passed. Color checks cover picker layout/cancel, wheel conversion and every day/night/six-hour boundary.

## Anchors, layout templates, snapping, movement lock and Apps performance
- Each layer supports Free placement or nine anchors with signed X/Y offsets. The layer width and font line-box height determine its anchored alignment. Existing layers retain Free positioning. Anchor coordinates update when the widget is resized.
- Widgets > Date & Time includes Lock movement, Snap to grid, grid spacing, and named layout templates. Templates are full independent copies of widget settings, persisted in WidgetTemplates. Loading is disabled while locked. Unlock from the Widgets tab before moving/editing.
- Desktop drag shows a dot grid and snaps to its points; editor background dragging also snaps the saved screen position. Grid origin is the virtual desktop origin.
- Apps search waits 160ms after typing and builds at most twenty result rows, with page navigation for the rest. This avoids rebuilding the full catalog and queuing every icon on each keystroke. Profile photo lookup is cached after its first read.
- Build and 24 widget/layout checks passed. Search filtering, input retention and the twenty-row cap passed. Normal-session performance and physical drag behavior remain unmeasured. The older pin test assumes all pins appear vertically and is incompatible with the six-pin strip.
- Config backup: config.json.before-layouts.bak.

## Recents and sign-in startup
- Repaired the user's Awl.lnk startup shortcut: it had no executable target. Verified its target is this Awl.exe. LaunchOnSignIn is true. Windows launches it after sign-in following power-on/restart.
- Widgets > Recents provides Grid, Vertical and Horizontal layouts; cell width/height; grid columns/visible rows; maximum recent count; combined or separate app/file sections; Icon only, Icon & name, or Icon/name/path display; configurable path character limit.
- Recents has independent glass, opacity, colors, movement lock and cover-to-hide. Drag its Recents heading to position it. Large collections scroll within the configured viewport. Maximum count applies across apps and files together.
- Files come from existing Windows Recent Items shortcuts, refreshed on a background STA thread every 30 seconds. Missing files are skipped. This depends on Windows recent-item history being enabled. Apps are recorded from foreground changes and Awl launches while running; prior unobserved app history is unavailable.
- Settings and observed app history persist in config.json under Recents and RecentApps. Config backup: config.json.before-recents.bak.
- Build and seven isolated Recents checks passed: ordering, count limits, all layouts, path truncation and focus/taskbar behavior. Live Windows file-history population and coverage behavior were not verified in this sandbox.

## Recents editor update
- Widgets > Recent now exposes only Source (Files / Apps / Both), Max items, enable and expand controls. The pencil opens the dedicated Recents editor.
- The editor provides Layout, Appearance and Position panels, with a live preview. Cell dimensions, grid dimensions, display details, path limit, split mode, glass, colors, opacity, screen position and movement lock are configured there.
- Split mode creates two independent layout containers within the widget: two grids, two vertical lists or two horizontal rows. Source filtering happens before the maximum count is applied.
- Build and all 11 isolated Recents checks passed. The editor preview was rendered and visually inspected. Live file-history population remains dependent on Windows Recent Items.

## Recents resizing and layers
- Recents editor now has a bottom-right widget resize handle and exact Width/Height under Position. Existing automatic dimensions remain until explicitly resized.
- Select Title, Apps, Files, Items (combined mode), Icon, Name or Path to drag that layer or enter X/Y offsets. Icon/name/path offsets apply consistently to every recent tile; split sections move independently.
- Icon size is configurable from Layout or Icon (12–128 px). Larger icons may require a taller row size.
- Width, height, icon size and layer offsets persist under Recents in config.json. Movement lock blocks resize and drag.
- Build and 16 isolated Recents checks passed, including resize events, icon rendering size, layer drag offsets and lock behavior. Physical mouse use in the user's normal desktop session was not tested.

## Shared widget editor behavior
- Date & Time and Recents use a shared editor frame and collapsible Layers list. Both widget cards start collapsed. A movement lock no longer blocks opening Date & Time's editor.
- Recents layers support direct mouse selection, live transforms while dragging, layer resize handles, dimensions, anchors, foreground schedules and wallpaper palette references. Date & Time gains explicit layer height and layer resize handles.
- Screen coordinates were removed from editor properties. Preview movement does not change desktop placement. Move Recents by its desktop heading with the shared snap grid; lock from Widgets. Date & Time retains desktop snap dragging.
- Background/border palette links are opt-in; individual text layers can select Wallpaper color mode and a palette index. References resolve against the current ten-color wallpaper palette instead of freezing a hex value.
- Build, 24 Date & Time checks and 16 Recents checks pass. Checks simulate drag events and do not prove physical mouse behavior in the user's desktop session. Recents anchors use the layer's containing section/cell; icon/name/path styles repeat across tiles.

## Preview drag lock fix
- Confirmed both live widget configurations had Locked=true. Editor gesture handlers incorrectly interpreted desktop movement lock as an editing lock. Removed that gate from both editors; desktop drag lock is preserved.
- Recents resize updates the preview content dimensions during the gesture. Removed the editor window resize grip to distinguish widget resizing from outer window resizing.
- Build and widget regression checks passed, including preview resize with Locked=true. No user configuration values were changed.

## Power and wallpaper playback
- Power uses a rounded themed Popup. Sleep/restart/shutdown no longer ask for confirmation. Power actions were not executed during validation.
- Quick > Set wallpaper accepts images and video. Images are decoded and converted to BMP for the Windows desktop. Videos use a muted looping WPF MediaElement parented to the desktop WorkerW behind icons. Video codec availability depends on Windows.
- Wallpaper Engine receives its supported -control stop command after replacement readiness. The selected path persists for sign-in startup. This stops playback, not Wallpaper Engine startup configuration.
- Compilation and Date & Time regressions passed. Live desktop parenting, codec playback and Wallpaper Engine switching require normal-session verification. No user's wallpaper was changed during tests.

## Widget Builder interaction fix
- Added an always-visible × close button; Escape also closes the builder. The top bar now uses compact icon controls with explanatory tooltips and accessible names.
- Corrected canvas event routing so a child receives the click before its parent. Clicking a nested layer now selects that exact node, refreshes its properties, and reveals its resize handle.
- Added a one-click project reset. Reset creates a clean versioned widget project and remains undoable during the current builder session.
- Background, foreground, border and other color fields now open Awl's custom color menu with wallpaper presets, saved presets, HSV wheel, brightness, hex input, preview, and preset saving.
- Build and 29 builder checks pass, including synthetic nested-layer selection, close/reset controls and custom-picker wiring.

## Visual Widget Builder MVP
- Widgets now includes **Open Widget Builder**. It opens a separate resizable window; **Win + Ctrl + W** opens or focuses the same builder from anywhere while Awl is running.
- UI, Logic and Prefab workspaces share one versioned project. The UI canvas supports drag-to-add primitives, live selection, 10 px movement snapping, bottom-right resizing, property literals or variable bindings, alignment, and drag-to-reparent layers.
- Logic includes searchable, category-colored event/access/control/UI/variable/operator blocks, movable graph cards, sequence links, editable parameters, variables, a live console and a small non-blocking mock interpreter.
- Prefabs round-trip as reusable subtrees with exposed parameters. Row, column and grid nodes support list-bound repeated children through a prefab template.
- Save/Open preserve `widget-builder.json`; Export validates unresolved variable bindings, missing prefab references, duplicate node ids and orphaned event links before writing a portable versioned widget JSON file.
- Examples includes Recent apps dock, Clipboard hover panel, Calendar and System monitor starter projects. The examples use editable nodes, variables, prefabs and mock graphs rather than screenshots.
- The isolated builder fixture passed 24 checks, including separate-window launch, hotkey dispatch, binding resolution, logic execution, undo/redo, prefab flow, live resize, JSON round-trip and repeated prefab rendering. Full Blockly-style value sockets, multi-select/distribute and real Windows access blocks remain later runtime work.

## Widget Builder icons, inspectors and connected logic
- Replaced text and placeholder controls throughout the builder with local SVG assets for the top bar, workspace rail, component palette, layer tree, property tabs, alignment tools, logic blocks, prefab actions and destructive controls. The renderer reads the SVG path data without an added runtime dependency.
- The property inspector is divided into Layout, Style, Content and Data tabs. Known enum values, UI targets, variables, prefabs and property names use icon-labelled dropdowns instead of free-form text entry.
- Dropdowns use a dark rounded popup, accent border, readable hover state and an icon-labelled selected value. Variables can be removed with their trash control.
- Delete removes the selected canvas layer or logic block while leaving text and dropdown editing intact. Escape closes the builder.
- Logic blocks now form connected vertical flows. Sequence blocks accept a Next child, control blocks accept an indented Body child, and dropping a block into either slot reparents it while preventing circular graphs.
- Typed reporter blocks such as Time can be dragged into value sockets such as Set variable. Connected values resolve live in the preview runtime.
- The production build and 39 focused builder checks pass. The rendered fixture verifies the SVGs, connected flow and selected dropdown content.

## Live builder data and Wallpaper Engine return
- Set property refreshes its available properties as soon as its target UI element changes. A text element exposes **Text** in the menu, stored as its `content` property.
- Link buttons now open a themed source menu containing Literal value, variables and compatible standalone data blocks. Set variable excludes its own destination variable from this menu, preventing an accidental self-reference.
- UI properties can bind directly to Time, CPU usage, Running apps, Get variable and operator reporter blocks. Bound text refreshes on the canvas every second without requiring an event connection.
- Data reporter cards remain standalone and show their current output. Time uses the local clock, CPU uses Windows system counters and Running apps uses visible Windows processes.
- Every logic block has a concise usage description in its tooltip and selected-block inspector. Get variable is a reporter: choose its variable, then drag the block into a value socket.
- When Awl is supplying an image or video wallpaper, Quick shows **Return to Wallpaper Engine**. Awl detects a running or installed Wallpaper Engine executable, restores playback and stops its own video host. If Wallpaper Engine is unavailable, the action is disabled with a `Requires Wallpaper Engine` tooltip.
- The production build and 44 focused builder/wallpaper checks pass.

## Deployed widget logic runtime
- Deployed Widget Builder projects now keep an independent runtime instead of recreating variables from their saved defaults on every desktop redraw.
- The desktop host executes `When loaded` once and schedules each `Every interval` flow using its Seconds value. Sequence links, Set variable, Set property, Clear children, Add child, If, For each and While/interval execute in deployed widgets.
- Desktop value resolution supports Time, CPU usage, Running apps, Get variable, Add and Compare reporter blocks. Direct UI-property links to reporter blocks also remain live.
- The desktop surface is rebuilt from the updated runtime every second. A Time → Set variable → Set Text flow with a one-second interval was tested across two ticks and its displayed value advances.
- The production build and 47 focused checks pass, including the exact deployed-clock regression.

## Data formatting, progress components and widget packages
- Time data blocks now expose format presets for 24-hour/12-hour clocks, with or without seconds, plus a combined date/time preset and Custom. Existing Time blocks gain these controls when selected.
- Custom time formats use Awl tokens: D day, H 24-hour, h 12-hour, M minute, S second, A/a meridiem, N month, Y year and W weekday. Repeating a token pads or expands it. The help icon opens a complete token guide with examples.
- Set property's property list is rebuilt from the selected component's real property schema. Changing the target immediately refreshes the list; text, progress, layout and visual components expose their applicable fields.
- Progress bar components support line, ring, arc and dashes. Their schema includes value, maximum, thickness, dash count, track color and style-specific direction: horizontal/vertical for lines and clockwise/counterclockwise for circular styles. Editor and desktop hosts use the same renderer.
- Export now creates a `.widget.zip` package with `manifest.json`, `widget.json`, `properties.json`, `widget.css` and an `assets` folder. Referenced local assets are copied into the package and rewritten to portable paths.
- Widgets includes Import widget. Import validates the package, safely extracts assets under `ImportedWidgets`, rewrites their paths, and opens the imported project in Widget Builder for review and desktop deployment.
- The production build and 55 focused checks pass, covering custom time casing, format presets, every progress style and the required package entries.

## Config templates
- Config is now a full sidebar tab. The adjacent pen button opens `config.json` directly.
- The template library is split into My templates, Imported templates and Public templates. Saved templates go to My templates; imported ZIPs go to Imported templates; locally supplied public packs are read from the Public templates folder.
- Saving asks for a name and thumbnail image. The image is center-cropped to a consistent 16:9 card preview; when no image is chosen, Awl captures the desktop with the Quick tab, widgets, top bar and taskbar visible.
- Template ZIPs contain the complete preference JSON, enabled built-in widgets, custom builder widget deployments, builder project, palette, referenced widget assets and the wallpaper. Wallpaper Engine templates store the workshop link/project metadata instead of copying the wallpaper.
- Wallpaper Engine templates display the installed app icon as a badge. They are dimmed and disabled with a `requires wallpaper engine` tooltip when Wallpaper Engine is unavailable.
- Every template card has a three-dot menu for editing its name or thumbnail, exporting its ZIP and deleting it. Active state follows a renamed template.
- Applying a template restores portable widget assets and reloads the shell, Recents, Date & Time and every custom desktop widget through the same reload path as Quick.
- The production build and 8 template-package checks pass, covering thumbnail normalization, package structure, portable wallpaper metadata, independent glass settings and enabled-widget state.
- Fixed template cards showing a flat color even though their ZIP contained a valid thumbnail. Card images now load through an extracted local cache, which is refreshed after thumbnail edits and removed with deleted templates.
- Fixed template switching leaving Quick's wallpaper preview, sampled palette and glass surfaces on the previous template. Wallpaper Engine templates now resolve the selected project's preview immediately, then resample and redraw the shell several times while Wallpaper Engine completes its asynchronous switch. The final refresh returns wallpaper detection to the live Wallpaper Engine configuration.
- Added a separate `Templates/Inbuilt Templates` library and an In-built templates section in Config. It is intentionally isolated from My, Imported and Public templates so preset ZIPs can be bundled into the future single-executable build.
- Added the temporary **Save as in-built** authoring button. It uses the normal name/thumbnail flow but writes the complete preset ZIP directly to `Templates/Inbuilt Templates`.
- Quick reload now prioritizes Awl's own image or video wallpaper, re-enables automatic colors, clears the previous fixed swatch, resamples the ten-color palette and redraws all themed surfaces. Video wallpapers use a cached Windows Shell thumbnail for both palette extraction and the Quick preview instead of falling back to Wallpaper Engine's previous project.
# 2026-09-14 System usage visibility and Widget Builder detection

- The running-app list now grows with its visible rows up to a capped height, keeping the Awl CPU, memory, disk-I/O, and **Open Task Manager** section visible directly below it.
- The Widgets page only shows **Open Widget Builder** when `WidgetBuilder.exe` is installed beside `Awl.exe`.
- **Win + Ctrl + W** uses the same companion-executable check and launches that executable directly.
# 2026-09-14 launcher search and System gauges

- Portable app discovery now scans likely app folders across fixed drives in the background and caches the result. It detects the local portable Godot 4.7.2 executable and Steam installation/shortcuts.
- Holding **Ctrl** while opening Start, including **Ctrl + Windows**, opens the Apps tab and focuses its search box. Existing Windows-key shortcuts continue to pass through.
- Holding **Alt** while typing in Apps search switches to Windows Settings search. **Enter** opens the first visible match in either search mode.
- Page headers now use the same vector icon as their sidebar tab, including the Bars page header.
- Awl's CPU, memory, and disk-I/O readings now use live ring gauges above the Task Manager shortcut.
- The expanded pinned-app grid now hides first, launches the selected app on the next UI dispatch, and then disposes its popup window. UI-handler exceptions are logged without terminating the shell.
# 2026-09-14 top workspace, system popups, switcher, and notifications

- Revealing the auto-hidden top bar temporarily reserves its height for ordinary maximized windows, then releases it when toolbar focus ends. Borderless/fullscreen windows remain in place and receive a solid black toolbar backdrop.
- WPF tooltips now use the current Awl palette, rounded corners, border, padding, and shadow.
- Volume, microphone, and battery popups use compact icon/slider/value layouts. Wi-Fi and Bluetooth use the same rounded popup surface.
- Alt+Tab is now rendered by Awl as a centered horizontal app switcher with active selection styling and app icons.
- Notifications use Windows notification history when access is available, grouped into Today/Earlier and by app, with inline update actions and Focus, Wi-Fi, and Bluetooth quick controls.
