using System.Collections.Generic;
using UnityEngine;

namespace Chess.Presentation
{
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "Audio/Database")]
    public class AudioDatabase : ScriptableObject
    {
        public List<AudioClipData> clips = new();

        private Dictionary<string, AudioClipData> _lookup;

        /// <summary>게임 시작 시 AudioManager에서 한 번 호출</summary>
        public void Initialize()
        {
            _lookup = new Dictionary<string, AudioClipData>(clips.Count);
            foreach (var data in clips)
            {
                if (string.IsNullOrEmpty(data.id))
                {
                    Debug.LogWarning($"[AudioDatabase] id가 비어 있는 항목이 있습니다: {data.name}");
                    continue;
                }
                if (!_lookup.TryAdd(data.id, data))
                    Debug.LogWarning($"[AudioDatabase] 중복 id: {data.id}");
            }
        }

        /// <summary>id로 클립 데이터 조회. 없으면 null 반환</summary>
        public AudioClipData Get(string id)
        {
            if (_lookup == null)
            {
                Debug.LogError("[AudioDatabase] Initialize()가 호출되지 않았습니다.");
                return null;
            }
            _lookup.TryGetValue(id, out var result);
            return result;
        }
    }
}