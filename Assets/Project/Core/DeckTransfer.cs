using Chess.Core;
using UnityEngine;

namespace Chess.Core
{
    public enum GameMode { Normal, Ranked, AI }

    public class DeckTransfer : MonoBehaviour
    {
        public static DeckTransfer Instance { get; private set; }

        public PlayerDeck Player0Deck { get; set; }
        public PlayerDeck Player1Deck { get; set; }
        public GameMode CurrentMode   { get; set; } = GameMode.AI;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
