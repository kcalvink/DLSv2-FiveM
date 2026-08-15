using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

var argsList = args.ToList();
var file = GetArg(argsList, "--file");
var model = GetArg(argsList, "--model");

if (string.IsNullOrWhiteSpace(file))
{
    Console.Error.WriteLine("Usage: --file <path-to-dlsv2-xml> [--model <vehicle-model>]");
    return 2;
}

if (!File.Exists(file))
{
    Console.Error.WriteLine($"File not found: {file}");
    return 2;
}

XDocument doc;
try
{
    doc = XDocument.Load(file, LoadOptions.SetLineInfo);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"XML parse error: {ex.Message}");
    return 1;
}

var root = doc.Root;
if (root == null || root.Name.LocalName != "Model")
{
    Console.Error.WriteLine("Invalid root element. Expected <Model>.");
    return 1;
}

var knownConditionElements = new HashSet<string>(StringComparer.Ordinal)
{
    "All","Any",
    "Time","Weather",
    "AudioControlGroup","AudioMode","LightControlGroup","LightMode",
    "Random",
    "EngineState","IndicatorLights","Doors","Speed","Acceleration","Livery","Towing","Trailer","Extra","LightEmissive","Braking","BodyHealth","EngineHealth","GasTankHealth","HeadlightDamage","BumperDamage","AnimEvent",
    "Driver","Seats","Occupants","Passengers","VehicleOwner","AtTrafficLight",
    "NodeFlags","RoadLanesLeft","RoadLanesRight","RoadShoulder","RoadMedian","RoadDirection","RoadLanes","RoadLanePosition","OnRoad","RoadLaneIndex","RoadHeadingOffset"
};

var allowedAttrs = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
{
    ["Model"] = Set("vehicles"),
    ["Mode"] = Set("name", "apply_default_siren_settings", "toggle", "active"),
    ["AudioMode"] = Set("name", "toggle", "hold", "active"),
    ["ControlGroup"] = Set("name", "cycle", "rev_cycle", "toggle", "exclusive"),
    ["AudioControlGroup"] = Set("name", "cycle", "rev_cycle", "toggle", "exclusive"),
    ["Yield"] = Set("enabled"),
    ["Extra"] = Set("id", "enabled"),
    ["Kit"] = Set("type", "index"),
    ["Paint"] = Set("slot", "color"),
    ["Item"] = Set("id", "sequence"),
    ["Sound"] = Set("soundbank", "soundset"),
    ["timeMultiplier"] = Set("value"),
    ["lightFalloffMax"] = Set("value"),
    ["lightFalloffExponent"] = Set("value"),
    ["lightInnerConeAngle"] = Set("value"),
    ["lightOuterConeAngle"] = Set("value"),
    ["lightOffset"] = Set("value"),
    ["sequencerBpm"] = Set("value"),
    ["leftHeadLightMultiples"] = Set("value"),
    ["rightHeadLightMultiples"] = Set("value"),
    ["leftTailLightMultiples"] = Set("value"),
    ["rightTailLightMultiples"] = Set("value"),
    ["useRealLights"] = Set("value"),
    ["delta"] = Set("value"),
    ["start"] = Set("value"),
    ["speed"] = Set("value"),
    ["sequencer"] = Set("value"),
    ["multiples"] = Set("value"),
    ["direction"] = Set("value"),
    ["syncToBpm"] = Set("value"),
    ["intensity"] = Set("value"),
    ["size"] = Set("value"),
    ["pull"] = Set("value"),
    ["faceCamera"] = Set("value"),
    ["color"] = Set("value"),
    ["lightGroup"] = Set("value"),
    ["rotate"] = Set("value"),
    ["scale"] = Set("value"),
    ["scaleFactor"] = Set("value"),
    ["flash"] = Set("value"),
    ["light"] = Set("value"),
    ["spotLight"] = Set("value"),
    ["castShadows"] = Set("value")
};

