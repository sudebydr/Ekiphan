namespace Ekiphan.Application.Seed;

using System.Collections.Generic;

public class PerformanceSeedOptions
{
    public int ProductCount { get; set; } = 20000;
    public int CategoryCount { get; set; } = 200;
    public int BrandCount { get; set; } = 100;
    public int AverageImagesPerProduct { get; set; } = 3;
    public int AverageAttributesPerProduct { get; set; } = 8;
    public List<string> LanguageCodes { get; set; } = new() { "tr", "en" };
}
