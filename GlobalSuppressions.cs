// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

// Suppress StyleCop rules that are too strict for our development phase
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header", Justification = "Not required during development phase")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1200:Using directives should be placed correctly", Justification = "Global using statements are acceptable")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Related types can be in the same file")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1413:Use trailing comma in multi-line initializers", Justification = "Not enforced")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Flexible ordering allowed")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1518:File should end with a single newline", Justification = "Handled by EditorConfig")]