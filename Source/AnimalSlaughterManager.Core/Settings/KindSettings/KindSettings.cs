using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

/// <summary>
/// Per-animal-kind slaughter settings, layered on top of vanilla AutoSlaughterConfig.
/// Slaughter priority is split into four sex×age buckets (matching vanilla's buckets), plus
/// the integration-owned settings stored opaquely under their providers' keys.
/// </summary>
public class KindSettings : IExposable, IPresettable
{
    private const int currentDataVersion = 2;

    private int dataVersion = currentDataVersion;

    // v1 bridge: the traits settings read from their legacy flat keys await their PostLoadInit move.
    private IKindSettings? pendingLegacy;

    public KindPreferenceSettings preferenceSettings;
    public KindPrioritySettings prioritySettings = new();

    /// <summary>Integration-owned settings by provider key; the core never names their types.</summary>
    public Dictionary<string, IKindSettings> integrationsSettings = [];

    public KindSettings()
    {
        preferenceSettings = new();
    }

    public KindSettings(GlobalSettings globals)
    {
        preferenceSettings = new(globals.preferenceSettings);
    }

    /// <summary>True when this kind deviates from vanilla behaviour and must be recomputed.</summary>
    public bool Customized => preferenceSettings.Customized || prioritySettings.HasRules || integrationsSettings.Values.Any(s => s.Customized);

    /// <summary>The integration's settings under the provider's key, created on first use.</summary>
    public T GetOrCreateIntegration<T>(IKindSettingsProvider provider)
        where T : class, IKindSettings
    {
        if (!integrationsSettings.TryGetValue(provider.Key, out var settings))
        {
            settings = provider.Create();
            integrationsSettings[provider.Key] = settings;
        }

        return (T)settings;
    }

    public void Reset()
    {
        preferenceSettings.Reset();
        prioritySettings.Reset();

        foreach (var settings in integrationsSettings.Values)
        {
            settings.Reset();
        }
    }

    public void Reset(GlobalSettings globals)
    {
        preferenceSettings.Reset(globals.preferenceSettings);
        prioritySettings.Reset();

        foreach (var settings in integrationsSettings.Values)
        {
            settings.Reset();
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref dataVersion, "dataVersion", 1);

        if (dataVersion == 1)
        {
            // v1: the integration settings were a flat field of this node. The (single, the
            // traits) provider's settings read the same keys right here, then move under their
            // own key at PostLoadInit; the save is rewritten in the current format.
            var providers = IntegrationServices.GetServices<IKindSettingsProvider>();

            if (Scribe.mode == LoadSaveMode.LoadingVars && pendingLegacy == null && providers.Count > 0)
            {
                pendingLegacy = providers[0].Create();
                pendingLegacy.ExposeData();
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit && pendingLegacy != null && providers.Count > 0)
            {
                integrationsSettings[providers[0].Key] = pendingLegacy;
                pendingLegacy = null;
                dataVersion = currentDataVersion;
            }
        }
        else
        {
            Scribe_Collections.Look(ref integrationsSettings, "integrationData", LookMode.Value, LookMode.Deep);
            integrationsSettings ??= [];
        }

        preferenceSettings.ExposeData();
        prioritySettings.ExposeData();
    }

    /// <summary>The kind's slice of a preset.</summary>
    public KindPresetDto ToPreset(ThingDef def)
    {
        var dto = new KindPresetDto { animal = def.defName };
        Save(dto);
        return dto;
    }

    public void FromPreset(KindPresetDto dto)
    {
        Reset();
        Load(dto);
    }

    public void Save(KindPresetDto dto)
    {
        preferenceSettings.Save(dto);
        prioritySettings.Save(dto);

        var holder = PresetSectionSerialization.Holder();

        foreach (var settings in integrationsSettings.Values)
        {
            settings.WriteXml(holder);
        }

        foreach (var el in PresetSectionSerialization.Children(holder))
        {
            dto.IntegrationSections.Add(el);
        }
    }

    public void Load(KindPresetDto dto)
    {
        preferenceSettings.Load(dto);
        prioritySettings.Load(dto);

        var kind = PresetSectionSerialization.Wrap(dto.IntegrationSections);

        foreach (var settings in integrationsSettings.Values)
        {
            settings.ReadXml(kind);
        }
    }
}
