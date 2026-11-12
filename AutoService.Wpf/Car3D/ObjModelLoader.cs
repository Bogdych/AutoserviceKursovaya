using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace AutoService.Wpf.Car3D;

// Одна деталь автомобиля
public sealed class CarPart
{
    public string GroupName { get; init; } = "";
    public GeometryModel3D Model { get; init; } = null!;
    public Material OriginalMaterial { get; init; } = null!;
    public Point3D Center { get; init; }
    public CarLayer Layer { get; set; }
    public string? Zone { get; set; }
}

// Результат загрузки модели
public sealed class LoadedCar
{
    public List<CarPart> Parts { get; init; } = new();
    public Rect3D Bounds { get; init; }
}

// Читает модель из файлов model.obj и model.mtl
public static class ObjModelLoader
{
    private sealed class RawPart
    {
        public string Group = "";
        public string Material = "";
        public List<float> Positions = new();
        public List<float> Normals = new();
        public List<float> Uvs = new();
        public List<int> Indices = new();
        public Dictionary<long, int> Map = new();
        public bool HasNormals;
        public float MinX = float.MaxValue, MinY = float.MaxValue, MinZ = float.MaxValue;
        public float MaxX = float.MinValue, MaxY = float.MinValue, MaxZ = float.MinValue;
    }

    private sealed class MtlInfo
    {
        public Color Color = Colors.Gray;
        public double Alpha = 1;
        public string? Texture;
    }

    private sealed record PartData(string Group, Point3D Center, MeshGeometry3D Mesh, Material Material);
    private sealed record BuildResult(List<PartData> Parts, Rect3D Bounds);

    public static async Task<LoadedCar> LoadAsync(string folder)
    {
        string objPath = System.IO.Path.Combine(folder, "model.obj");
        if (!File.Exists(objPath))
            throw new FileNotFoundException("Не найден файл модели: " + objPath);

        // тяжёлое чтение выполняем в фоне, чтобы окно не зависало
        var built = await Task.Run(() => Build(folder, objPath));

        // объекты, которые потом будем перекрашивать, создаём в потоке окна
        var parts = new List<CarPart>();
        foreach (var d in built.Parts)
        {
            parts.Add(new CarPart
            {
                GroupName = d.Group,
                Center = d.Center,
                OriginalMaterial = d.Material,
                Model = new GeometryModel3D(d.Mesh, d.Material) { BackMaterial = d.Material }
            });
        }
        return new LoadedCar { Parts = parts, Bounds = built.Bounds };
    }

