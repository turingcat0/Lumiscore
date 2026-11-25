using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumiscore.Core
{
    [Serializable]
    public sealed class StagePresetValueMap : ISerializationCallbackReceiver
    {
        [Serializable]
        private class Entry
        {
            public string Key;
            [SerializeReference] public IStagePresetValue Value;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        [NonSerialized] private Dictionary<string, IStagePresetValue> _map =
            new Dictionary<string, IStagePresetValue>();

        public int Count => _map.Count;
        public IEnumerable<string> Keys => _map.Keys;

        public bool ContainsKey(string fieldName)
            => !string.IsNullOrEmpty(fieldName) && _map.ContainsKey(fieldName);

        public bool TryGetValue(string fieldName, out IStagePresetValue value)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                value = null;
                return false;
            }

            return _map.TryGetValue(fieldName, out value);
        }

        public void SetValue(string fieldName, IStagePresetValue value)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return;
            }

            _map[fieldName] = value;
        }

        public bool Remove(string fieldName)
            => !string.IsNullOrEmpty(fieldName) && _map.Remove(fieldName);

        public void Clear()
        {
            _map.Clear();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (_map == null)
            {
                return;
            }

            _entries.Clear();
            foreach (KeyValuePair<string, IStagePresetValue> pair in _map)
            {
                _entries.Add(new Entry
                {
                    Key = pair.Key,
                    Value = pair.Value
                });
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _map = new Dictionary<string, IStagePresetValue>();
            if (_entries == null)
            {
                return;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                {
                    _map[entry.Key] = entry.Value;
                }
            }
        }
    }
}
