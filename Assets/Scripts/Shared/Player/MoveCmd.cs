namespace Platformer.Shared.Player
{
    /// <summary>
    /// Структура данных для объекта движений игрока.
    /// </summary>
    public struct MoveCmd
    {
        public float HorizontalInput;
        public float VerticalInput;

        public int Buttons;

        public double LocalTime;

        /// <summary>
        /// Проверяет пустая ли команда.
        /// </summary>
        public bool IsValid() => HorizontalInput != 0 || VerticalInput != 0 || Buttons != 0;
    }
}