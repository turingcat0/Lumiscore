using System;

namespace Lumiscore.Core
{
    /// <summary>
    /// Streaming anchor attribute that assigns this public field and all subsequent public fields
    /// (in Base-to-Derived declaration order) to the named preset part until another anchor appears.
    /// Repeated keys merge into the same part.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class StagePresetPartAttribute : Attribute
    {
        public string Key { get; }
        public string DisplayName { get; }

        public StagePresetPartAttribute(string key, string displayName = null)
        {
            Key = key ?? string.Empty;
            DisplayName = string.IsNullOrEmpty(displayName) ? Key : displayName;
        }
    }

    /// <summary>
    /// Excludes a public field from preset capture and override tracking without closing the active part anchor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class StagePresetIgnoreAttribute : Attribute
    {
    }
}
