using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeGraph
{
    public abstract class NodeGraphView<T> : NodeGraphView where T : GraphObject
    {
        public new T graph { get { return base.graph as T; } }
        protected NodeGraphView(GraphWindow window) : base(window) { }
    }
    partial class NodeGraphView
    {
        private GraphElement context_target;
        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (SearchTreeEntry.userData == null) return false;


            var mousePosition = rootVisualElement.ChangeCoordinatesTo(rootVisualElement.parent,
                context.screenMousePosition - position.position);
            var graphMousePosition = this.contentViewContainer.WorldToLocal(mousePosition);

            GraphElement element;
            Type type = (Type)SearchTreeEntry.userData;
            if (type == typeof(GroupData))
            {
                element = this.CreateGroup(null);
                element.SetPosition(new Rect(graphMousePosition, element.GetPosition().size));
            }
            else
            {
                element = this.CreateNode((Type)SearchTreeEntry.userData, null);
                element.SetPosition(new Rect(graphMousePosition, element.GetPosition().size));
            }
            this.AfterCreateNode(element);
            return true;
        }
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Nodes"), 0),
            };
            var nodeTypes = this.FitterNodeTypes(nodeDic.Values.ToList(), context_target);

            for (int i = 0; i < nodeTypes.Count; i++)
            {
                var type = nodeTypes[i];
                NodeAttribute attr = type.GetCustomAttribute(typeof(NodeAttribute)) as NodeAttribute;
                if (attr == null)
                {
                    var entry = new SearchTreeEntry(new GUIContent(type.FullName))
                    {
                        level = 1,
                        userData = type
                    };
                    tree.Add(entry);
                }
                else
                {
                    var path = attr.path;
                    var sp = path.Split('/');

                    for (int j = 0; j < sp.Length; j++)
                    {
                        if (sp.Length - 1 == j)
                        {
                            if (tree.Find(x => x.name == sp[j] && x.level == j + 1) == null)
                            {
                                var entry = new SearchTreeEntry(new GUIContent(sp[j]))
                                {
                                    level = j + 1,
                                    userData = type
                                };
                                tree.Add(entry);
                            }
                            else
                            {
                                throw new Exception($"Same Node path : {path}");
                            }
                        }
                        else
                        {
                            if (tree.Find(x => x.name == sp[j] && x.level == j + 1) == null)
                            {
                                var entry = new SearchTreeGroupEntry(new GUIContent(sp[j]), j + 1);
                                tree.Add(entry);
                            }
                        }
                    }

                }
            }

            tree.Add(new SearchTreeEntry(new GUIContent("Group"))
            {
                level = 1,
                userData = typeof(GroupData)
            });
            return tree;
        }
        static Dictionary<Type, Type> nodeDic = new Dictionary<Type, Type>();

        [InitializeOnLoadMethod]
        static void GraphEditorTool()
        {
            var find = AppDomain.CurrentDomain.GetAssemblies()
                               .SelectMany(item => item.GetTypes())
                               .Where(item => !item.IsAbstract && item.IsSubclassOf(typeof(GraphNode)))
                               .Select(x => new { dataType = x.BaseType.GetGenericArguments()[0], node = x });

            nodeDic = find.ToDictionary(x => x.dataType, x => x.node);

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

        public GraphGroup CreateGroup(GroupData data)
        {
            var group = new GraphGroup(this);
            if (data != null)
            {
                group.SetData(data);
                group.SetPosition(data.position);
                group.AddElements(this.nodes.Where(x => data.nodes.Contains(x.GUID)));
            }
            this.AddElement(group);
            return group;
        }
        public GraphNode CreateNode(Type nodeType, GraphData nodeData)
        {
            GraphNode node = Activator.CreateInstance(nodeType) as GraphNode;
            if (nodeData != null)
            {
                var field = nodeType.GetField(nameof(GraphNode<GraphData>.data));
                field.SetValue(node, nodeData);
                node.SetPosition(nodeData.position);
            }
            node.onSelected += this.OnSelectNode;
            node?.OnCreated(this);
            this.AddElement(node);
            return node;
        }

        public GraphConnection CreateConnection(ConnectionData data)
        {
            var input = this.ports.Find(x => x.node.GUID == data.InNodeGuid
                                    && x.portName == data.InPortName
                                    && x.portType.FullName == data.inPortType);
            var output = this.ports.Find(x => x.node.GUID == data.outNodeGuid
                                    && x.portName == data.outputPortName
                                    && x.portType.FullName == data.outPortType);
            if (input != null && output != null)
            {
                return ConnectPort(input, output);
            }
            return null;
        }
        public GraphConnection ConnectPort(GraphPort a, GraphPort b)
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
            this.Add(connection);
            GraphPort.ValidConnection(this, connection);
            return connection;
        }

        public void Duplicate()
        {
            this.Duplicate(selection.ConvertAll(x => x as GraphElement));
        }
        public List<GraphElement> Duplicate(List<GraphElement> src)
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

            CreateElements(result, datas, groupDatas, conDatas);
            this.ClearSelection();
            foreach (var item in result)
                this.AddToSelection(item);
            return result;
        }
        private void CreateElements(List<GraphElement> result, IEnumerable<GraphData> nodes, IEnumerable<GroupData> groups, IEnumerable<ConnectionData> cons)
        {
            foreach (var data in nodes)
                result.Add(CreateNode(nodeDic[data.GetType()], data));
            foreach (var item in cons)
                result.Add(CreateConnection(item));
            foreach (var data in groups)
                result.Add(CreateGroup(data));
        }
    }
    public abstract partial class NodeGraphView : GraphView
    {
        protected GraphObject graph;
        private GraphWindow window { get; set; }
        protected sealed override bool canCopySelection => false;
        protected sealed override bool canCutSelection => false;
        protected sealed override bool canDuplicateSelection => false;
        protected sealed override bool canPaste => false;

        public VisualElement rootVisualElement => window.rootVisualElement;
        public Rect position => window.position;
        public new List<GraphPort> ports => base.ports.ToList().ConvertAll(x => x as GraphPort);

        public new List<GraphNode> nodes => base.nodes.ToList().ConvertAll(x => x as GraphNode);
        public List<GraphConnection> connections => base.edges.ToList().ConvertAll(x => x as GraphConnection);
        public List<GraphGroup> groups => graphElements.ToList().Where(x => x is GraphGroup).Cast<GraphGroup>().ToList();
        public List<GraphNode> selectedNodes { get { return selection.Where(x => x is GraphNode).Select(x => x as GraphNode).ToList(); } }
        protected GUIContent titleContent
        {
            get => window.titleContent;
            set
            {
                window.titleContent = value;
            }
        }

        public NodeGraphView(GraphWindow window)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/NodeGraph/Editor/NodeGraphView.uss");
            this.window = window;
            if (styleSheet != null) styleSheets.Add(styleSheet);
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);//zoom     

            //拖拽背景
            this.AddManipulator(new ContentDragger());
            //拖拽节点
            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            //背景 这个需要在 uss/styleSheet 中定义GridBackground类来描述类型
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            nodeCreationRequest = context =>
            {
                context_target = context.target as GraphElement;
                if (context.screenMousePosition == Vector2.zero)
                    SearchWindow.Open(new SearchWindowContext(Event.current.mousePosition + window.position.position), window);
                else
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), window);
            };

        }


        public void OpenSearchPop(VisualElement target, Vector2 position)
        {
            nodeCreationRequest?.Invoke(new NodeCreationContext
            {
                target = target,
                index = 0,
                screenMousePosition = position
            });
        }

        private DropdownMenuAction.Status DeleteSelectionStutas(DropdownMenuAction arg)
        {
            return selection.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList()
                .Where(x =>
                    x.direction != startPort.direction &&
                    x.node != (startPort as GraphPort).node
                    && OnCheckCouldLink((startPort as GraphPort).node, x.node, startPort as GraphPort, x)
                )
                .ToList()
                .ConvertAll(x => x as Port);
        }


        public virtual void Save()
        {
            graph.position = this.viewTransform.position;
            graph.scale = this.viewTransform.scale;
            graph.Clear();
            //保存node
            graph.SaveNodes(this.nodes.ConvertAll(x => Node2Data(x)));
            foreach (var connectedPort in this.connections.Where(x => x.input.node != null))
                graph.connections.Add(Connection2Data(connectedPort));
            foreach (var group in this.groups)
                graph.groups.Add(Group2Data(group));
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        public virtual void Load(GraphObject data)
        {
            this.graph = data;
            this.viewTransform.position = graph.position;
            this.viewTransform.scale = graph.scale;
            CreateElements(new List<GraphElement>(), graph.GetNodes(), graph.groups, graph.connections);
            this.RegisterCallback<KeyDownEvent>(KeyDownCallback);

        }
        protected virtual void KeyDownCallback(KeyDownEvent evt)
        {
            if (evt.commandKey || evt.ctrlKey)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.S:
                        Save();
                        evt.StopImmediatePropagation();
                        break;
                    case KeyCode.D:
                        Duplicate();
                        evt.StopImmediatePropagation();
                        break;
                }
            }

        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphConnection)
            {
                GraphConnection con = (GraphConnection)evt.target;
                evt.menu.AppendAction("Delete", (x) =>
                {
                    this.DeleteElements(new List<GraphElement>() { con });
                }, DropdownMenuAction.AlwaysEnabled);
            }
            if (!(evt.target is NodeGraphView)) return;
            if (nodeCreationRequest != null)
            {
                evt.menu.AppendAction("Create Node", (x) =>
                {
                    OpenSearchPop(null, x.eventInfo.mousePosition + window.position.position);

                }, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }
            evt.menu.AppendAction("Delete Selection", (x) =>
            {
                this.DeleteSelection();
            }, DeleteSelectionStutas);
        }
        protected abstract bool OnCheckCouldLink(GraphNode startNode, GraphNode endNode, GraphPort start, GraphPort end);
        protected abstract void OnSelectNode(GraphNode obj);
        protected abstract void AfterCreateNode(GraphElement element);
        protected abstract List<Type> FitterNodeTypes(List<Type> src, GraphElement element);


    }
}