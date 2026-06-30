using System.Collections.ObjectModel;

namespace Nexus.Shared.Models;

public class FanConfig
{
    public Collection<FanCurvePoint> CpuCurve { get; init; } = [];
    public Collection<FanCurvePoint> GpuCurve { get; init; } = [];
    public FanMode LastMode { get; set; } = FanMode.Auto;

    public static FanConfig Default
    {
        get
        {
            var config = new FanConfig { LastMode = FanMode.Auto };
            PopulateDefaultCurve(config.CpuCurve);
            PopulateDefaultCurve(config.GpuCurve);
            return config;
        }
    }

    private static void PopulateDefaultCurve(Collection<FanCurvePoint> curve)
    {
        curve.Add(new(0, 0));
        curve.Add(new(25, 25));
        curve.Add(new(50, 50));
        curve.Add(new(75, 75));
        curve.Add(new(100, 100));
    }

    public void ValidateAndSortCurves()
    {
        SanitizeCurveInPlace(CpuCurve);
        SanitizeCurveInPlace(GpuCurve);
    }

    private static void SanitizeCurveInPlace(Collection<FanCurvePoint> curve)
    {
        if (curve.Count == 0)
            return;

        var clamped = curve.Select(p => new FanCurvePoint(
            Math.Clamp(p.Temperature, 0, 100),
            Math.Clamp(p.Speed, 0, 100)
        ));

        var distinctPoints = clamped
            .GroupBy(p => p.Temperature)
            .ToDictionary(g => g.Key, g => g.Last().Speed);

        distinctPoints.TryAdd(0, 0);
        distinctPoints.TryAdd(100, 100);

        var sortedPoints = distinctPoints
            .Select(kvp => new FanCurvePoint(kvp.Key, kvp.Value))
            .OrderBy(p => p.Temperature)
            .ToList();

        curve.Clear();
        foreach (var point in sortedPoints)
            curve.Add(point);
    }
}