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
            if (panel != null) panel.SetActive(true);

            if (SettingsManager.Instance == null) return;
            if (bgmSlider != null) bgmSlider.value = SettingsManager.Instance.BGMVolume;
            if (sfxSlider != null) sfxSlider.value = SettingsManager.Instance.SFXVolume;
            if (vibrationToggle != null) vibrationToggle.isOn = SettingsManager.Instance.Vibration;
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        void OnBGMChanged(float value)
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetBGM(value);
        }

        void OnSFXChanged(float value)
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetSFX(value);
        }

        void OnVibrationChanged(bool value)
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetVibration(value);
        }
    }
}