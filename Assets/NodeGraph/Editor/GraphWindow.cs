using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;
using System;
using System.Linq;
using System.Reflection;
using static NodeGraph.GraphObject;

namespace NodeGraph
{

    public abstract class GraphWindow<T> : GraphWindow { }
    public interface IGraphWindow
    {
        bool CheckCouldLink(BasePort start, BasePort end);
        void SelectNode(BaseNode node);
    }
    public abstract class GraphWindow : EditorWindow, ISearchWindowProvider, IGraphWindow
    {
        [OnOpenAsset(1)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj is GraphObject)
            {
                var find = AppDomain.CurrentDomain.GetAssemblies()
                          .SelectMany(item => item.GetTypes())
                          .Where(item => !item.IsAbstract && item.IsSubclassOf(typeof(GraphWindow)))
                          .Where(x => x.BaseType.GetGenericArguments()[0] == obj.GetType())
                          .FirstOrDefault();

                GraphObject graph = (GraphObject)obj;
                path = AssetDatabase.GetAssetPath(graph);
                if (find != null)
                    GetWindow(find);
                return find != null;
            }
            return obj is GraphObject; // we did not handle the open
        }

        private NodeGraphView _view = null;
        private GraphObject _data = null;
        private string _path;
        private static string path;
        public List<GraphElement> selection { get { return _view.selection.ConvertAll(x => x as GraphElement); } }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(path))
                _path = path;

            _data = AssetDatabase.LoadAssetAtPath<GraphObject>(_path);
            CreateView();
            GraphEditorTool.Load(_data, _view);
            CreateWindowView(_view);
            _view.RegisterCallback<KeyDownEvent>(KeyDownCallback);


        }

        private void KeyDownCallback(KeyDownEvent evt)
        {
            if (evt.commandKey || evt.ctrlKey)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.S:
                        SaveGraph();
                        evt.StopImmediatePropagation();
                        break;
                    case KeyCode.D:
                        Duplicate();
                        evt.StopImmediatePropagation();
                        break;
                }
            }

        }

        private void Duplicate()
        {
            GraphEditorTool.Copy(_view, selection);
        }

        private void CreateView()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/NodeGraph/Editor/NodeGraphView.uss");
            _view = new NodeGraphView(this, styleSheet);
            _view.StretchToParentSize();
            rootVisualElement.Add(_view);
        }


        protected abstract void CreateWindowView(NodeGraphView view);
        protected void SaveGraph()
        {
            GraphEditorTool.Save(_data, _view);
        }
        private void OnDisable()
        {
            SaveGraph();
            _path = AssetDatabase.GetAssetPath(_data);
            _view = null;
            _data = null;
        }






        protected abstract void FitterNodeTypes(List<Type> result);
        List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
                new SearchTreeEntry(new GUIContent("Group"))
                {
                    level = 1,
                    userData=typeof(GroupData)
                },

            };
            var nodeTypes = GraphEditorTool.NodeTypes;
            FitterNodeTypes(nodeTypes);
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
            return tree;
        }

        bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (SearchTreeEntry.userData == null) return false;


            var mousePosition = rootVisualElement.ChangeCoordinatesTo(rootVisualElement.parent,
                context.screenMousePosition - position.position);
            var graphMousePosition = _view.contentViewContainer.WorldToLocal(mousePosition);

            Type type = (Type)SearchTreeEntry.userData;
            if (type == typeof(GroupData))
            {
                var node = GraphEditorTool.CreateGroup(_view, null);
                node.SetPosition(new Rect(graphMousePosition, node.GetPosition().size));
            }
            else
            {
                var node = GraphEditorTool.CreateNode((Type)SearchTreeEntry.userData, _view, null);
                node.SetPosition(new Rect(graphMousePosition, node.GetPosition().size));
            }



            return true;
        }
        void IGraphWindow.SelectNode(BaseNode node)
        {
            OnSelectNode(node);
        }

        bool IGraphWindow.CheckCouldLink(BasePort start, BasePort end)
        {
            return OnCheckCouldLink(start.node as BaseNode, end.node as BaseNode, start, end);
        }
        protected abstract bool OnCheckCouldLink(BaseNode startNode, BaseNode endNode, BasePort start, BasePort end);
        protected abstract void OnSelectNode(BaseNode node);
    }
}