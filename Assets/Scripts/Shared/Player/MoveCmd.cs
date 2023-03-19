namespace Platformer.Shared.Player
{
    public struct MoveCmd
    {
        public float horizontalInput;
        public float verticalInput;

        public int buttons;

        public int tickNumber;
    }
}