# Awl Motion Showcase

A Visual plugin based on `motion-showcase.html`, using **Awl Motion API** for frame scheduling and procedural curves. It contains no executable code, actions, or new tabs.

Install and enable Awl Motion API first, then install this package from Config. It can be enabled or disabled without restarting Awl. With the API missing or disabled, Awl keeps its standard motion instead.

- Start opens at the taskbar's width and position, rises into the screen center in 280ms, and reshapes its blank shell to the configured menu size. The UI fades in only once the shell has settled, at its normal proportions. Side/top taskbars and a Start menu on another monitor are supported.
- Taskbar hover lifts/scales the icon with a small spring overshoot, independently of the old Magnify switch. Icons use a transparent overlay so the taskbar window cannot clip their motion.
- A newly opened application's taskbar icon bounces and tilts. Clicking an existing taskbar app also gives that feedback.
- Quick Search holds its magnifier for 180 ms, grows a circle for 200 ms, pauses for 100 ms, then stretches into the bar before the tool buttons drop into place. The complete sequence lasts 1000 ms at normal speed. Typing, composition, or pasting interrupts the reveal immediately without losing the query. Dismissal keeps the short scale/fade exit.
- Start closes with a short fade. Escape, focus loss, and repeated toggles clean up in-flight animation.

All effects respect Awl's animation speed, animation-off/reduce-motion settings, and independent surface GUI scaling. Hover is controlled by this plugin, independently of Magnify. The profile does not introduce a search backdrop, preserving the current search UI and opacity settings.

`motion.json` controls durations, hover size/lift, and bounce/tilt. Images `0.png` and `1.png` are schematic previews, not screenshots.

Native app openings and taskbar activation grow a live DWM thumbnail from the icon to the application's existing bounds in 340ms. The real window's geometry stays intact and its temporary cloak is undone at completion, cancellation, failure, shutdown, or by Awl's guard after an unexpected exit. If the compositor rejects an app, Awl keeps the normal activation. Tooltips use a bubble above the taskbar, with a pointer to the app.

Application order holds for one second after activation or opening. The plugin then lifts, tilts, and grows the promoted app before sliding it into the first slot over 520 ms. Neighbors shift aside with a small spring overshoot and settle. New apps initially enter at the end, so the opening bounce can finish before promotion. Focus/show event hooks refresh the taskbar promptly, and switching back to the first app cancels stale pending moves.

Capture and recorder bars reuse the previous Quick Search 92% spring/fade entrance. Topbar popups (Wi-Fi, Bluetooth, Volume, Microphone, Brightness, Battery, and Notifications) use a top-anchored spring and fade. These effects are independently selectable in motion.json.

App icons land from the main bounce, then make three progressively smaller, faster rebounds before settling.

Pinned columns and grids, taskbar and topbar reveals, context menus, the power menu, capture presets, recent-file menus, and Quick Search unit dropdowns now ease open from their attachment edge. These effects respect GUI scale and the animation setting. Closing during an entrance or disabling the plugin restores the original surface state. The `pinnedPanels`, `taskbarPopups`, and `dropdowns` profile flags independently enable these effects.

Toggle thumbs briefly compress, ease across the track, and bounce lightly into place over 340 ms while the track color blends. The timing follows the supplied Dribbble switch animation reference while keeping Awl colors. Slider knobs grow to 140% on hover or drag using a render transform, so their values and layout do not change. Exclusive choice groups have square internal seams and rounded outer ends; selected and unselected options use the same colors and borders. Selection travels by flattening the facing corners, releasing the old tail, then rounding the new outer edge and inner edge in sequence over 380 ms. Hover slightly rounds internal corners. Rapid changes continue from the current visual state. The `toggleSwitches`, `sliderKnobs`, and `segmentedChoices` flags control these effects, and `controlDurationMs` sets the selection duration; `toggleDurationMs` sets the toggle duration.
