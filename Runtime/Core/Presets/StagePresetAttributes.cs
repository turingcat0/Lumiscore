using System;

namespace Lumiscore.Core
{
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

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class StagePresetIgnoreAttribute : Attribute
    {
    }
}
