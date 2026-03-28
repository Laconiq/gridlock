using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gridlock.Grid
{
    [Serializable]
    public class PathDefinition
    {
        public int routeId;
        public List<Vector2Int> waypoints = new();
    }
}
