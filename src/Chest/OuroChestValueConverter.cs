using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Chest;

public sealed class OuroChestValueConverter : IValueConverter {
    private static readonly Dictionary<Sphere, int> Values = new()
    {
        { Sphere.Blue, 16 },
        { Sphere.Teal, 26 },
        { Sphere.Green, 39 },
        { Sphere.Yellow, 59 },
        { Sphere.Orange, 94 },
        { Sphere.Red, 154 },
    };

    private static readonly HashSet<Sphere> Used =
    [
        Sphere.Blue, Sphere.Teal, Sphere.Green,
        Sphere.Yellow, Sphere.Orange, Sphere.Red,
    ];

    public int GetValue(Sphere sphere) => Values[sphere];
    public IReadOnlySet<Sphere> UsedSpheres => Used;
}
