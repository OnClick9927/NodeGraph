using UnityEngine;
using System;

namespace NodeGraph
{
    public class GraphData
    {
        public Rect position;
        public string guid;
        public string title;
        public GraphData()
        {
            position = Rect.zero;
            title = GetType().Name;
            guid = Guid.NewGuid().ToString();
        }
        public virtual GraphData DeepCopy()
        {
            var type = this.GetType();
            GraphData result = Activator.CreateInstance(type) as GraphData;
            foreach (var field in type.GetFields())
            {
                field.SetValue(result, field.GetValue(this));
            }
            return result;
        }
    }

}