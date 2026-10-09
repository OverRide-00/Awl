# Awl Plugin SDK

Awl plugins are installed from `.awlplugin` ZIP archives. The archive must contain `manifest.json` at its root. Awl extracts each package into `%LOCALAPPDATA%\Awl\Plugins\Installed\<plugin-id>` and stores enable state in `config.json`.

## Core plugins

Reference `Awl.exe`, implement `Awl.IAwlCorePlugin`, and put the resulting assembly in the package:

```csharp
using Awl;
using System.Windows.Controls;

public sealed class ExamplePlugin : IAwlCorePlugin {
  public void Initialize(IAwlPluginHost host) {
    host.RegisterAction(new AwlPluginAction {
      Id = "hello",
      Title = "Say hello",
      Description = "Runs from Config",
      Execute = () => host.Log("Hello from the plugin")
    });

    host.RegisterCalculator(new AwlPluginCalculator {
      Id = "double",
      Title = "Double a number",
      Evaluate = query => query.StartsWith("double ")
        ? (double.Parse(query.Substring(7)) * 2).ToString()
        : null
    });

    host.RegisterTab(new AwlPluginTab {
      Id = "example",
      Title = "Example",
      Build = () => new TextBlock { Text = "Plugin tab" }
    });
  }
}
```

The host also supports `RegisterToolbarAction`. Core DLLs remain loaded for the Awl process lifetime, so enabling, disabling, updating, or removing one takes effect after restart. The standalone and Dev Mode executables currently contain their own SDK types. A DLL compiled against Awl.exe must therefore ship a second DLL compiled against Awl.DevMode.exe to run in Dev Mode; declare that relative filename as `entryAssemblyDevMode` in the manifest. Both DLLs use the same entry type. Build.ps1 packages both variants for Motion API and Ratio.

For small actions, a package can omit a DLL and declare `actions` in its manifest. Targets stay inside the plugin folder unless they are absolute URIs. A `.ps1` target is launched through PowerShell.

## Visual plugins

Visual plugins contain `style.css` and no executable assembly. Their manifest type is `Visual`, or `Both` when paired with Core behavior.

```css
:root {
  --background: #10251f;
  --text: #ecfff7;
  --accent: #62d6ad;
  --panel: #17342c;
  --card: #1d3e35;
  --muted: #9bb8ae;
  --card-border: #31564a;
  --card-radius: 18px;
  --card-padding: 20px;
}
```

Visual plugins may also declare `"motion": "motion.json"` in their manifest. This is a declarative profile, not a DLL. The host uses the installed `awl.animation-api` service to animate supported surfaces; without that service it retains standard Awl behavior. The reference package is `plugins/Visual/motion-showcase`.

Supported switches: `startMenu`, `quickSearch`, `taskbarHover`, `appLaunch`, `taskbarReorder`, `captureBars`, `topbarPopups`. Durations in milliseconds: `menuDurationMs`, `searchDurationMs`, `hoverDurationMs`, `launchDurationMs`, `windowDurationMs`, `exitDurationMs`, `reorderDurationMs`, `searchRevealDurationMs`, `captureDurationMs`, `panelDurationMs`. Hover and launch parameters: `hoverScale`, `hoverLift`, `bounceHeight`, `tiltDegrees`. The required service is declared as `"requires": "awl.animation-api"`. Invalid/nonfinite values are rejected; magnitudes and duration are bounded. Awl's animation speed multiplies the durations, and disabling animation/reducing motion prevents effects. Visual motion changes apply live, and in-flight effects are canceled and their transforms restored when disabled or removed. The API itself still adds no animations.

These profiles can only affect Awl's menu, search, and taskbar visuals. Native app fly-ins use a live DWM thumbnail with a temporary source cloak, while preserving source window geometry. Completion, cancellation, shutdown and a guard process restore the source. They cannot register actions, scripts, calculations, or functional tabs. Motion preferences remain in the external ZIP package. Packages can use `.zip` or `.awlplugin`.

## Catalog metadata

`plugins/catalog.json` and `templates/catalog.json` are the repository indexes. Catalog items include type, categories, tags, author, version, download URL, and summary. The runtime already provides text, type, and multi-category filtering for the future browse screen.

Package an unpacked plugin with:

```powershell
.\scripts\Pack-Plugin.ps1 -Source .\plugins\examples\visual-example
```

## Shared services

Core plugins can provide capabilities to other Core plugins through the host service registry:

```csharp
host.RegisterService("author.service", service);
var service = host.GetService("author.service");
```

### Motion API

Install **Awl Motion API** before a plugin that needs procedural motion. It registers `awl.animation-api` as an `IAwlAnimationApi` service. The API can animate arbitrary numeric output, follow live input such as a drag position or audio level, loop and auto-reverse, and evaluate built-in or custom curves.

```csharp
var motion = host.GetService("awl.animation-api") as IAwlAnimationApi;
if (motion != null) {
  motion.Animate(new AwlAnimationSpec {
    DurationMs = 420,
    ProceduralCurve = p => p * p * (3 - 2 * p)
  }, value => panel.Opacity = value);
}
```

`AwlAnimationCurve` includes `Linear`, `SmoothStep`, `EaseIn`, `EaseOut`, `EaseInOut`, `BackOut`, `ElasticOut`, `Spring`, and `Wave`. Call `Cancel()` or `Dispose()` on a returned handle when the owning plugin removes its UI.

## Plugin sections

Core plugins can place a themed card in an existing Awl tab. The section icon defaults to the plugin manifest icon, and is used in its card heading. Tabs use the same icon behavior.

```csharp
host.RegisterSection(new AwlPluginSection {
  Id = "my-section",
  Tab = "Interface",
  Title = "My section",
  Icon = "ratio",
  Build = () => new TextBlock { Text = "Section content" }
});
```

### Plugin updates

Config checks the repository plugin catalog at startup and hourly, with a Check updates action for an immediate check. Installed plugin rows and browser cards/details show Update when the catalog version is newer. Version comparison uses numeric components and prerelease precedence; build metadata does not create an update. Increase the manifest and catalog version together and publish a matching ZIP.

Updates validate the package ID and exact catalog version before replacing any installed files. Disabled plugins stay disabled and plugin DataRoot is preserved. Visual packages apply live. Core packages are staged in the persistent Plugins/Pending library and applied before loading plugins after restart. Their action becomes Reload, and the existing Start menu reload action is available. Dev Mode uses its own persistent library and the same update flow. A failed extraction leaves the installed version intact.

Motion profiles can enable `toggleSwitches`, `sliderKnobs`, and `segmentedChoices`, with `controlDurationMs` (default 380) and `toggleDurationMs` (default 340). All control effects use the shared Motion API, respect the global animation setting, preserve immediate control actions, and restore ordinary templates/transforms when the Visual plugin is disabled. Choice groups include Quick options, text/icon modes, color mode, module alignment, Bars tabs, system sorting, and browser tabs/filters.