    private static BuildResult Build(string folder, string objPath)
    {
        var raw = ParseObj(objPath);
        var mtl = ParseMtl(System.IO.Path.Combine(folder, "model.mtl"));
        var materials = new Dictionary<string, Material>();

        var list = new List<PartData>(raw.Count);
        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

        foreach (var r in raw)
        {
            if (r.Indices.Count == 0) continue;

            if (!materials.TryGetValue(r.Material, out var material))
            {
                mtl.TryGetValue(r.Material, out var info);
                material = BuildMaterial(info, folder);
                materials[r.Material] = material;
            }

            var center = new Point3D((r.MinX + r.MaxX) / 2, (r.MinY + r.MaxY) / 2, (r.MinZ + r.MaxZ) / 2);
            list.Add(new PartData(r.Group, center, BuildMesh(r), material));

            minX = Math.Min(minX, r.MinX); minY = Math.Min(minY, r.MinY); minZ = Math.Min(minZ, r.MinZ);
            maxX = Math.Max(maxX, r.MaxX); maxY = Math.Max(maxY, r.MaxY); maxZ = Math.Max(maxZ, r.MaxZ);
        }

        return new BuildResult(list, new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ));
    }

    // ---------- файл .obj ----------

    private static float F(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static List<RawPart> ParseObj(string path)
    {
        var v = new List<float>();
        var vt = new List<float>();
        var vn = new List<float>();
        var byKey = new Dictionary<(string, string), RawPart>();
        var order = new List<RawPart>();
        string group = "default", material = "";
        RawPart? cur = null;

        foreach (string line in File.ReadLines(path))
        {
            if (line.Length < 2) continue;

            if (line[0] == 'v')
            {
                var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (line[1] == ' ') { v.Add(F(p[1])); v.Add(F(p[2])); v.Add(F(p[3])); }
                else if (line[1] == 't') { vt.Add(F(p[1])); vt.Add(F(p[2])); }
                else if (line[1] == 'n') { vn.Add(F(p[1])); vn.Add(F(p[2])); vn.Add(F(p[3])); }
            }
            else if (line[0] == 'f' && line[1] == ' ')
            {
                if (cur == null)
                {
                    if (!byKey.TryGetValue((group, material), out cur))
                    {
                        cur = new RawPart { Group = group, Material = material };
                        byKey[(group, material)] = cur;
                        order.Add(cur);
                    }
                }

                var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int n = p.Length - 1;
                if (n < 3) continue;

                var face = new int[n];
                for (int i = 0; i < n; i++)
                    face[i] = AddVertex(cur, p[i + 1], v, vt, vn);

                // многоугольник режем на треугольники
                for (int i = 1; i < n - 1; i++)
                {
                    cur.Indices.Add(face[0]);
                    cur.Indices.Add(face[i]);
                    cur.Indices.Add(face[i + 1]);
                }
            }
            else if ((line[0] == 'g' || line[0] == 'o') && line[1] == ' ')
            {
                group = line.Substring(2).Trim();
                cur = null;
            }
            else if (line.StartsWith("usemtl ", StringComparison.Ordinal))
            {
                material = line.Substring(7).Trim();
                cur = null;
            }
        }
        return order;
    }

    private static int Resolve(string s, int count)
    {
        int i = int.Parse(s, CultureInfo.InvariantCulture);
        return i > 0 ? i - 1 : count + i;
    }

    // Добавляет вершину в деталь (одинаковые вершины не дублируются)
    private static int AddVertex(RawPart part, string token, List<float> v, List<float> vt, List<float> vn)
    {
        var a = token.Split('/');
        int vi = Resolve(a[0], v.Count / 3);
        int ti = a.Length > 1 && a[1].Length > 0 ? Resolve(a[1], vt.Count / 2) : -1;
        int ni = a.Length > 2 && a[2].Length > 0 ? Resolve(a[2], vn.Count / 3) : -1;

        long key = ((long)vi << 42) | ((long)(ti + 1) << 21) | (long)(ni + 1);
        if (part.Map.TryGetValue(key, out int existing)) return existing;

        int index = part.Positions.Count / 3;
        float x = v[vi * 3], y = v[vi * 3 + 1], z = v[vi * 3 + 2];
        part.Positions.Add(x); part.Positions.Add(y); part.Positions.Add(z);

        if (x < part.MinX) part.MinX = x; if (x > part.MaxX) part.MaxX = x;
        if (y < part.MinY) part.MinY = y; if (y > part.MaxY) part.MaxY = y;
        if (z < part.MinZ) part.MinZ = z; if (z > part.MaxZ) part.MaxZ = z;

        if (ti >= 0) { part.Uvs.Add(vt[ti * 2]); part.Uvs.Add(1f - vt[ti * 2 + 1]); }  // у WPF ось V направлена вниз
        else { part.Uvs.Add(0); part.Uvs.Add(0); }

        if (ni >= 0)
        {
            part.Normals.Add(vn[ni * 3]); part.Normals.Add(vn[ni * 3 + 1]); part.Normals.Add(vn[ni * 3 + 2]);
            part.HasNormals = true;
        }
        else { part.Normals.Add(0); part.Normals.Add(1); part.Normals.Add(0); }

        part.Map[key] = index;
        return index;
    }

    private static MeshGeometry3D BuildMesh(RawPart r)
    {
        int n = r.Positions.Count / 3;
        var positions = new Point3DCollection(n);
        var normals = new Vector3DCollection(n);
        var uvs = new PointCollection(n);

        for (int i = 0; i < n; i++)
        {
            positions.Add(new Point3D(r.Positions[i * 3], r.Positions[i * 3 + 1], r.Positions[i * 3 + 2]));
            normals.Add(new Vector3D(r.Normals[i * 3], r.Normals[i * 3 + 1], r.Normals[i * 3 + 2]));
            uvs.Add(new Point(r.Uvs[i * 2], r.Uvs[i * 2 + 1]));
        }

        var mesh = new MeshGeometry3D
        {
            Positions = positions,
            TextureCoordinates = uvs,
            TriangleIndices = new Int32Collection(r.Indices)
        };
        if (r.HasNormals) mesh.Normals = normals;
        mesh.Freeze();   // замороженный объект можно передать из фонового потока в окно
        return mesh;
    }

    // ---------- файл .mtl (материалы) ----------

    private static Dictionary<string, MtlInfo> ParseMtl(string path)
    {
        var result = new Dictionary<string, MtlInfo>();
        if (!File.Exists(path)) return result;

        MtlInfo? cur = null;
        foreach (string raw in File.ReadLines(path))
        {
            string line = raw.Trim();
            if (line.StartsWith("newmtl ", StringComparison.Ordinal))
            {
                cur = new MtlInfo();
                result[line.Substring(7).Trim()] = cur;
            }
            else if (cur == null) continue;
            else if (line.StartsWith("Kd ", StringComparison.Ordinal))
            {
                var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                cur.Color = Color.FromRgb(ToByte(p[1]), ToByte(p[2]), ToByte(p[3]));
            }
            else if (line.StartsWith("d ", StringComparison.Ordinal))
            {
                var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                cur.Alpha = double.Parse(p[1], CultureInfo.InvariantCulture);
            }
            else if (line.StartsWith("map_Kd ", StringComparison.Ordinal))
            {
                cur.Texture = line.Substring(7).Trim();
            }
        }
        return result;
    }

    private static byte ToByte(string s) =>
        (byte)Math.Clamp((int)Math.Round(double.Parse(s, CultureInfo.InvariantCulture) * 255), 0, 255);

    private static Material BuildMaterial(MtlInfo? info, string folder)
    {
        Brush brush;
        string? texturePath = info?.Texture == null ? null : System.IO.Path.Combine(folder, info.Texture);

        if (texturePath != null && File.Exists(texturePath))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(texturePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            brush = new ImageBrush(bitmap)
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, 1, 1)
            };
        }
        else
        {
            brush = new SolidColorBrush(info?.Color ?? Colors.Gray);
        }

        double alpha = info?.Alpha ?? 1;
        if (alpha < 0.95) brush.Opacity = Math.Max(alpha, 0.2);   // стёкла
        brush.Freeze();

        var material = new DiffuseMaterial(brush);
        material.Freeze();
        return material;
    }
}