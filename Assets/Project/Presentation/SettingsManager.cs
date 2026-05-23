using UnityEngine;

namespace Chess.Presentation
{
    public class SettingsManager : MonoBehaviour
    {
        private const string KEY_VIB = "Vibration";

        public static SettingsManager Instance { get; private set; }

        // Audio volume properties now read directly from AudioManager
        public float BGMVolume => AudioManager.Instance != null ? AudioManager.Instance.bgmVolume : 1f;
        public float SFXVolume => AudioManager.Instance != null ? AudioManager.Instance.sfxVolume : 1f;
        public float UIVolume  => AudioManager.Instance != null ? AudioManager.Instance.uiVolume  : 1f;
        public bool Vibration { get; private set; } = true;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void SetBGM(float value)
        {
            AudioManager.Instance?.SetBGMVolume(value);
        }

        public void SetSFX(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
        }

        public void SetUI(float value)
        {
            AudioManager.Instance?.SetUIVolume(value);
        }

        public void SetVibration(bool value)
        {
            Vibration = value;
            PlayerPrefs.SetInt(KEY_VIB, value ? 1 : 0);
        }

        void Load()
        {
            // Audio volume is loaded by AudioManager on its own Awake
            Vibration = PlayerPrefs.GetInt(KEY_VIB, 1) == 1;
        }
    }
}