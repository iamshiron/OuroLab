using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Quest;

public sealed class OuroQuestValueConverter : IValueConverter {
    private static readonly Dictionary<Sphere, int> Values = new()
    {
        { Sphere.Purple, 11 },
        { Sphere.Blue, 12 },
        { Sphere.Teal, 26 },
        { Sphere.Green, 41 },
        { Sphere.Yellow, 61 },
        { Sphere.Orange, 96 },
        { Sphere.Red, 156 },
    };

    private static readonly HashSet<Sphere> Used =
    [
        Sphere.Purple, Sphere.Blue, Sphere.Teal, Sphere.Green,
        Sphere.Yellow, Sphere.Orange, Sphere.Red,
    ];

    public int GetValue(Sphere sphere) => Values[sphere];
    public IReadOnlySet<Sphere> UsedSpheres => Used;
}
