using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    /// <summary>
    /// 게임 모드 선택 패널. MainMenu / DeckBuilder 양쪽에서 사용.
    /// Inspector에서 버튼 연결 후 Open(callback) 호출.
    /// </summary>
    public class ModeSelectPanel : MonoBehaviour
    {
        [Header("오버레이 (전체화면 배경 — 클릭 시 닫기)")]
        public Button backdropButton;

        [Header("버튼")]
        public Button aiButton;
        public Button normalButton;
        public Button rankedButton;

        [Header("버튼 텍스트 (선택)")]
        public TextMeshProUGUI aiLabel;
        public TextMeshProUGUI normalLabel;
        public TextMeshProUGUI rankedLabel;

        System.Action<GameMode> _onModeSelected;
        float _backdropEnableTime;

        void Awake()
        {
            if (backdropButton != null) backdropButton.onClick.AddListener(OnBackdropClicked);
            if (aiButton       != null) aiButton.onClick.AddListener(()     => OnModeChosen(GameMode.AI));
            if (normalButton   != null) normalButton.onClick.AddListener(() => OnModeChosen(GameMode.Normal));
            if (rankedButton   != null) rankedButton.onClick.AddListener(() => OnModeChosen(GameMode.Ranked));

            gameObject.SetActive(false);
        }

        /// <summary>패널 열기. modeCallback은 모드 확정 시 호출.</summary>
        public void Open(System.Action<GameMode> modeCallback)
        {
            _onModeSelected = modeCallback;
            _backdropEnableTime = Time.unscaledTime + 0.15f;
            gameObject.SetActive(true);
            AudioManager.Instance?.Play("ui_button_confirm");
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void OnBackdropClicked()
        {
            if (Time.unscaledTime < _backdropEnableTime) return;
            Close();
        }

        void OnModeChosen(GameMode mode)
        {
            if (DeckTransfer.Instance != null)
                DeckTransfer.Instance.CurrentMode = mode;

            AudioManager.Instance?.Play("ui_button_confirm");
            Close();
            _onModeSelected?.Invoke(mode);
        }
    }
}
