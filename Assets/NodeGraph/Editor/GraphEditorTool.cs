using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
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
                               .Where(item => !item.IsAbstract && item.IsSubclassOf(typeof(GraphNode)))
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



        private static GraphData Node2Data(GraphNode node)
        {
            var nodeType = node.GetType();
            var field = nodeType.GetField(nameof(GraphNode<GraphData>.data));
            GraphData data = field.GetValue(node) as GraphData;
            data.position = node.GetPosition();
            return data;
        }
        private static GroupData Group2Data(GraphGroup group)
        {
            var guids = group.containedNodes.ConvertAll(x => x.GUID);
            var data = group.data;
            data.nodes = guids;
            data.position = group.GetPosition();
            return data;
        }
        private static ConnectionData Connection2Data(GraphConnection connection)
        {
            GraphPort output = connection.output;
            GraphPort input = connection.input;
            GraphNode outputNode = output.node as GraphNode;
            GraphNode inputNode = input.node as GraphNode;
            return new ConnectionData
            {
                outNodeGuid = outputNode.GUID,
                outputPortName = output.portName,
                outPortType = output.portType.FullName,
                inPortType = input.portType.FullName,
                InNodeGuid = inputNode.GUID,
                InPortName = input.portName
            };
        }

        public static GraphGroup CreateGroup(NodeGraphView view, GroupData data)
        {
            var group = new GraphGroup(view);
            if (data != null)
            {
                group.SetData(data);
                group.SetPosition(data.position);
                group.AddElements(view.nodes.Where(x => data.nodes.Contains(x.GUID)));
            }
            view.AddElement(group);
            return group;
        }
        public static GraphNode CreateNode(Type nodeType, NodeGraphView view, GraphData nodeData)
        {
            GraphNode node = Activator.CreateInstance(nodeType) as GraphNode;
            if (nodeData != null)
            {
                var field = nodeType.GetField(nameof(GraphNode<GraphData>.data));
                field.SetValue(node, nodeData);
                node.SetPosition(nodeData.position);
            }
            node.onSelected += view.window.SelectNode;
            node?.OnCreated(view);
            view.AddElement(node);
            return node;
        }
        public static GraphConnection CreateConnection(NodeGraphView view, ConnectionData data)
        {
            var input = view.ports.Find(x => x.node.GUID == data.InNodeGuid 
                                    && x.portName == data.InPortName 
                                    && x.portType.FullName == data.inPortType);
            var output = view.ports.Find(x => x.node.GUID == data.outNodeGuid 
                                    && x.portName == data.outputPortName 
                                    && x.portType.FullName == data.outPortType);
            if (input != null && output != null)
            {
                return ConnectPort(view, input, output);
            }
            return null;
        }
        public static GraphConnection ConnectPort(NodeGraphView view, GraphPort a, GraphPort b)
        {
            GraphPort input = a.direction == Direction.Input ? a : b;
            GraphPort output = a.direction == Direction.Output ? a : b;
            var connection = new GraphConnection()
            {
                output = output,
                input = input
            };
            connection?.input.Connect(connection);
            connection?.output.Connect(connection);
            view.Add(connection);
            GraphPort.ValidConnection(view, connection);
            return connection;
        }

        public static void Save(GraphObject graph, NodeGraphView view)
        {
            graph.position = view.viewTransform.position;
            graph.scale = view.viewTransform.scale;
            var fields = GetFileds(graph);
            foreach (var item in fields)
            {
                var list = item.GetValue(graph);
                list.GetType().GetMethod("Clear").Invoke(list, null);
            }
            //保存node
            foreach (GraphNode node in view.nodes)
            {
                GraphData data = Node2Data(node);
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
            foreach (var connectedPort in view.connections.Where(x => x.input.node != null))
                graph.connections.Add(Connection2Data(connectedPort));
            foreach (var group in view.groups)
                graph.groups.Add(Group2Data(group));
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void Load(GraphObject graph, NodeGraphView view)
        {
            view.viewTransform.position = graph.position;
            view.viewTransform.scale = graph.scale;
            List<GraphData> nodeDatas = new List<GraphData>();
            var fields = GetFileds(graph);
            foreach (var item in fields)
            {
                var innerType = item.FieldType.GetGenericArguments()[0];
                if (innerType.IsSubclassOf(typeof(GraphData)) && innerType != typeof(GroupData))
                {
                    var list = item.GetValue(graph) as IEnumerable<GraphData>;
                    nodeDatas.AddRange(list);
                }
            }

            CreateElements(view, new List<GraphElement>(), nodeDatas, graph.groups, graph.connections);
        }
        public static List<GraphElement> Copy(NodeGraphView view, List<GraphElement> src)
        {
            List<GraphElement> result = new List<GraphElement>();
            Vector2 offset = Vector2.one * 100;
            var groups = src.Where(x => x is GraphGroup).Select(x => x as GraphGroup).ToList();
            var nodes = src.Where(x => x is GraphNode).Select(x => x as GraphNode).ToList();

            var connectedPorts = src.Where(x => x is GraphConnection)
                .Select(x => x as GraphConnection)
                .Where(x => nodes.Contains(x.output.node) && nodes.Contains(x.input.node))
                .ToList();
            var datas = nodes.ConvertAll(x => Node2Data(x).DeepCopy());
            var groupDatas = groups.ConvertAll(x => Group2Data(x).DeepCopy() as GroupData).Select(
                x =>
                {
                    x.position = new Rect(x.position.position + offset, x.position.size);
                    return x;
                }
                );
            var conDatas = connectedPorts.ConvertAll(x => Connection2Data(x));
            foreach (var data in datas)
            {
                string oldGuid = data.guid;
                var newGuid = Guid.NewGuid().ToString();
                var find = groupDatas.Where(x => x.nodes.Contains(oldGuid));
                foreach (var _find in find)
                {
                    _find.nodes.Remove(oldGuid);
                    _find.nodes.Add(newGuid);
                }
                var find_in = conDatas.FindAll(x => x.InNodeGuid == oldGuid);
                for (int i = 0; i < find_in.Count; i++)
                    find_in[i].InNodeGuid = newGuid;
                var find_out = conDatas.FindAll(x => x.outNodeGuid == oldGuid);
                for (int i = 0; i < find_out.Count; i++)
                    find_out[i].outNodeGuid = newGuid;
                data.guid = newGuid;
                data.position = new Rect(data.position.position + offset, data.position.size);
            }

            CreateElements(view, result, datas, groupDatas, conDatas);
            view.ClearSelection();
            foreach (var item in result)
                view.AddToSelection(item);
            return result;
        }
        private static void CreateElements(NodeGraphView view, List<GraphElement> result, IEnumerable<GraphData> nodes, IEnumerable<GroupData> groups, IEnumerable<ConnectionData> cons)
        {
            foreach (var data in nodes)
                result.Add(CreateNode(nodeDic[data.GetType()], view, data));
            foreach (var item in cons)
                result.Add(CreateConnection(view, item));
            foreach (var data in groups)
                result.Add(CreateGroup(view, data));
        }
    }
}
