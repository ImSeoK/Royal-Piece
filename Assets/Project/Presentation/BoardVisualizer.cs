using UnityEngine;
using System.Collections.Generic;
using Chess.Core;
using Chess.Simulation;

namespace Chess.Presentation
{
    public class BoardVisualizer : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject tilePrefab;
        public GameObject moveHighlightPrefab;

        [Header("Tile Colors")]
        public Color lightTileColor = Color.white;
        public Color darkTileColor = new Color(0.4f, 0.4f, 0.4f);

        [Header("Highlight Colors")]
        public Color moveHighlightColor = new Color(0, 1, 0, 0.5f);
        public Color attackHighlightColor = new Color(1, 0, 0, 0.5f);
        public Color skillTargetHighlightColor = new Color(1, 0.5f, 0, 0.7f);

        private BoardState board;
        private List<GameObject> activeHighlights = new List<GameObject>();

        public void Initialize(BoardState board)
        {
            this.board = board;
        }

        public void CreateBoard()
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    GameObject tile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                    tile.name = $"Tile_{x}_{y}";
                    tile.transform.parent = transform;
                    tile.GetComponent<SpriteRenderer>().color = (x + y) % 2 == 0 ? lightTileColor : darkTileColor;
                }
            }
        }

        public void ShowHighlights(UnitState unit, List<Vector2Int> validMoves)
        {
            ClearHighlights();

            foreach (var pos in validMoves)
            {
                bool hasEnemy = board.TryGetUnit(pos, out var target) && target.ownerID != unit.ownerID;
                SpawnHighlight(pos, hasEnemy ? attackHighlightColor : moveHighlightColor, $"Highlight_{pos.x}_{pos.y}");
            }

            foreach (var target in AttackResolver.GetAttackTargets(unit, board))
            {
                if (!validMoves.Contains(target.position))
                    SpawnHighlight(target.position, new Color(1, 0, 0, 0.7f), $"AttackHighlight_{target.position.x}_{target.position.y}");
            }
        }

        // 패시브 Single 스킬 대상 선택 시 선택 가능한 유닛 하이라이트
        public void ShowSkillTargetHighlights(List<Vector2Int> validMoves, bool targetAlly, int casterOwnerID)
        {
            ClearHighlights();

            foreach (var pos in validMoves)
            {
                if (!board.TryGetUnit(pos, out var unit)) continue;
                if (!unit.IsAlive) continue;

                bool isAlly = unit.ownerID == casterOwnerID;
                if (targetAlly == isAlly)
                    SpawnHighlight(pos, skillTargetHighlightColor, $"SkillHighlight_{pos.x}_{pos.y}");
            }
        }

        public void ClearHighlights()
        {
            foreach (var h in activeHighlights)
                if (h != null) Destroy(h);
            activeHighlights.Clear();
        }

        void SpawnHighlight(Vector2Int pos, Color color, string objName)
        {
            GameObject h = Instantiate(moveHighlightPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
            h.name = objName;
            h.transform.parent = transform;
            h.GetComponent<SpriteRenderer>().color = color;
            activeHighlights.Add(h);
        }
    }
}