# Awl Motion API

Awl Motion API is a Core compatibility plugin. It makes no visible changes by itself. Other Core plugins can request `awl.animation-api` and use it for time-based, spring, wave, custom procedural, or input-following animation.

```csharp
var motion = host.GetService("awl.animation-api") as IAwlAnimationApi;
if (motion == null) return;

motion.Animate(new AwlAnimationSpec {
  DurationMs = 420,
  From = 0,
  To = 1,
  ProceduralCurve = p => p * p * (3 - 2 * p)
}, value => card.Opacity = value);

// Follow a drag, pointer position, scroll value, audio level, or other live source.
motion.Follow(() => normalizedPointerX, value => card.RenderTransform = new TranslateTransform(value * 24, 0));
```

`AwlAnimationCurve` also includes `Spring`, `Wave`, `ElasticOut`, `BackOut`, and standard easing curves. The API returns an `IAwlAnimationHandle`; call `Cancel` or `Dispose` when the plugin unloads its own UI.
