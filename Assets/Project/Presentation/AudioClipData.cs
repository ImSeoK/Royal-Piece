using UnityEngine;

namespace Chess.Presentation
{
    public enum AudioType { SFX, UI, BGM }

    [CreateAssetMenu(fileName = "New AudioClipData", menuName = "Audio/Clip Data")]
    public class AudioClipData : ScriptableObject
    {
        [Header("식별")]
        public string id;           // 예: "sfx_gacha_ssr", "ui_button_tap"
        public AudioType type;      // SFX / UI / BGM

        [Header("클립")]
        public AudioClip clip;

        [Header("재생 설정")]
        [Range(0f, 1f)] public float volumeScale = 1f;
        [Range(0f, 0.5f)] public float pitchVariance = 0f; // 0이면 피치 고정
        public bool loop = true; // BGM: true / Sting: false
    }
}