using System.Collections.Generic;
using UnityEngine;
using System;
namespace NodeGraph
{
    [Serializable]
    public class GroupData : GraphData
    {
        public Color color = Color.white;
        public List<string> nodes = new List<string>();
        public GroupData() : base()
        {
            title = "Group";
        }
        public override GraphData DeepCopy()
        {
            return new GroupData() { position = position, title = title, nodes = nodes, color = color };
        }
    }
}