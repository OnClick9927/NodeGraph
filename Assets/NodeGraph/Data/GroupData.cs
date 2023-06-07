using System.Collections.Generic;
using UnityEngine;
using System;
namespace NodeGraph
{
    [Serializable]
    public class GroupData
    {
        public List<string> nodes = new List<string>();
        public Rect position;
        public string title = "Group";

        public GroupData DeepCopy()
        {
            return new GroupData() { position = position, title = title, nodes = nodes };
        }
    }
}