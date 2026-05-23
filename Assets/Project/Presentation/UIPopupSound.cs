using UnityEngine;

namespace Chess.Presentation
{
    public class UIPopupSound : MonoBehaviour
    {
        [SerializeField] private string openSoundId  = "ui_popup_open";
        [SerializeField] private string closeSoundId = "ui_popup_close";

        public void OnOpen()
        {
            if (AudioManager.Instance == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[UIPopupSound] AudioManager instance not found.");
#endif
                return;
            }

            if (string.IsNullOrEmpty(openSoundId)) return;
            AudioManager.Instance.Play(openSoundId);
        }

        public void OnClose()
        {
            if (AudioManager.Instance == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[UIPopupSound] AudioManager instance not found.");
#endif
                return;
            }

            if (string.IsNullOrEmpty(closeSoundId)) return;
            AudioManager.Instance.Play(closeSoundId);
        }
    }
}
