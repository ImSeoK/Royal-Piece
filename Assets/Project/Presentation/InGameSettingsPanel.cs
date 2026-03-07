using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Chess.Presentation
{
    public class InGameSettingsPanel : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panel;
        public Slider bgmSlider;
        public Slider sfxSlider;
        public Button forfeitButton;
        public Button closeButton;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (forfeitButton != null)
                forfeitButton.onClick.AddListener(OnForfeit);

            if (bgmSlider != null)
                bgmSlider.onValueChanged.AddListener(OnBGMChanged);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }

        public void Open()
        {
            if (panel != null) panel.SetActive(true);
            if (SettingsManager.Instance == null) return;
            if (bgmSlider != null) bgmSlider.value = SettingsManager.Instance.BGMVolume;
            if (sfxSlider != null) sfxSlider.value = SettingsManager.Instance.SFXVolume;
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        void OnForfeit()
        {
            SceneManager.LoadScene("MainMenu");
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
    }
}