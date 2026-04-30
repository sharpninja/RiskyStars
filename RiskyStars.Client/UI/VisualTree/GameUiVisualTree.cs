using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace RiskyStars.Client;

internal static class GameUiVisualElementIds
{
    public const string BackBuffer = "xna.backBuffer";
    public const string MapViewport = "xna.mapViewport";
    public const string MyraDesktop = "myra.desktop";
    public const string TopBar = "hud.topBar";
    public const string ResourceChips = "hud.resourceChips";
    public const string HelpPanel = "hud.helpPanel";
    public const string SelectionPanel = "hud.selectionPanel";
    public const string PlayerDashboard = "window.playerDashboard";
    public const string EncyclopediaWindow = "window.encyclopedia";
    public const string ContinentZoomWindow = "window.continentZoom";
    public const string MapSelectionTarget = "xna.mapSelectionTarget";
    public const string ContinentZoomSurface = "xna.continentZoomSurface";
    public const string CombatOverlay = "xna.combatOverlay";
}

internal enum GameUiVisualElementSource
{
    Myra,
    Xna
}

internal readonly record struct GameUiScaleContext(
    int BackBufferWidth,
    int BackBufferHeight,
    int ClientWidth,
    int ClientHeight,
    int UiScalePercent,
    float UiScaleFactor)
{
    public float DpiScaleX => ClientWidth > 0 ? BackBufferWidth / (float)ClientWidth : 1f;
    public float DpiScaleY => ClientHeight > 0 ? BackBufferHeight / (float)ClientHeight : 1f;

    public static GameUiScaleContext Create(
        int backBufferWidth,
        int backBufferHeight,
        int clientWidth,
        int clientHeight,
        int uiScalePercent,
        float uiScaleFactor)
    {
        return new GameUiScaleContext(
            Math.Max(1, backBufferWidth),
            Math.Max(1, backBufferHeight),
            Math.Max(1, clientWidth),
            Math.Max(1, clientHeight),
            Math.Clamp(uiScalePercent, 1, 400),
            Math.Clamp(uiScaleFactor, 0.01f, 4f));
    }
}

internal sealed record GameUiAuditEntry(
    string Id,
    string? ParentId,
    int Depth,
    GameUiVisualElementSource Source,
    string TypeName,
    bool Visible,
    bool TreeVisible,
    Rectangle DeclaredBounds,
    Rectangle LocalBounds,
    Rectangle ScreenBounds,
    int UiScalePercent,
    float UiScaleFactor,
    float DpiScaleX,
    float DpiScaleY,
    IReadOnlyList<string> Warnings)
{
    public bool HasValidScreenBounds => ScreenBounds.Width > 0 && ScreenBounds.Height > 0;
}

internal sealed record GameUiAuditReport(
    GameUiScaleContext Scale,
    IReadOnlyList<GameUiAuditEntry> Entries)
{
    public int MyraCount => Entries.Count(entry => entry.Source == GameUiVisualElementSource.Myra);
    public int XnaCount => Entries.Count(entry => entry.Source == GameUiVisualElementSource.Xna);
    public int WarningCount => Entries.Count(entry => entry.Warnings.Count > 0);
    public int HiddenCount => Entries.Count(entry => !entry.TreeVisible);
    public int InvalidCount => Entries.Count(entry => !entry.HasValidScreenBounds);

    public string Summary =>
        $"UI: {Entries.Count} elements ({MyraCount} Myra, {XnaCount} XNA), " +
        $"{WarningCount} warnings, UI scale {Scale.UiScalePercent}%, " +
        $"DPI {Scale.DpiScaleX:F2}x{Scale.DpiScaleY:F2}";
}

internal readonly record struct GameUiVisualTreeRow(
    string Id,
    string DisplayText,
    string BoundsText,
    int Depth,
    bool IsSelected,
    bool HasWarnings,
    bool HasValidScreenBounds);

internal readonly record struct GameUiWidgetMetrics(
    bool Visible,
    bool TreeVisible,
    Rectangle DeclaredBounds,
    Rectangle LocalBounds,
    Rectangle ScreenBounds);

internal static class GameUiWidgetBoundsResolver
{
    public static bool TryGetScreenBounds(Widget? widget, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        if (widget == null || !IsWidgetTreeVisible(widget))
        {
            return false;
        }

        GameUiWidgetMetrics metrics = GetMetrics(widget);
        bounds = metrics.ScreenBounds;
        return bounds.Width > 0 && bounds.Height > 0;
    }

