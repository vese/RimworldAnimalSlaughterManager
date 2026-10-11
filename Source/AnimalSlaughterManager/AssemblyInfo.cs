using System.Runtime.CompilerServices;

// Tests reach internal test hooks (Store, RootFolderOverride). GenerateAssemblyInfo is off,
// so the MSBuild InternalsVisibleTo item would not be emitted — the attribute lives here.
[assembly: InternalsVisibleTo("AnimalSlaughterManager.Tests")]
