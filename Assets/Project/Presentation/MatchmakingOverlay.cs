using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Chess.Core;

namespace Chess.Presentation
{
    public class MatchmakingOverlay : MonoBehaviour
    {
        public static MatchmakingOverlay Instance { get; private set; }

        [Header("풀 바 (MainMenu용)")]
        public GameObject fullPanel;
        public Button cancelButton;

        [Header("미니 아이콘 (다른 씬용)")]
        public GameObject miniPanel;
        public Button miniCancelButton;
        public RectTransform miniSpinnerTransform;

        [Header("공용")]
        public RectTransform spinnerTransform;
        public GameObject matchFoundPanel;

        Button _startGameButton;
        RectTransform _categoryButtonsRT;
        Vector2 _categoryOriginalAnchorMin;
        Vector2 _categoryOriginalAnchorMax;
        Vector2 _categoryOriginalAnchoredPos;

        bool _isMatchmaking;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            if (fullPanel != null) fullPanel.SetActive(false);
            if (miniPanel != null) miniPanel.SetActive(false);
            if (matchFoundPanel != null) matchFoundPanel.SetActive(false);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_isMatchmaking) return;

            bool isMainMenu = IsFullPanelScene(scene.name);
            if (fullPanel != null) fullPanel.SetActive(isMainMenu);
            if (miniPanel != null) miniPanel.SetActive(!isMainMenu);

            // MainMenu가 아니면 startGameButton/ButtonGrid 참조 무효화
            if (!isMainMenu)
            {
                _startGameButton = null;
                _categoryButtonsRT = null;
            }
        }

        void Update()
        {
            if (!_isMatchmaking) return;
            if (spinnerTransform != null)
                spinnerTransform.Rotate(0f, 0f, -200f * Time.deltaTime);
            if (miniSpinnerTransform != null)
                miniSpinnerTransform.Rotate(0f, 0f, -200f * Time.deltaTime);
        }

        public void RegisterMainMenuRefs(Button startGameButton, GameObject categoryButtonsParent)
        {
            _startGameButton = startGameButton;

            if (categoryButtonsParent != null)
            {
                var rt = categoryButtonsParent.GetComponent<RectTransform>();
                _categoryOriginalAnchorMin   = rt.anchorMin;
                _categoryOriginalAnchorMax   = rt.anchorMax;
                _categoryOriginalAnchoredPos = rt.anchoredPosition;
                _categoryButtonsRT = rt;
            }

            if (_isMatchmaking)
                ApplyUIState(true);
        }

        public void StartMatchmaking(GameMode mode)
        {
            var pm = PhotonManager.Instance;
            if (pm == null) return;

            pm.OnMatchReady  += HandleMatchReady;
            pm.OnMatchFailed += HandleMatchFailed;
            pm.StartMatchmaking(mode);

            _isMatchmaking = true;
            ApplyUIState(true);

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(Cancel);
            }
            if (miniCancelButton != null)
            {
                miniCancelButton.onClick.RemoveAllListeners();
                miniCancelButton.onClick.AddListener(Cancel);
            }
        }

        void HandleMatchReady()
        {
            Unsubscribe();
            _isMatchmaking = false;
            ApplyUIState(false);
            StartCoroutine(ShowMatchFoundAndLoad());
        }

        void HandleMatchFailed(string reason)
        {
            Unsubscribe();
            _isMatchmaking = false;
            ApplyUIState(false);
            Debug.LogError($"[MatchmakingOverlay] 매칭 실패: {reason}");
        }

        void Cancel()
        {
            Unsubscribe();
            PhotonManager.Instance?.Disconnect();
            _isMatchmaking = false;
            ApplyUIState(false);
        }

        bool IsFullPanelScene(string sceneName) =>
            sceneName == "MainMenu" || sceneName == "Inventory";

        void Unsubscribe()
        {
            var pm = PhotonManager.Instance;
            if (pm == null) return;
            pm.OnMatchReady  -= HandleMatchReady;
            pm.OnMatchFailed -= HandleMatchFailed;
        }

        void ApplyUIState(bool show)
        {
            bool isMainMenu = IsFullPanelScene(SceneManager.GetActiveScene().name);

            if (fullPanel != null) fullPanel.SetActive(show && isMainMenu);
            if (miniPanel != null) miniPanel.SetActive(show && !isMainMenu);
            if (!show && matchFoundPanel != null) matchFoundPanel.SetActive(false);

            if (_startGameButton != null)
                _startGameButton.gameObject.SetActive(!show);

            if (_categoryButtonsRT != null && _startGameButton != null)
            {
                var startRT = _startGameButton.GetComponent<RectTransform>();
                if (show)
                {
                    _categoryButtonsRT.anchorMin        = startRT.anchorMin;
                    _categoryButtonsRT.anchorMax        = startRT.anchorMax;
                    _categoryButtonsRT.anchoredPosition = startRT.anchoredPosition;
                }
                else
                {
                    _categoryButtonsRT.anchorMin        = _categoryOriginalAnchorMin;
                    _categoryButtonsRT.anchorMax        = _categoryOriginalAnchorMax;
                    _categoryButtonsRT.anchoredPosition = _categoryOriginalAnchoredPos;
                }
            }
        }

        System.Collections.IEnumerator ShowMatchFoundAndLoad()
        {
            if (matchFoundPanel != null) matchFoundPanel.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            if (matchFoundPanel != null) matchFoundPanel.SetActive(false);
            PhotonManager.Instance?.LoadGameScene();
        }
    }
}