    public static GameUiWidgetMetrics GetMetrics(Widget widget)
    {
        var declared = new Rectangle(widget.Left, widget.Top, widget.Width ?? 0, widget.Height ?? 0);
        var local = widget.ActualBounds.Width > 0 && widget.ActualBounds.Height > 0
            ? widget.ActualBounds
            : widget.Bounds;
        var screen = ResolveVisualScreenBounds(
            widget.ToGlobal(Point.Zero),
            widget.Bounds,
            local,
            widget.Width,
            widget.Height);

        return new GameUiWidgetMetrics(
            widget.Visible,
            IsWidgetTreeVisible(widget),
            declared,
            local,
            screen);
    }

    public static Rectangle ResolveVisualScreenBounds(
        Point widgetGlobalOrigin,
        Rectangle widgetBounds,
        Rectangle actualBounds,
        int? explicitWidth,
        int? explicitHeight)
    {
        int width = explicitWidth.GetValueOrDefault();
        int height = explicitHeight.GetValueOrDefault();
        if (width <= 0 || height <= 0)
        {
            width = widgetBounds.Width;
            height = widgetBounds.Height;
        }

        if (width > 0 && height > 0)
        {
            return new Rectangle(widgetGlobalOrigin.X, widgetGlobalOrigin.Y, width, height);
        }

        return actualBounds.Width > 0 && actualBounds.Height > 0
            ? new Rectangle(
                widgetGlobalOrigin.X + actualBounds.X,
                widgetGlobalOrigin.Y + actualBounds.Y,
                actualBounds.Width,
                actualBounds.Height)
            : Rectangle.Empty;
    }

    private static bool IsWidgetTreeVisible(Widget widget)
    {
        for (Widget? current = widget; current != null; current = current.Parent)
        {
            if (!current.Visible)
            {
                return false;
            }
        }

        return true;
    }
}

internal static class GameUiLayoutMetrics
{
    public static int ResolveTopBarBottom(Rectangle measuredTopBarBounds, int fallbackHeight)
    {
        if (measuredTopBarBounds.Width <= 0 || measuredTopBarBounds.Height <= 0)
        {
            return Math.Max(0, fallbackHeight);
        }

        return Math.Max(0, measuredTopBarBounds.Bottom);
    }

    public static int ResolveContentTop(Rectangle measuredTopBarBounds, int fallbackHeight, int gap)
    {
        return ResolveTopBarBottom(measuredTopBarBounds, fallbackHeight) + Math.Max(0, gap);
    }
}

internal static class GameUiVisualTreeInspector
{
    public static IReadOnlyList<GameUiVisualTreeRow> BuildRows(
        GameUiAuditReport report,
        string? selectedElementId,
        int maxRows = int.MaxValue)
    {
        if (report.Entries.Count == 0 || maxRows <= 0)
        {
            return [];
        }

        return report.Entries
            .OrderBy(entry => entry.Id, StringComparer.Ordinal)
            .Take(maxRows)
            .Select(entry => CreateRow(entry, selectedElementId))
            .ToList();
    }

