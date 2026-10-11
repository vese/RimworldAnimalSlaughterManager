using System;
using System.IO;
using System.Linq;
using ASM;
using Xunit;

namespace ASM.Tests;

/// <summary>
/// Condition-list presets at the format level (DTO only — no RimWorld types, the reference
/// package has no runtime). Covers the unified carrier roundtrip, the root-element dispatch
/// between the current and the two historical formats, and the bucket mapping. Runs against a
/// temp preset tree via the PresetIO test hooks.
/// </summary>
public class PresetIOConditionPresetsTests : IDisposable
{
    private readonly string root;

    public PresetIOConditionPresetsTests()
    {
        root = Path.Combine(Path.GetTempPath(), "asm-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        PresetIO.RootFolderOverride = root;
        PresetIO.Store = new FilePresetStore();
    }

    public void Dispose()
    {
        PresetIO.RootFolderOverride = null;
        PresetIO.Store = new FilePresetStore();
        Directory.Delete(root, true);
    }

    private static string BucketPath(CondBucket bucket) =>
        Path.Combine(PresetIO.RootFolderOverride!, "List", "Conditions", bucket.ToString());

    private static PriorityRuleDto Pregnancy(bool has) => new() { rule = "PregnancyPriorityRule", has = has };

    private static SlaughterPresetDto Carrier(KindDto kd) => new() { Kinds = [kd] };

    [Fact]
    public void Carrier_RoundTripsThroughText()
    {
        var kd = new KindDto { animal = "" };
        PresetFormats.BucketRuleDtos(kd, CondBucket.AdultMale).Add(Pregnancy(false));

        var text = PresetFormats.SerializeToText(Carrier(kd));
        var read = Assert.Single(PresetFormats.ReadCarrier(text)!.Kinds);

        var rules = PresetFormats.ConditionRuleDtos(read);
        Assert.NotNull(rules);
        Assert.False(Assert.Single(rules!).has);
    }

    [Fact]
    public void RootElement_DistinguishesTheThreeFormats()
    {
        Assert.Equal("SlaughterPreset", PresetFormats.RootElementOf(PresetFormats.SerializeToText(Carrier(new KindDto()))));
        Assert.Equal("RulePresetDto", PresetFormats.RootElementOf("<RulePresetDto bucket=\"AdultMale\" />"));
        Assert.Equal("ConditionPreset", PresetFormats.RootElementOf("<ConditionPreset bucket=\"AdultMale\" />"));
    }

    [Fact]
    public void ConditionRules_TakeTheSingleNonEmptyBucketList_CrossBucket()
    {
        var kd = new KindDto { animal = "" };
        PresetFormats.BucketRuleDtos(kd, CondBucket.YoungFemale).Add(Pregnancy(true));

        // Read back regardless of the bucket the rules were stored under.
        var text = PresetFormats.SerializeToText(Carrier(kd));
        var read = PresetFormats.ReadCarrier(text)!.Kinds[0];

        Assert.Single(PresetFormats.ConditionRuleDtos(read)!);
        Assert.Empty(PresetFormats.BucketRuleDtos(read, CondBucket.AdultMale));
    }

    [Fact]
    public void ReadRulePreset_ReadsPreUnificationFormat()
    {
        const string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<RulePresetDto bucket=\"AdultMale\">\n" +
            "  <Rule rule=\"PregnancyPriorityRule\" has=\"true\" />\n" +
            "</RulePresetDto>";

        var rules = PresetFormats.ReadRulePreset(xml);

        Assert.True(Assert.Single(rules!).has);
    }

    [Fact]
    public void ReadConditionPreset_ReadsLegacyFormat()
    {
        const string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<ConditionPreset bucket=\"AdultMale\">\n" +
            "  <Cond type=\"Pregnancy\" has=\"true\" />\n" +
            "</ConditionPreset>";

        var conditions = PresetFormats.ReadConditionPreset(xml);

        Assert.Equal("Pregnancy", Assert.Single(conditions!).type);
    }

    [Fact]
    public void ListConditionPresets_MarksEachWithItsBucket()
    {
        Directory.CreateDirectory(BucketPath(CondBucket.AdultMale));
        Directory.CreateDirectory(BucketPath(CondBucket.YoungFemale));
        File.WriteAllText(Path.Combine(BucketPath(CondBucket.AdultMale), "a.xml"),
            PresetFormats.SerializeToText(Carrier(new KindDto())));
        File.WriteAllText(Path.Combine(BucketPath(CondBucket.YoungFemale), "b.xml"),
            PresetFormats.SerializeToText(Carrier(new KindDto())));

        var entries = PresetIO.ListConditionPresets(CondBucket.AdultMale);

        Assert.Equal(CondBucket.AdultMale, entries.First(e => e.name == "a").condBucket);
        Assert.Equal(CondBucket.YoungFemale, entries.First(e => e.name == "b").condBucket);
    }
}