foreach (var conditionElement in knownConditionElements)
{
    allowedAttrs.TryAdd(conditionElement, Set(
        "delay_time", "max_on_time", "min_on_time", "stay_on_time",
        "name", "active", "start", "end", "chance",
        "engine_on", "status", "door", "state",
        "units", "inclusive", "abs", "round", "min", "max",
        "id", "attached", "model", "enabled", "side", "damaged",
        "bumper", "condition", "event", "has_driver",
        "occupied", "all", "any", "full", "is_player_vehicle",
        "stopped_at_light", "nearest_n", "max_dist_to_node",
        "no_node_status", "include_disabled_nodes", "on_shoulder",
        "in_median", "is_one_way", "both_directions", "on_road", "from_left", "has_flag"));
}

var allowedChildren = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
{
    ["Model"] = Set("Audio", "PatternSync", "SpeedDrift", "DefaultMode", "Modes", "ControlGroups"),
    ["Audio"] = Set("AudioModes", "AudioControlGroups"),
    ["AudioModes"] = Set("AudioMode"),
    ["AudioControlGroups"] = Set("AudioControlGroup"),
    ["ControlGroups"] = Set("ControlGroup"),
    ["ControlGroup"] = Set("Modes"),
    ["AudioControlGroup"] = Set("AudioModes"),
    ["Modes"] = Set("Mode"),
    ["Mode"] = Set("Yield", "Triggers", "Requirements", "Indicators", "Extras", "ModKits", "Animation", "Paints", "SirenSettings", "Sequences"),
    ["AudioMode"] = Set("Yield", "Sound", "Triggers", "Requirements"),
    ["SirenSettings"] = Set("timeMultiplier","lightFalloffMax","lightFalloffExponent","lightInnerConeAngle","lightOuterConeAngle","lightOffset","textureName","sequencerBpm","leftHeadLight","rightHeadLight","leftTailLight","rightTailLight","leftHeadLightMultiples","rightHeadLightMultiples","leftTailLightMultiples","rightTailLightMultiples","useRealLights","sirens"),
    ["sirens"] = Set("Item"),
    ["Item"] = Set("rotation","flashiness","corona","color","intensity","lightGroup","rotate","scale","scaleFactor","flash","light","spotLight","castShadows", "sequencer"),
    ["rotation"] = Set("delta","start","speed","sequencer","multiples","direction","syncToBpm"),
    ["flashiness"] = Set("delta","start","speed","sequencer","multiples","direction","syncToBpm"),
    ["corona"] = Set("intensity","size","pull","faceCamera"),
    ["Triggers"] = knownConditionElements,
    ["Requirements"] = knownConditionElements,
    ["All"] = knownConditionElements,
    ["Any"] = knownConditionElements,
    ["Flags"] = Set("Item")
};

var unsupported = new List<object>();
ValidateRecursive(root, null, unsupported, allowedAttrs, allowedChildren, knownConditionElements);

var vehiclesRaw = root.Attribute("vehicles")?.Value ?? string.Empty;
var vehicles = vehiclesRaw
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .ToArray();

