using System;

namespace Chess.Core
{
    [Flags]
    public enum MovementAttribute
    {
        None = 0,
        Pawn = 1 << 0,      // 1
        Rook = 1 << 1,      // 2
        Bishop = 1 << 2,    // 4
        Knight = 1 << 3,    // 8
        King = 1 << 4,      // 16
        Enchanter = 1 << 5,
        Queen = Rook | Bishop  // 6 (2 + 4)
    }
}