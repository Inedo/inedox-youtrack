using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Inedo.Extensibility;

[assembly: AssemblyTitle("YouTrack")]
[assembly: AssemblyDescription("Contains an issue tracking source for JetBrains YouTrack, and operations for working with issues and fields.")]
[assembly: AssemblyCompany("Inedo, LLC")]
[assembly: AssemblyCopyright("Copyright © Inedo 2021")]
[assembly: AssemblyVersion("2.4.0")]
[assembly: AssemblyFileVersion("2.4.0")]
[assembly: CLSCompliant(false)]
[assembly: ComVisible(false)]

[assembly: AppliesTo(InedoProduct.BuildMaster | InedoProduct.Otter)]
[assembly: ScriptNamespace("YouTrack")]
