using UnityEngine;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    public class PlayerInfoUI : MonoBehaviour
    {
        public TextMeshProUGUI playerNameText;
        public TextMeshProUGUI recordText;

        public void UpdateInfo(PlayerData playerData)
        {
            if (playerData == null) return;

            // 이름
            if (playerNameText != null)
            {
                playerNameText.text = playerData.GetDisplayName();
            }

            // 전적
            if (recordText != null)
            {
                recordText.text = playerData.GetRecord();
            }
        }
    }
}