var normalized = new
{
    Vehicles = vehicles,
    DefaultMode = root.Element("DefaultMode")?.Value,
    PatternSync = root.Element("PatternSync")?.Value,
    SpeedDrift = root.Element("SpeedDrift")?.Value,
    LightModes = root.Element("Modes")?.Elements("Mode").Select(m => m.Attribute("name")?.Value).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(),
    LightControlGroups = root.Element("ControlGroups")?.Elements("ControlGroup").Select(cg => new
    {
        Name = cg.Attribute("name")?.Value,
        Toggle = cg.Attribute("toggle")?.Value,
        Cycle = cg.Attribute("cycle")?.Value,
        ReverseCycle = cg.Attribute("rev_cycle")?.Value,
        Exclusive = cg.Attribute("exclusive")?.Value,
        Modes = cg.Element("Modes")?.Elements("Mode").Select(m => new
        {
            Toggle = m.Attribute("toggle")?.Value,
            Targets = (m.Value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        }).ToArray()
    }).ToArray(),
    AudioModes = root.Element("Audio")?.Element("AudioModes")?.Elements("AudioMode").Select(m => m.Attribute("name")?.Value).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(),
    AudioControlGroups = root.Element("Audio")?.Element("AudioControlGroups")?.Elements("AudioControlGroup").Select(cg => new
    {
        Name = cg.Attribute("name")?.Value,
        Toggle = cg.Attribute("toggle")?.Value,
        Cycle = cg.Attribute("cycle")?.Value,
        ReverseCycle = cg.Attribute("rev_cycle")?.Value,
        Exclusive = cg.Attribute("exclusive")?.Value,
        Modes = cg.Element("AudioModes")?.Elements("AudioMode").Select(m => new
        {
            Toggle = m.Attribute("toggle")?.Value,
            Hold = m.Attribute("hold")?.Value,
            Targets = (m.Value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        }).ToArray()
    }).ToArray()
};

bool? modelMatched = null;
if (!string.IsNullOrWhiteSpace(model))
{
    modelMatched = vehicles.Any(v => string.Equals(v, model, StringComparison.OrdinalIgnoreCase));
}

var report = new
{
    File = Path.GetFullPath(file),
    VehicleMatchQuery = model,
    VehicleMatch = modelMatched,
    UnsupportedProperties = unsupported,
    NormalizedConfiguration = normalized
};

Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
{
    WriteIndented = true
}));

return unsupported.Count == 0 ? 0 : 3;

static string? GetArg(List<string> args, string name)
{
    var idx = args.FindIndex(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    if (idx < 0 || idx + 1 >= args.Count)
    {
        return null;
    }

    return args[idx + 1];
}

static HashSet<string> Set(params string[] values) => new(values, StringComparer.Ordinal);

static void ValidateRecursive(
    XElement element,
    XElement? parent,
    List<object> unsupported,
    Dictionary<string, HashSet<string>> allowedAttrs,
    Dictionary<string, HashSet<string>> allowedChildren,
    HashSet<string> knownConditionElements)
{
    var elementName = element.Name.LocalName;

    if (allowedAttrs.TryGetValue(elementName, out var attrs))
    {
        foreach (var attr in element.Attributes())
        {
            if (!attrs.Contains(attr.Name.LocalName))
            {
                unsupported.Add(new
                {
                    Type = "UnknownAttribute",
                    Element = elementName,
                    Attribute = attr.Name.LocalName,
                    Path = BuildPath(element),
                    Line = GetLine(element)
                });
            }
        }
    }

    HashSet<string>? allowedForParent = null;
    if (parent != null)
    {
        var parentName = parent.Name.LocalName;
        if (allowedChildren.TryGetValue(parentName, out var parentAllowed))
        {
            allowedForParent = parentAllowed;
        }
        else if (string.Equals(parentName, "Triggers", StringComparison.OrdinalIgnoreCase)
              || string.Equals(parentName, "Requirements", StringComparison.OrdinalIgnoreCase)
              || string.Equals(parentName, "All", StringComparison.OrdinalIgnoreCase)
              || string.Equals(parentName, "Any", StringComparison.OrdinalIgnoreCase))
        {
            allowedForParent = knownConditionElements;
        }

        if (allowedForParent != null && !allowedForParent.Contains(elementName))
        {
            unsupported.Add(new
            {
                Type = "UnknownElement",
                Element = elementName,
                Parent = parentName,
                Path = BuildPath(element),
                Line = GetLine(element)
            });
        }
    }

    foreach (var child in element.Elements())
    {
        ValidateRecursive(child, element, unsupported, allowedAttrs, allowedChildren, knownConditionElements);
    }
}

static string BuildPath(XElement el)
{
    var names = new Stack<string>();
    var current = el;
    while (current != null)
    {
        names.Push(current.Name.LocalName);
        current = current.Parent;
    }

    return "/" + string.Join('/', names);
}

static int? GetLine(XElement el)
{
    if (el is IXmlLineInfo info && info.HasLineInfo())
    {
        return info.LineNumber;
    }

    return null;
}
