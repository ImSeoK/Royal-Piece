using UnityEngine;
using UnityEngine.Audio;

namespace Chess.Presentation
{
    public class SettingsManager : MonoBehaviour
    {
        private const string KEY_BGM = "BGMVolume";
        private const string KEY_SFX = "SFXVolume";
        private const string KEY_VIB = "Vibration";

        public static SettingsManager Instance { get; private set; }

        public float BGMVolume { get; private set; } = 1f;
        public float SFXVolume { get; private set; } = 1f;
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
            BGMVolume = value;
            PlayerPrefs.SetFloat(KEY_BGM, value);
            AudioManager.Instance?.SetVolume(value);
        }

        public void SetSFX(float value)
        {
            SFXVolume = value;
            PlayerPrefs.SetFloat(KEY_SFX, value);
        }

        public void SetVibration(bool value)
        {
            Vibration = value;
            PlayerPrefs.SetInt(KEY_VIB, value ? 1 : 0);
        }

        void Load()
        {
            BGMVolume = PlayerPrefs.GetFloat(KEY_BGM, 1f);
            SFXVolume = PlayerPrefs.GetFloat(KEY_SFX, 1f);
            Vibration = PlayerPrefs.GetInt(KEY_VIB, 1) == 1;
        }
    }
}