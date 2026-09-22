using System.Numerics;
using Raylib_cs;

namespace Gridlock.Input
{
    public sealed class InputManager
    {
        public Vector2 MouseScreenPos { get; private set; }
        public Vector2 MouseDelta { get; private set; }
        public float ScrollDelta { get; private set; }
        public bool LeftClicked { get; private set; }
        public bool RightClicked { get; private set; }
        public bool MiddleHeld { get; private set; }
        public bool SpacePressed { get; private set; }
        public bool EscapePressed { get; private set; }

        public void Poll()
        {
            MouseScreenPos = Raylib.GetMousePosition();
            MouseDelta = Raylib.GetMouseDelta();
            ScrollDelta = Raylib.GetMouseWheelMove();
            LeftClicked = Raylib.IsMouseButtonPressed(MouseButton.Left);
            RightClicked = Raylib.IsMouseButtonPressed(MouseButton.Right);
            MiddleHeld = Raylib.IsMouseButtonDown(MouseButton.Middle);
            SpacePressed = Raylib.IsKeyPressed(KeyboardKey.Space);
            EscapePressed = Raylib.IsKeyPressed(KeyboardKey.Escape);
        }
    }
}