    public static bool TryResolveSelectedBounds(
        GameUiAuditReport report,
        string? selectedElementId,
        out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        if (string.IsNullOrWhiteSpace(selectedElementId))
        {
            return false;
        }

        GameUiAuditEntry? selectedEntry = report.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Id, selectedElementId, StringComparison.Ordinal));
        if (selectedEntry == null || !selectedEntry.HasValidScreenBounds)
        {
            return false;
        }

        bounds = selectedEntry.ScreenBounds;
        return true;
    }

    public static string FormatSelectionDetails(GameUiAuditReport report, string? selectedElementId)
    {
        if (string.IsNullOrWhiteSpace(selectedElementId))
        {
            return "Selected: none";
        }

        GameUiAuditEntry? selectedEntry = report.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Id, selectedElementId, StringComparison.Ordinal));
        if (selectedEntry == null)
        {
            return $"Selected: {selectedElementId} (not in current visual tree)";
        }

        string warnings = selectedEntry.Warnings.Count == 0
            ? "none"
            : string.Join(", ", selectedEntry.Warnings);

        return string.Join(
            Environment.NewLine,
            $"Selected: {selectedEntry.Id}",
            $"Source: {selectedEntry.Source} {selectedEntry.TypeName}",
            $"Declared: {FormatBounds(selectedEntry.DeclaredBounds)}",
            $"Local: {FormatBounds(selectedEntry.LocalBounds)}",
            $"Screen: {FormatBounds(selectedEntry.ScreenBounds)}",
            $"Visible: {selectedEntry.Visible}, tree visible: {selectedEntry.TreeVisible}",
            $"Warnings: {warnings}");
    }

    private static GameUiVisualTreeRow CreateRow(GameUiAuditEntry entry, string? selectedElementId)
    {
        bool isSelected = string.Equals(entry.Id, selectedElementId, StringComparison.Ordinal);
        string marker = isSelected ? "> " : "  ";
        string warningMarker = entry.Warnings.Count > 0 ? "! " : "  ";
        string indent = new(' ', Math.Min(entry.Depth, 8) * 2);
        string source = entry.Source == GameUiVisualElementSource.Xna ? "XNA" : "Myra";
        string text = $"{marker}{indent}{warningMarker}{entry.Id} ({source} {entry.TypeName})";

        return new GameUiVisualTreeRow(
            entry.Id,
            text,
            FormatBounds(entry.ScreenBounds),
            entry.Depth,
            isSelected,
            entry.Warnings.Count > 0,
            entry.HasValidScreenBounds);
    }

    private static string FormatBounds(Rectangle bounds)
    {
        return $"{bounds.Width}x{bounds.Height} @ {bounds.X},{bounds.Y}";
    }
}

internal sealed record GameUiHierarchyValidationIssue(string ElementId, string Message);

internal sealed record GameUiHierarchyValidationReport(
    IReadOnlyList<GameUiHierarchyValidationIssue> Errors,
    IReadOnlyList<GameUiHierarchyValidationIssue> Warnings)
{
    public bool IsValid => Errors.Count == 0;
}

internal static class GameUiVisualTreeHierarchyValidator
{
    public static GameUiHierarchyValidationReport Validate(GameUiAuditReport report)
    {
        var errors = new List<GameUiHierarchyValidationIssue>();
        var warnings = new List<GameUiHierarchyValidationIssue>();
        var entriesById = new Dictionary<string, GameUiAuditEntry>(StringComparer.Ordinal);

        foreach (GameUiAuditEntry entry in report.Entries)
        {
            if (!entriesById.TryAdd(entry.Id, entry))
            {
                errors.Add(new GameUiHierarchyValidationIssue(entry.Id, "Duplicate visual tree element id."));
            }
        }

        foreach (GameUiAuditEntry entry in report.Entries)
        {
            ValidateParent(entry, entriesById, errors, warnings);
        }

        return new GameUiHierarchyValidationReport(errors, warnings);
    }

    private static void ValidateParent(
        GameUiAuditEntry entry,
        IReadOnlyDictionary<string, GameUiAuditEntry> entriesById,
        ICollection<GameUiHierarchyValidationIssue> errors,
        ICollection<GameUiHierarchyValidationIssue> warnings)
    {
        if (string.IsNullOrWhiteSpace(entry.ParentId))
        {
            if (entry.Source == GameUiVisualElementSource.Myra &&
                !string.Equals(entry.Id, GameUiVisualElementIds.MyraDesktop, StringComparison.Ordinal))
            {
                warnings.Add(new GameUiHierarchyValidationIssue(entry.Id, "Myra element is not nested under a parent."));
            }

            return;
        }

        string parentId = entry.ParentId!;
        if (string.Equals(entry.Id, parentId, StringComparison.Ordinal))
        {
            errors.Add(new GameUiHierarchyValidationIssue(entry.Id, "Element cannot be its own parent."));
            return;
        }

        if (!entriesById.TryGetValue(parentId, out GameUiAuditEntry? parent))
        {
            errors.Add(new GameUiHierarchyValidationIssue(entry.Id, $"Parent '{parentId}' is missing."));
            return;
        }

        if (entry.Depth <= parent.Depth)
        {
            errors.Add(new GameUiHierarchyValidationIssue(entry.Id, "Child depth must be greater than parent depth."));
        }

        if (!parent.TreeVisible && entry.TreeVisible)
        {
            warnings.Add(new GameUiHierarchyValidationIssue(entry.Id, "Child is visible while its parent is hidden."));
        }

        if (entry.Source == GameUiVisualElementSource.Myra &&
            parent.Source == GameUiVisualElementSource.Myra &&
            entry.TreeVisible &&
            parent.TreeVisible &&
            entry.HasValidScreenBounds &&
            parent.HasValidScreenBounds &&
            !parent.ScreenBounds.Contains(entry.ScreenBounds))
        {
            warnings.Add(new GameUiHierarchyValidationIssue(entry.Id, "Child bounds extend outside parent bounds."));
        }
    }
}

