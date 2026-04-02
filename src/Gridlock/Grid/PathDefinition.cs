using System.Collections.Generic;

namespace Gridlock.Grid
{
    public sealed class PathDefinition
    {
        public int RouteId { get; set; }
        public List<Vector2Int> Waypoints { get; set; } = new();
    }
}
