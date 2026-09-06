using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Chess.Presentation
{
    public class MainMenuSettingsPanel : MonoBehaviour
    {
        [Header("메인 패널")]
        public GameObject panel;
        public Button closeButton;
        public Button soundButton;
        public Button logoutButton;

        [Header("사운드 서브패널")]
        public GameObject soundPanel;
        public Slider bgmSlider;
        public Slider sfxSlider;
        public Button soundBackButton;

        private bool _initializing;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (soundPanel != null) soundPanel.SetActive(false);

            if (closeButton  != null) closeButton.onClick.AddListener(Close);
            if (soundButton  != null) soundButton.onClick.AddListener(OnSoundClicked);
            if (soundBackButton != null) soundBackButton.onClick.AddListener(OnSoundBack);
            if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutClicked);

            if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBGMChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }

        public void Open()
        {
            if (panel != null) panel.SetActive(true);
            if (soundPanel != null) soundPanel.SetActive(false);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (soundPanel != null) soundPanel.SetActive(false);
        }

        void OnSoundClicked()
        {
            if (panel != null) panel.SetActive(false);

            _initializing = true;
            if (soundPanel != null) soundPanel.SetActive(true);
            if (AudioManager.Instance != null)
            {
                if (bgmSlider != null) bgmSlider.value = AudioManager.Instance.bgmVolume;
                if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.sfxVolume;
            }
            _initializing = false;
        }

        void OnSoundBack()
        {
            if (soundPanel != null) soundPanel.SetActive(false);
            if (panel != null) panel.SetActive(true);
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

        void OnLogoutClicked()
        {
            Chess.Core.FirebaseManager.Instance?.SignOut();
            SceneManager.LoadScene("Login");
        }
    }
}