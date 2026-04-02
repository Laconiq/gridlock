using System;

namespace Gridlock.Grid
{
    public readonly struct Vector2Int : IEquatable<Vector2Int>
    {
        public readonly int X;
        public readonly int Y;

        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Vector2Int other) => X == other.X && Y == other.Y;
        public override bool Equals(object? obj) => obj is Vector2Int other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(Vector2Int a, Vector2Int b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
    }
}
