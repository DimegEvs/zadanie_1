using WukongBenchAuto.Core.Models;
using WukongBenchAuto.Core.Settings;

namespace WukongBenchAuto.Tests;

public class IniProfileApplierTests
{
    private const string SampleIni = """
        [/Script/GSGameSettings.GSGameUserSettings]
        DesiredScreenWidth=1707
        DesiredScreenHeight=960
        bNeverShowStartupUI=False
        UISettingData=(("MainDisplay", "0"),("PrivacyAgreement", "1"),("Vsync", "0"),("ImageQuality", "75"),("InsertFrame", "1"),("Rtx", "0"),("QualityLevel", "3"),("ViewDistance", "3"),("AntiAliasing", "3"),("GlobalIllumination", "3"),("ReflectionQuality", "3"))
        bUseVSync=False
        ResolutionSizeX=2560
        ResolutionSizeY=1440
        FrameRateLimit=0.000000

        [ScalabilityGroups]
        sg.ResolutionQuality=66.6999969
        sg.ViewDistanceQuality=2
        sg.AntiAliasingQuality=2
        sg.ShadowQuality=2
        sg.GlobalIlluminationQuality=2
        sg.RayTracingQuality=0
        sg.ReflectionQuality=2
        sg.PostProcessQuality=2
        sg.TextureQuality=2
        sg.EffectsQuality=2
        sg.FoliageQuality=2
        sg.ShadingQuality=2
        """;

    [Fact]
    public void Apply_SetsResolution()
    {
        var profile = Profiles.Gpu(2560, 1440);
        var result = IniProfileApplier.Apply(SampleIni, profile);

        Assert.Contains("ResolutionSizeX=2560", result);
        Assert.Contains("ResolutionSizeY=1440", result);
        Assert.Contains("LastUserConfirmedResolutionSizeX=2560", result);
    }

    [Fact]
    public void Apply_CpuProfile_DisablesRtxAndFrameGen()
    {
        var profile = Profiles.Cpu(2560, 1440);
        var result = IniProfileApplier.Apply(SampleIni, profile);

        Assert.Contains("(\"Rtx\", \"0\")", result);
        Assert.Contains("(\"InsertFrame\", \"0\")", result);
        Assert.Contains("(\"Dlss\", \"0\")", result);
    }

    [Fact]
    public void Apply_GpuProfile_EnablesRtxAndFullRenderScale()
    {
        var profile = Profiles.Gpu(2560, 1440);
        var result = IniProfileApplier.Apply(SampleIni, profile);

        Assert.Contains("(\"Rtx\", \"1\")", result);
        Assert.Contains("(\"ImageQuality\", \"100\")", result);
        Assert.Contains("sg.RayTracingQuality=3", result);
    }

    [Fact]
    public void Apply_KeepsUnrelatedUiPairs()
    {
        var result = IniProfileApplier.Apply(SampleIni, Profiles.Cpu(1280, 720));
        Assert.Contains("(\"PrivacyAgreement\", \"1\")", result);
    }

    [Fact]
    public void Apply_OverridesExistingQualityValues()
    {
        var profile = Profiles.Cpu(1280, 720);
        var result = IniProfileApplier.Apply(SampleIni, profile);

        Assert.Contains("sg.ViewDistanceQuality=5", result);
        Assert.Contains("sg.TextureQuality=1", result);
        Assert.Contains("(\"QualityLevel\", \"1\")", result);
    }

    [Fact]
    public void Apply_SetsFrameRateLimitZero_AndNoVSync()
    {
        var result = IniProfileApplier.Apply(SampleIni, Profiles.Cpu(1280, 720));
        Assert.Contains("FrameRateLimit=0.000000", result);
        Assert.Contains("bUseVSync=False", result);
    }

    [Fact]
    public void Apply_SetsNeverShowStartupUI()
    {
        var result = IniProfileApplier.Apply(SampleIni, Profiles.Cpu(1280, 720));
        Assert.Contains("bNeverShowStartupUI=True", result);
    }

    [Fact]
    public void ParseUiData_ExtractsPairs()
    {
        var ui = IniProfileApplier.ParseUiData(SampleIni);
        Assert.Equal("75", ui["ImageQuality"]);
        Assert.Equal("1", ui["InsertFrame"]);
        Assert.Equal("3", ui["QualityLevel"]);
    }

    [Fact]
    public void Snapshot_ContainsKeySettings()
    {
        var snap = IniProfileApplier.Snapshot(Profiles.Cpu(1280, 720));
        Assert.Equal("25", snap["ImageQuality"]);
        Assert.Equal("1280", snap["ResolutionSizeX"]);
        Assert.Equal("5", snap["UI:ViewDistance"]);
    }
}

public class ProfilesTests
{
    [Fact]
    public void CpuProfile_MinimalGpuLoad_MaxCpuLoad()
    {
        var p = Profiles.Cpu(2560, 1440);
        Assert.Equal(0, p.Rtx);
        Assert.Equal(0, p.InsertFrame);
        Assert.Equal(0, p.Dlss);
        Assert.Equal(25, p.ImageQuality);
        Assert.Equal(5, p.ViewDistance);
        Assert.Equal(5, p.VegetationQuality);
        Assert.Equal(5, p.FxQuality);
        Assert.Equal(0, p.VSync);
        Assert.Equal(0, p.FrameRateLimit);
    }

    [Fact]
    public void GpuProfile_MaxGpuLoad()
    {
        var p = Profiles.Gpu(2560, 1440);
        Assert.Equal(1, p.Rtx);
        Assert.Equal(100, p.ImageQuality);
        Assert.Equal(2560, p.ResolutionWidth);
        Assert.Equal(1440, p.ResolutionHeight);
        Assert.Equal(0, p.InsertFrame);
        Assert.Equal(0, p.Dlss);
        Assert.Equal(0, p.VSync);
    }
}
