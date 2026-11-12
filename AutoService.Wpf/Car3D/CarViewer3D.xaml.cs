using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using AutoService.Models;

namespace AutoService.Wpf.Car3D;

public enum CarViewPreset { ThreeQuarter, Front, Rear, Left, Right, Top }

public partial class CarViewer3D : UserControl
{
    // Прозрачная «призрачная» деталь кузова на внутреннем слое
    private static readonly Material GhostMaterial = Frozen(new DiffuseMaterial(
        new SolidColorBrush(Color.FromArgb(28, 150, 160, 175))));

    // Синее свечение выбранной зоны
    private static readonly Material GlowMaterial = Frozen(new EmissiveMaterial(
        new SolidColorBrush(Color.FromArgb(110, 40, 110, 255))));

    private static readonly Dictionary<ZoneState, Material> StateMaterials = new()
    {
        [ZoneState.Ok] = MakeStateMaterial(Color.FromRgb(76, 175, 80)),
        [ZoneState.Warning] = MakeStateMaterial(Color.FromRgb(255, 193, 7)),
        [ZoneState.Critical] = MakeStateMaterial(Color.FromRgb(229, 57, 53))
    };

    private List<CarPart> _parts = new();
    private readonly Dictionary<GeometryModel3D, CarPart> _partByModel = new();
    private Dictionary<string, ZoneState> _states = new();
    private CarLayer _layer = CarLayer.Body;
    private string? _selectedZone;
    private Rect3D _bounds = new(-1, 0, -2.5, 2, 2, 5);
    private Point _downPoint;
    private bool _mouseDown;

    // Нажатие на зону: передаём её название
    public event Action<string>? ZoneClicked;

    public CarViewer3D()
    {
        InitializeComponent();
    }

    private static Material Frozen(Material material)
    {
        material.Freeze();
        return material;
    }

    private static Material MakeStateMaterial(Color color)
    {
        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
        group.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(60, color.R, color.G, color.B))));
        group.Freeze();
        return group;
    }

    // ---------- публичные методы ----------

    public CarLayer Layer
    {
        get => _layer;
        set
        {
            _layer = value;
            _selectedZone = null;
            ApplyAppearance();
        }
    }

    public async Task LoadModelAsync(string folder, IZoneMap zoneMap)
    {
        StatusText.Text = "Загрузка 3D-модели…";
        StatusText.Visibility = Visibility.Visible;
        try
        {
            var car = await ObjModelLoader.LoadAsync(folder);

            _parts = car.Parts;
            _partByModel.Clear();
            foreach (var part in _parts)
            {
                var info = zoneMap.Classify(part.GroupName, part.Center);
                part.Layer = info.Layer;
                part.Zone = info.Zone;
                _partByModel[part.Model] = part;
            }

            // внутренние детали идут первыми, прозрачный кузов поверх них
            var group = new Model3DGroup();
            foreach (var part in _parts.Where(p => p.Layer == CarLayer.Inner)) group.Children.Add(part.Model);
            foreach (var part in _parts.Where(p => p.Layer == CarLayer.Body)) group.Children.Add(part.Model);
            CarVisual.Content = group;

            _bounds = car.Bounds;
            ApplyAppearance();
            SetView(CarViewPreset.ThreeQuarter);
            StatusText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Не удалось загрузить модель:\n" + ex.Message;
        }
    }

    // Состояния зон: название зоны -> цвет
    public void SetStates(Dictionary<string, ZoneState> states)
    {
        _states = new Dictionary<string, ZoneState>(states);
        ApplyAppearance();
    }

    public void ClearSelection()
    {
        _selectedZone = null;
        ApplyAppearance();
    }

    // Стандартные виды: спереди, сзади, слева, справа, сверху
    public void SetView(CarViewPreset preset)
    {
        var center = new Point3D(
            _bounds.X + _bounds.SizeX / 2,
            _bounds.Y + _bounds.SizeY / 2,
            _bounds.Z + _bounds.SizeZ / 2);
        double size = Math.Max(_bounds.SizeX, Math.Max(_bounds.SizeY, _bounds.SizeZ));
        double d = size * 1.4;

        // левый борт автомобиля это +X, перёд это +Z
        Vector3D offset = preset switch
        {
            CarViewPreset.Front => new Vector3D(0, 0.1 * size, d),
            CarViewPreset.Rear => new Vector3D(0, 0.1 * size, -d),
            CarViewPreset.Left => new Vector3D(d, 0.1 * size, 0),
            CarViewPreset.Right => new Vector3D(-d, 0.1 * size, 0),
            CarViewPreset.Top => new Vector3D(0, d, 0),
            _ => new Vector3D(0.75 * d, 0.4 * d, 0.85 * d)
        };
        var up = preset == CarViewPreset.Top ? new Vector3D(0, 0, 1) : new Vector3D(0, 1, 0);

        Viewer.Camera = new PerspectiveCamera(center + offset, -offset, up, 45);
    }

    // ---------- внешний вид деталей ----------

    // На внутреннем слое детали кузова становятся «призраками»
    private bool IsGhost(CarPart part) => _layer == CarLayer.Inner && part.Layer == CarLayer.Body;

    private void ApplyAppearance()
    {
        foreach (var part in _parts)
        {
            Material material;
            if (IsGhost(part))
            {
                material = GhostMaterial;
            }
            else
            {
                var state = ZoneState.NotChecked;
                if (part.Zone != null && _states.TryGetValue(part.Zone, out var s)) state = s;
                bool selected = part.Zone != null && part.Zone == _selectedZone;
                material = MaterialFor(part, state, selected);
            }
            part.Model.Material = material;
            part.Model.BackMaterial = material;
        }
    }

    private static Material MaterialFor(CarPart part, ZoneState state, bool selected)
    {
        if (state == ZoneState.NotChecked && !selected) return part.OriginalMaterial;

        Material baseMaterial = state == ZoneState.NotChecked
            ? part.OriginalMaterial
            : StateMaterials[state];
        if (!selected) return baseMaterial;

        var group = new MaterialGroup();
        group.Children.Add(baseMaterial);
        group.Children.Add(GlowMaterial);
        return group;
    }

    // ---------- клик мышью ----------

    private void Viewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _downPoint = e.GetPosition(Viewer);
        _mouseDown = true;
    }

    private void Viewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_mouseDown) return;
        _mouseDown = false;

        var point = e.GetPosition(Viewer);
        // если мышь заметно сдвинули, это не клик
        if (Math.Abs(point.X - _downPoint.X) > 4 || Math.Abs(point.Y - _downPoint.Y) > 4) return;

        PickAt(point);
    }

    private void PickAt(Point point)
    {
        CarPart? hit = null;

        VisualTreeHelper.HitTest(Viewer, null, result =>
        {
            if (result is RayMeshGeometry3DHitTestResult ray
                && ray.ModelHit is GeometryModel3D model
                && _partByModel.TryGetValue(model, out var part))
            {
                if (IsGhost(part)) return HitTestResultBehavior.Continue;   // прозрачное не мешает
                hit = part;
                return HitTestResultBehavior.Stop;                          // первая видимая деталь
            }
            return HitTestResultBehavior.Continue;
        }, new PointHitTestParameters(point));

        if (hit?.Zone != null && hit.Layer == _layer)
        {
            _selectedZone = hit.Zone;
            ApplyAppearance();
            ZoneClicked?.Invoke(hit.Zone);
        }
    }
}