using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;
using System;
using System.Linq;

namespace NodeGraph
{
    public class GraphWindow : EditorWindow, ISearchWindowProvider
    {
        [OnOpenAsset(1)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj is GraphObject)
            {
                path = AssetDatabase.GetAssetPath(obj);
                GetWindow<GraphWindow>();
            }
            return obj is GraphObject; // we did not handle the open
        }

        private GraphObject _data = null;

        private NodeGraphView view;
        private string _path;
        private static string path;

        private NodeGraphView CreateView()
        {
            var find = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(item => item.GetTypes())
                        .Where(item => !item.IsAbstract && item.BaseType != null && item.IsSubclassOf(typeof(NodeGraphView)))
                        .Where(x => x.BaseType.GetGenericArguments()[0] == _data.GetType())
                        .FirstOrDefault();
            var _view = Activator.CreateInstance(find, new Object[] { this }) as NodeGraphView;
            _view.StretchToParentSize();
            rootVisualElement.Add(_view);
            return _view;
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(path) && string.IsNullOrEmpty(_path))
                _path = path;
            _data = AssetDatabase.LoadAssetAtPath<GraphObject>(_path);
            if (_data == null) return;

            view = CreateView();
            view.Load(_data);
        }


        private void OnDisable()
        {
            if (_data == null) return;
            view.Save();
            _path = AssetDatabase.GetAssetPath(_data);
            view = null;
            _data = null;
        }

        List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context) => view.CreateSearchTree(context);

        bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context) => view.OnSelectEntry(SearchTreeEntry, context);

    }
}