using System.Collections.Generic;
using UnityEngine;
using System;
namespace NodeGraph
{
    [Serializable]
    public class GroupData : BaseNodeData
    {
        public List<string> nodes = new List<string>();
        public override BaseNodeData DeepCopy()
        {
            return new GroupData() { position = position, title = title, nodes = nodes };
        }
    }
}