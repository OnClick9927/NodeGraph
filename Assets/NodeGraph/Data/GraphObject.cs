using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace NodeGraph
{
    public abstract class GraphObject : ScriptableObject
    {
        public Vector3 position;
        public Vector3 scale = Vector3.one;

        public List<ConnectionData> connections = new List<ConnectionData>();
        public List<GroupData> groups = new List<GroupData>();

        [SerializeField] private List<GraphData> graphs = new List<GraphData>();
        public void SaveNodes(List<GraphData> datas)
        {
            var fields = GetFileds();
            foreach (GraphData data in datas)
            {
                bool find = false;
                foreach (var _field in fields)
                {
                    if (_field.FieldType.GetGenericArguments()[0] == data.GetType())
                    {
                        var list = _field.GetValue(this);
                        list.GetType().GetMethod("Add").Invoke(list, new object[] { data });
                        find = true;
                        break;
                    }
                }
                if (!find)
                    throw new Exception($"Node List For Data Type : {data.GetType()} is not defined");
            }
            foreach (GraphData data in datas)
            {
                graphs.Add(data);
            }
        }
        public virtual List<GraphData> GetNodes()
        {
            List<GraphData> nodeDatas = new List<GraphData>();
            var fields = this.GetFileds();
            foreach (var item in fields)
            {
                var innerType = item.FieldType.GetGenericArguments()[0];
                if (innerType.IsSubclassOf(typeof(GraphData)) && innerType != typeof(GroupData))
                {
                    var list = item.GetValue(this) as IEnumerable<GraphData>;
                    nodeDatas.AddRange(list);
                }
            }
            return nodeDatas;
        }
        public void Clear()
        {
            graphs.Clear();
            var fields = GetFileds();
            foreach (var item in fields)
            {
                var list = item.GetValue(this);
                list.GetType().GetMethod("Clear")?.Invoke(list, null);
            }
        }
        private List<FieldInfo> GetFileds()
        {
            var types = this.GetType().GetFields().ToList();
            var find = types.FindAll(x => x.FieldType.IsGenericType &&
                x.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                .ToList();
            return find;
        }

    }
}