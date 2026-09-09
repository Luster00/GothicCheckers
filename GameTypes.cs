namespace GothicCheckers;

// Перечисление двух игровых сторон.
public enum PlayerColor { White, Black }

// Класс, описывающий одну игровую шашку.
public sealed class Piece
{
    // Цвет шашки определяет её принадлежность игроку.
    public PlayerColor Color { get; }
    // Признак дамки. Дамка обладает расширенными возможностями хода и взятия.
    public bool IsKing { get; set; }

    public Piece(PlayerColor color, bool king = false)
    {
        Color = color;
        IsKing = king;
    }

    // Создание независимой копии шашки для анализа позиции компьютером.
    public Piece Clone() => new(Color, IsKing);
}

// Структура для хранения координат клетки игрового поля.
public readonly record struct Pos(int Row, int Col)
{
    // Проверка, находятся ли координаты внутри доски 8×8.
    public bool Inside => Row is >= 0 and < 8 && Col is >= 0 and < 8;
}

// Класс, описывающий один возможный ход.
public sealed class Move
{
    // Исходная позиция шашки.
    public Pos From { get; }
    // Конечная позиция шашки.
    public Pos To { get; }
    // Координаты побитой шашки. Если значение отсутствует, ход обычный.
    public Pos? Captured { get; }

    // Удобная проверка: является ли ход взятием.
    public bool IsCapture => Captured.HasValue;

    public Move(Pos from, Pos to, Pos? captured = null)
    {
        From = from;
        To = to;
        Captured = captured;
    }
}
