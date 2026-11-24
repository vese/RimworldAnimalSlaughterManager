namespace ASM;

/// <summary>An integration settings entry that needs its owning kind — e.g. to migrate its
/// legacy data into the core storage; KindSettings attaches itself to every entry it holds.</summary>
public interface IOwnedKindSettings
{
    void Attach(KindSettings owner);
}
