using WukongBenchAuto.Core.Config;

namespace WukongBenchAuto.Tests;

public class SteamLocatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"C:\not\exists")]
    [InlineData(@"C:\Windows")]
    public void IsValid_RejectsInvalidPaths(string? path)
    {
        Assert.False(GameInstallValidator.IsValid(path));
    }

    [Fact]
    public void IsValid_RejectsPathWithoutBenchmarkFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wba_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Assert.False(GameInstallValidator.IsValid(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void IsValid_AcceptsRealBenchLayout()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wba_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "b1", "Binaries", "Win64"));
        File.WriteAllText(Path.Combine(dir, "b1_benchmark.exe"), "stub");
        File.WriteAllText(Path.Combine(dir, "b1", "Binaries", "Win64", "b1-Win64-Shipping.exe"), "stub");
        try
        {
            Assert.True(GameInstallValidator.IsValid(dir));
            Assert.Equal(
                Path.Combine(dir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini"),
                GameInstallValidator.IniPath(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetSteamLibraries_ReturnsExistingAppsDirs()
    {
        var libs = SteamLocator.GetSteamLibraries();
        // На машине с установленным Steam должен найтись хотя бы один путь со steamapps,
        // но в CI этого может не быть — проверяем только отсутствие исключений.
        Assert.All(libs, l => Assert.Contains("steamapps", l));
    }
}
