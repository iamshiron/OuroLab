namespace Shiron.OuroLab.Core;

public interface IValueConverter {
    int GetValue(Sphere sphere);
    IReadOnlySet<Sphere> UsedSpheres { get; }
}
