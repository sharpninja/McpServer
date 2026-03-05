// Wrapper project that exposes NSubstitute 6.0.0 via direct Reference from lib/NSubstitute.6.0.0.
// Populate lib/NSubstitute.6.0.0 via scripts\Copy-NSubstituteFromCache.ps1, then commit so CI can build.
using System.Reflection;

[assembly: AssemblyTitle("NSubstitute.Reference")]
[assembly: AssemblyDescription("Local reference to NSubstitute 6.0.0")]
[assembly: AssemblyVersion("6.0.0.0")]
