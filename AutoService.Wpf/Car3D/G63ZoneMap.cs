using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;

namespace AutoService.Wpf.Car3D;

// Слой модели: внешний (кузов) или внутренний (узлы и детали)
public enum CarLayer { Body, Inner }

// К какому слою и зоне относится деталь (Zone = null: по ней нельзя кликнуть)
public record PartClass(CarLayer Layer, string? Zone);

// Правила разбора модели на зоны. Для каждой модели автомобиля своя реализация.
public interface IZoneMap
{
    PartClass Classify(string groupName, Point3D center);
}

// У этой модели левый борт смотрит в сторону +X, перёд автомобиля в сторону +Z
public class G63ZoneMap : IZoneMap
{
    private static readonly string[] SuspensionWords =
    {
        "axle", "shock", "coilover", "lowerlink", "upperlink", "lowerarm", "upperarm", "leaf",
        "swaybar", "trackbar", "draglink", "tierod", "pitman", "kingpin", "ujoint", "halfshaft",
        "propshaft", "subframe", "linkguards", "uppermounts", "lowermounts", "steeringbox",
        "steeringlink", "hub_"
    };

    private static readonly Regex DoorRegex = new(@"^G-class_(FL|FR|RL|RR)_door_");
    private static readonly Regex DoorGlassRegex = new(@"^G-class_doorglass_(FL|FR|RL|RR)");

    public PartClass Classify(string n, Point3D c)
    {
        bool left = c.X > 0;
        string corner = (c.Z > 0 ? "перед." : "задн.") + " " + (left ? "лев." : "прав.");

        PartClass Body(string? zone) => new(CarLayer.Body, zone);
        PartClass Inner(string zone) => new(CarLayer.Inner, zone);
        bool S(string prefix) => n.StartsWith(prefix, StringComparison.Ordinal);

        // колёса: диск и шина (внешний слой), ступица и тормоза (внутренний слой)
        if (S("Object_4.") || S("AMG_wheels")) return Body($"Колесо ({corner})");
        if (S("_wheel_")) return Inner($"Тормоза и ступица ({corner})");

        // узлы и детали (выхлоп проверяем раньше двигателя)
        if (n.Contains("sidepipe") || n.Contains("exhaust")) return Inner("Выхлопная система");
        if (n.Contains("engine_v8") || S("G-class_v8_")) return Inner("Двигатель");
        if (n.Contains("engbay")) return Inner("Моторный отсек");
        if (SuspensionWords.Any(w => n.Contains(w))) return Inner("Подвеска и мосты");
        if (S("chassis_") || S("G-class_frame") || S("G-class_underbody")) return Inner("Рама и днище");

        // салон
        if (S("AMG_interior") || n.Contains("doorpanel") || S("AMG_rear_seats") || S("AMG_driver_seat")
            || S("AMG_passanger_seat") || n.Contains("steeringwheel") || S("G-class_needle")
            || S("G-class_shifter") || S("G-class_signalstalk") || S("AMG_gauges") || S("temp_")
            || S("Music") || n.Contains("_screen_") || S("brabus_screen") || S("AMG_screen")
            || S("G-class_sunroof_int"))
            return Inner("Салон");

        // кузов
        var door = DoorRegex.Match(n);
        if (!door.Success) door = DoorGlassRegex.Match(n);
        if (door.Success)
        {
            string where = door.Groups[1].Value switch
            {
                "FL" => "перед. лев.",
                "FR" => "перед. прав.",
                "RL" => "задн. лев.",
                _ => "задн. прав."
            };
            return Body($"Дверь ({where})");
        }

        if (S("AMG_hood")) return Body("Капот");
        if (S("AMG_tailgate") || S("brabus_tailgate") || S("G-class_tailgateglass")) return Body("Крышка багажника");
        if (S("AMG_front_bamper") || S("brabus_front_bamper") || S("AMG_grill")) return Body("Передний бампер");
        if (S("AMG_rear_bamper")) return Body("Задний бампер");
        if (n.Contains("fender")) return Body($"Крыло ({corner})");
        if (n.Contains("mirror_")) return Body("Зеркало " + (left ? "левое" : "правое"));
        if (S("G-class_headlight") || S("G-class_signal_") || S("G-class_fendersignalglass"))
            return Body("Фара " + (left ? "левая" : "правая"));
        if (S("G-class_taillight")) return Body("Задний фонарь " + (left ? "левый" : "правый"));
        if (S("AMG_running_board")) return Body("Подножка " + (left ? "левая" : "правая"));
        if (S("AMG_sparetire")) return Body("Запасное колесо");
        if (S("G-class_body") || S("bodyshell") || S("G-class_windshield") || S("G-class_quarterglass")
            || S("G-class_sunroof_glass"))
            return Body("Кузов и крыша");

        return Body(null);
    }
}