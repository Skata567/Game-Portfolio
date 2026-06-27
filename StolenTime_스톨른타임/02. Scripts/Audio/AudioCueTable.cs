using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioCueTable", menuName = "60s Dungeon/Audio/Audio Cue Table")]
public class AudioCueTable : ScriptableObject
{
    [Serializable]
    private class Entry
    {
        public AudioEventId eventId;
        public AudioCue cue;
    }

    [SerializeField] private List<Entry> entries = new();

    private readonly Dictionary<AudioEventId, AudioCue> _cueByEvent = new();
    private bool _isCacheDirty = true;

    public bool TryGetCue(AudioEventId eventId, out AudioCue cue)
    {
        if (_isCacheDirty)
            RebuildCache();

        return _cueByEvent.TryGetValue(eventId, out cue) && cue != null;
    }

    private void OnEnable()
    {
        _isCacheDirty = true;
    }

    private void OnValidate()
    {
        _isCacheDirty = true;
        ValidateDuplicates();
    }

    private void RebuildCache()
    {
        _cueByEvent.Clear();

        foreach (Entry entry in entries)
        {
            if (entry == null || entry.eventId == AudioEventId.None || entry.cue == null)
                continue;

            if (_cueByEvent.ContainsKey(entry.eventId))
                continue;

            _cueByEvent.Add(entry.eventId, entry.cue);
        }

        _isCacheDirty = false;
    }

    private void ValidateDuplicates()
    {
        HashSet<AudioEventId> seen = new();
        foreach (Entry entry in entries)
        {
            if (entry == null || entry.eventId == AudioEventId.None)
                continue;

            if (!seen.Add(entry.eventId))
                Debug.LogWarning($"AudioCueTable: '{entry.eventId}' 이벤트가 중복 등록되어 첫 번째 Cue만 사용됩니다.", this);
        }
    }
}
