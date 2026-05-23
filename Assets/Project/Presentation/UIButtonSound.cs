using UnityEngine;

namespace Chess.Presentation
{
    public class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private string soundId = "ui_button_tap";

        public void OnClick()
        {
            if (AudioManager.Instance == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[UIButtonSound] AudioManager instance not found.");
#endif
                return;
            }

            if (string.IsNullOrEmpty(soundId)) return;
            AudioManager.Instance.Play(soundId);
        }
    }
}
