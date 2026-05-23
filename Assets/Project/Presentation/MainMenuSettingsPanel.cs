using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Chess.Presentation
{
    public class MainMenuSettingsPanel : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panel;
        public Slider bgmSlider;
        public Slider sfxSlider;
        public Toggle vibrationToggle;
        public Button closeButton;

        private bool _initializing;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (bgmSlider != null)
                bgmSlider.onValueChanged.AddListener(OnBGMChanged);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(OnSFXChanged);

            if (vibrationToggle != null)
                vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
        }

        public void Open()
        {
            _initializing = true;
            if (panel != null) panel.SetActive(true);
            if (AudioManager.Instance != null)
            {
                if (bgmSlider != null) bgmSlider.value = AudioManager.Instance.bgmVolume;
                if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.sfxVolume;
            }
            if (vibrationToggle != null && SettingsManager.Instance != null)
                vibrationToggle.isOn = SettingsManager.Instance.Vibration;
            _initializing = false;
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        void OnBGMChanged(float value)
        {
            if (_initializing) return;
            AudioManager.Instance?.SetBGMVolume(value);
        }

        void OnSFXChanged(float value)
        {
            if (_initializing) return;
            AudioManager.Instance?.SetSFXVolume(value);
        }

        void OnVibrationChanged(bool value)
        {
            if (_initializing) return;
            SettingsManager.Instance?.SetVibration(value);
        }
    }
}