using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System;
using System.Reflection;
using UnityEngine;

namespace NodeGraph
{
    [InitializeOnLoad]
    public static class GraphEditorTool
    {
        public static List<Type> NodeTypes { get { return nodeDic.Values.ToList(); } }
        static Dictionary<Type, Type> nodeDic = new Dictionary<Type, Type>();
        static GraphEditorTool()
        {
            var find = AppDomain.CurrentDomain.GetAssemblies()
                               .SelectMany(item => item.GetTypes())
                               .Where(item => !item.IsAbstract && item.IsSubclassOf(typeof(BaseNode)))
                               .Select(x => new { dataType = x.BaseType.GetGenericArguments()[0], node = x });

            nodeDic = find.ToDictionary(x => x.dataType, x => x.node);

        }

        private static List<FieldInfo> GetFileds(GraphObject graph)
        {
            var types = graph.GetType().GetFields().ToList();
            var find = types.FindAll(x => x.FieldType.IsGenericType &&
                x.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                .ToList();
            return find;
        }

        private static BaseNodeData Node2Data(BaseNode node)
        {
            var nodeType = node.GetType();
            var field = nodeType.GetField(nameof(BaseNode<BaseNodeData>.data));
            BaseNodeData data = field.GetValue(node) as BaseNodeData;
            data.position = node.GetPosition();
            return data;
        }
        private static GroupData Group2Data(BaseGroup group)
        {
            var guids = group.containedElements.Where(x => x is BaseNode)
              .Cast<BaseNode>()
              .Select(x => x.GUID)
              .ToList();
            var data = group.data;
            data.nodes = guids;
            return data;
        }
        private static ConnectionData Connection2Data(BaseConnection connection)
        {
            BasePort output = connection.output;
            BasePort input = connection.input;
            BaseNode outputNode = output.node as BaseNode;
            BaseNode inputNode = input.node as BaseNode;
            return new ConnectionData
            {
                OutNodeGUID = outputNode.GUID,
                OutputPortName = output.portName,
                InNodeGUID = inputNode.GUID,
                InPortName = input.portName
            };
        }
        public static void Save(GraphObject graph, NodeGraphView view)
        {
            graph.position = view.viewTransform.position;
            graph.scale = view.viewTransform.scale;

            var edges = view.connections;
            var nodes = view.nodes;
            var fields = GetFileds(graph);
            foreach (var item in fields)
            {
                var list = item.GetValue(graph);
                list.GetType().GetMethod("Clear").Invoke(list, null);
            }
            //保存node
            foreach (BaseNode node in nodes)
            {
                BaseNodeData data = Node2Data(node);
                bool find = false;
                foreach (var _field in fields)
                {
                    if (_field.FieldType.GetGenericArguments()[0] == data.GetType())
                    {
                        var list = _field.GetValue(graph);
                        list.GetType().GetMethod("Add").Invoke(list, new object[] { data });
                        find = true;
                        break;
                    }
                }
                if (!find)
                    throw new Exception($"Node List For Data Type : {data.GetType()} is not defined");
            }

            //保存link
            var connectedPorts = edges?.Where(x => x.input.node != null)?.ToArray();
            if (connectedPorts != null)
            {
                for (int i = 0; i < connectedPorts.Length; i++)
                {
                    graph.connections.Add(Connection2Data(connectedPorts[i]));
                }
            }

            //保存组
            var groups = view.groups;
            foreach (var group in groups)
            {
                var data = Group2Data(group);
                graph.groups.Add(data);
            }

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        public static BaseGroup CreateGroup(NodeGraphView view, GroupData data)
        {
            var group = new BaseGroup();
            if (data != null)
                group.SetData(data);
            view.AddElement(group);
            return group;
        }
        public static BaseNode CreateNode(Type nodeType, NodeGraphView view, BaseNodeData nodeData)
        {
            BaseNode node = Activator.CreateInstance(nodeType) as BaseNode;
            if (nodeData != null)
            {
                var field = nodeType.GetField(nameof(BaseNode<BaseNodeData>.data));
                field.SetValue(node, nodeData);
                node.SetPosition(nodeData.position);
            }
            node.onSelected += view.window.SelectNode;
            node?.OnCreated(view);
            view.AddElement(node);
            return node;
        }

        public static void CreateConnection(NodeGraphView view, List<ConnectionData> links,List<GraphElement> result)
        {
           
            var nodes = view.nodes;
            foreach (BaseNode node in nodes)
            {
                var nodeType = node.GetType();
                var connections = links?.Where(x => x.OutNodeGUID == node.GUID)?.ToList();
                if (connections == null) continue;
                foreach (ConnectionData con in connections)
                {
                    var targetNode = nodes.First(x => x.GUID == con.InNodeGUID);

                    var outputPort = node.outputContainer.Query<BasePort>().ToList().First(p =>
                        p.portName.Equals(con.OutputPortName));
                    var targetPort = targetNode.inputContainer.Query<BasePort>().ToList().First(p =>
                        p.portName.Equals(con.InPortName));
                    var tempEdge = new BaseConnection()
                    {
                        output = outputPort,
                        input = targetPort
                    };
                    tempEdge?.input.Connect(tempEdge);
                    tempEdge?.output.Connect(tempEdge);
                    view.Add(tempEdge);
                    result.Add(tempEdge);
                }

            }
        }
        public static void Load(GraphObject graph, NodeGraphView view)
        {
            view.viewTransform.position = graph.position;
            view.viewTransform.scale = graph.scale;

            List<BaseNodeData> nodeDatas = new List<BaseNodeData>();
            var fields = GetFileds(graph);
            foreach (var item in fields)
            {
                var innerType = item.FieldType.GetGenericArguments()[0];
                if (innerType.IsSubclassOf(typeof(BaseNodeData)) && innerType!= typeof(GroupData))
                {
                    var list = item.GetValue(graph) as IEnumerable<BaseNodeData>;
                    nodeDatas.AddRange(list);
                }
            }
            foreach (BaseNodeData nodeData in nodeDatas)
            {
                var nodeType = nodeDic[nodeData.GetType()];
                CreateNode(nodeType, view, nodeData);
            }

            //加载link
            CreateConnection(view, graph.connections,new List<GraphElement>());
            var nodes = view.nodes;

            //加载组
            foreach (var group in graph.groups)
            {
                var block = CreateGroup(view, group);
                block.AddElements(nodes.Where(x => group.nodes.Contains(x.GUID)));
            }


        }


        public static List<GraphElement> Copy(NodeGraphView view, List<GraphElement> src)
        {
            List<GraphElement> result = new List<GraphElement>();
            Vector2 offset = Vector2.one * 100;
            var groups = src.Where(x => x is BaseGroup).Select(x => x as BaseGroup).ToList();
            var nodes = src.Where(x => x is BaseNode).Select(x => x as BaseNode).ToList();

            var connectedPorts = src.Where(x => x is BaseConnection)
                .Select(x => x as BaseConnection)
                .Where(x => nodes.Contains(x.output.node) && nodes.Contains(x.input.node))
                .ToList();
            var datas = nodes.ConvertAll(x => Node2Data(x).DeepCopy());
            var groupDatas = groups.ConvertAll(x => Group2Data(x).DeepCopy() as GroupData);
            var conDatas = connectedPorts.ConvertAll(x => Connection2Data(x));


            foreach (var data in datas)
            {
                var nodeType = nodeDic[data.GetType()];
                string oldGuid = data.GUID;
                var newGuid = Guid.NewGuid().ToString();
                var find = groupDatas.FindAll(x => x.nodes.Contains(oldGuid));
                foreach (var _find in find)
                {
                    _find.nodes.Remove(oldGuid);
                    _find.nodes.Add(newGuid);
                }
                var find_in = conDatas.FindAll(x => x.InNodeGUID == oldGuid);
                for (int i = 0; i < find_in.Count; i++)
                    find_in[i].InNodeGUID = newGuid;
                var find_out = conDatas.FindAll(x => x.OutNodeGUID == oldGuid);
                for (int i = 0; i < find_out.Count; i++)
                    find_out[i].OutNodeGUID = newGuid;


                data.GUID = newGuid;
                data.position = new Rect(data.position.position + offset, data.position.size);
                var node = CreateNode(nodeType, view, data);
                result.Add(node);
            }
            nodes = view.nodes;
            CreateConnection(view, conDatas, result);

            foreach (var data in groupDatas)
            {
                data.position = new Rect(data.position.position + offset, data.position.size);
                var block = CreateGroup(view, data);
                block.AddElements(nodes.Where(x => data.nodes.Contains(x.GUID)));
                result.Add(block);
            }
            view.ClearSelection();
            foreach (var item in result)
                view.AddToSelection(item);
            return result;



        }
    }
}
