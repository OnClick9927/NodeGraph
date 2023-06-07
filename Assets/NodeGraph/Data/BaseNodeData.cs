using UnityEngine;
using System;

namespace NodeGraph
{
    public class BaseNodeData
    {
        public Rect position;
        public string GUID;
        public string title;
        public BaseNodeData()
        {
            position = Rect.zero;
            title = GetType().Name;
            GUID = Guid.NewGuid().ToString();
        }
        public virtual BaseNodeData DeepCopy()
        {
            var type = this.GetType();
            BaseNodeData result = Activator.CreateInstance(type) as BaseNodeData;
            foreach (var field in type.GetFields())
            {
                field.SetValue(result, field.GetValue(this));
            }
            return result;
        }
    }

}