internal sealed class GameUiVisualTree
{
    private readonly Dictionary<string, VisualElement> _elements = new(StringComparer.Ordinal);
    private readonly Dictionary<Widget, string> _myraWidgetIds = new(ReferenceEqualityComparer.Instance);

    public void AddMyraElement(string id, Widget? widget, string? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(id) || widget == null)
        {
            return;
        }

        if (_myraWidgetIds.TryGetValue(widget, out string? existingId) &&
            !string.Equals(existingId, id, StringComparison.Ordinal))
        {
            SetParentIfMissing(existingId, parentId);
            return;
        }

        _elements[id] = VisualElement.FromMyra(widget, parentId, ResolveDepth(parentId));
        _myraWidgetIds[widget] = id;
    }

    public void AddMyraRoot(string id, Rectangle bounds, string typeName = "Desktop", string? parentId = null)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            _elements[id] = VisualElement.FromMyraRoot(bounds, typeName, parentId, ResolveDepth(parentId));
        }
    }

    public void AddXnaElement(string id, Rectangle bounds, string typeName = "XNA", string? parentId = null)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            _elements[id] = VisualElement.FromXna(bounds, typeName, parentId, ResolveDepth(parentId));
        }
    }

    public void AddMyraTree(string idPrefix, IEnumerable<Widget> roots)
    {
        int index = 0;
        string? parentId = _elements.ContainsKey(idPrefix) ? idPrefix : null;
        foreach (Widget root in roots)
        {
            AddMyraSubtree($"{idPrefix}.{index}", root, parentId);
            index++;
        }
    }

    public bool TryResolveBounds(string id, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        return _elements.TryGetValue(id, out VisualElement element) &&
            element.TryResolveBounds(out bounds);
    }

    public bool TryGetWidget(string id, out Widget widget)
    {
        widget = null!;
        if (!_elements.TryGetValue(id, out VisualElement element) || element.Widget == null)
        {
            return false;
        }

        widget = element.Widget;
        return true;
    }

    public IReadOnlyDictionary<string, Rectangle> ResolveBounds()
    {
        var resolved = new Dictionary<string, Rectangle>(StringComparer.Ordinal);
        foreach (var (id, element) in _elements)
        {
            if (element.TryResolveBounds(out Rectangle bounds))
            {
                resolved[id] = bounds;
            }
        }

        return resolved;
    }

    public GameUiAuditReport CreateAuditReport(GameUiScaleContext scale)
    {
        var entries = new List<GameUiAuditEntry>(_elements.Count);
        foreach (var (id, element) in _elements.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            entries.Add(element.CreateAuditEntry(id, scale));
        }

        return new GameUiAuditReport(scale, entries);
    }

    private void AddMyraSubtree(string path, Widget widget, string? parentId)
    {
        string typeName = widget.GetType().Name;
        string elementId;
        if (_myraWidgetIds.TryGetValue(widget, out string? existingId))
        {
            elementId = existingId;
            SetParentIfMissing(elementId, parentId);
        }
        else
        {
            elementId = $"{path}.{typeName}";
            AddMyraElement(elementId, widget, parentId);
        }

        int childIndex = 0;
        foreach (Widget child in EnumerateMyraChildren(widget))
        {
            AddMyraSubtree($"{path}.{typeName}.{childIndex}", child, elementId);
            childIndex++;
        }
    }

    private static IEnumerable<Widget> EnumerateMyraChildren(Widget widget)
    {
        var yielded = new HashSet<Widget>(ReferenceEqualityComparer.Instance);

        if (widget is Panel panel)
        {
            foreach (Widget child in panel.Widgets)
            {
                if (yielded.Add(child))
                {
                    yield return child;
                }
            }
        }

        foreach (string propertyName in new[] { "Widgets", "Children" })
        {
            var property = widget.GetType().GetProperty(propertyName);
            if (property == null)
            {
                continue;
            }

            System.Collections.IEnumerable? children;
            try
            {
                children = property.GetValue(widget) as System.Collections.IEnumerable;
            }
            catch
            {
                continue;
            }

            if (children == null)
            {
                continue;
            }

            foreach (object? item in children)
            {
                if (item is Widget child && yielded.Add(child))
                {
                    yield return child;
                }
            }
        }

        foreach (string propertyName in new[] { "Content", "Widget", "Child" })
        {
            var property = widget.GetType().GetProperty(propertyName);
            if (property == null || !typeof(Widget).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            Widget? child;
            try
            {
                child = property.GetValue(widget) as Widget;
            }
            catch
            {
                continue;
            }

            if (child != null && yielded.Add(child))
            {
                yield return child;
            }
        }
    }

    private int ResolveDepth(string? parentId)
    {
        return !string.IsNullOrWhiteSpace(parentId) && _elements.TryGetValue(parentId, out VisualElement parent)
            ? parent.Depth + 1
            : 0;
    }

    private void SetParentIfMissing(string id, string? parentId)
    {
        if (string.IsNullOrWhiteSpace(parentId) ||
            !_elements.TryGetValue(id, out VisualElement element) ||
            !string.IsNullOrWhiteSpace(element.ParentId))
        {
            return;
        }

        _elements[id] = element with
        {
            ParentId = parentId,
            Depth = ResolveDepth(parentId)
        };
    }

    private readonly record struct VisualElement(
        Widget? Widget,
        Rectangle? Bounds,
        string TypeName,
        GameUiVisualElementSource Source,
        string? ParentId,
        int Depth)
    {
        public static VisualElement FromMyra(Widget widget, string? parentId, int depth)
        {
            return new VisualElement(widget, null, widget.GetType().Name, GameUiVisualElementSource.Myra, parentId, depth);
        }

        public static VisualElement FromMyraRoot(Rectangle bounds, string typeName, string? parentId, int depth)
        {
            return new VisualElement(null, bounds, typeName, GameUiVisualElementSource.Myra, parentId, depth);
        }

        public static VisualElement FromXna(Rectangle bounds, string typeName, string? parentId, int depth)
        {
            return new VisualElement(null, bounds, typeName, GameUiVisualElementSource.Xna, parentId, depth);
        }

        public bool TryResolveBounds(out Rectangle bounds)
        {
            if (Widget != null)
            {
                return GameUiWidgetBoundsResolver.TryGetScreenBounds(Widget, out bounds);
            }

            bounds = Bounds ?? Rectangle.Empty;
            return bounds.Width > 0 && bounds.Height > 0;
        }

        public GameUiAuditEntry CreateAuditEntry(string id, GameUiScaleContext scale)
        {
            if (Widget != null)
            {
                GameUiWidgetMetrics metrics = GameUiWidgetBoundsResolver.GetMetrics(Widget);
                return new GameUiAuditEntry(
                    id,
                    ParentId,
                    Depth,
                    GameUiVisualElementSource.Myra,
                    TypeName,
                    metrics.Visible,
                    metrics.TreeVisible,
                    metrics.DeclaredBounds,
                    metrics.LocalBounds,
                    metrics.ScreenBounds,
                    scale.UiScalePercent,
                    scale.UiScaleFactor,
                    scale.DpiScaleX,
                    scale.DpiScaleY,
                    BuildWarnings(metrics));
            }

            Rectangle bounds = Bounds ?? Rectangle.Empty;
            return new GameUiAuditEntry(
                id,
                ParentId,
                Depth,
                Source,
                TypeName,
                true,
                true,
                bounds,
                bounds,
                bounds,
                scale.UiScalePercent,
                scale.UiScaleFactor,
                scale.DpiScaleX,
                scale.DpiScaleY,
                BuildWarnings(bounds));
        }

        private static IReadOnlyList<string> BuildWarnings(GameUiWidgetMetrics metrics)
        {
            var warnings = new List<string>();
            if (!metrics.TreeVisible)
            {
                warnings.Add("hidden");
            }

            if (metrics.ScreenBounds.Width <= 0 || metrics.ScreenBounds.Height <= 0)
            {
                warnings.Add("no resolved screen size");
            }

            return warnings;
        }

        private static IReadOnlyList<string> BuildWarnings(Rectangle bounds)
        {
            return bounds.Width <= 0 || bounds.Height <= 0
                ? ["no resolved screen size"]
                : [];
        }
    }
}
