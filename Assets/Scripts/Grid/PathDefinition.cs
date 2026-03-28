using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIWE.Grid
{
    [Serializable]
    public class PathDefinition
    {
        public int routeId;
        public List<Vector2Int> waypoints = new();
    }
}
