using System.Collections.Generic;
using UnityEngine;
namespace NodeGraph
{
    public abstract class GraphObject : ScriptableObject
    {
        public Vector3 position;
        public Vector3 scale = Vector3.one;

        public List<ConnectionData> connections = new List<ConnectionData>();
        public List<GroupData> groups = new List<GroupData>();
    }
}