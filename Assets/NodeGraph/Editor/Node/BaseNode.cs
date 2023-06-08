using UnityEditor.Experimental.GraphView;
using System;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{

    public abstract class BaseNode : Node
    {
        public Action<BaseNode> onSelected;
        public abstract string GUID { get; }
        public abstract string NodeName { get; }
        public NodeGraphView view { get; private set; }
        public virtual void OnCreated(NodeGraphView view)
        {
            this.view = view;
        }
        public override void OnSelected()
        {
            base.OnSelected();
            onSelected?.Invoke(this);

        }



        private void AddConnectionsToDeleteSet(VisualElement container, ref HashSet<GraphElement> toDelete)
        {
            List<GraphElement> toDeleteList = new List<GraphElement>();
            container.Query<Port>().ForEach(delegate (Port elem)
            {
                if (elem.connected)
                {
                    foreach (Edge connection in elem.connections)
                    {
                        if ((connection.capabilities & Capabilities.Deletable) != 0)
                        {
                            toDeleteList.Add(connection);
                        }
                    }
                }
            });
            toDelete.UnionWith(toDeleteList);
        }
        private void DisconnectAll(DropdownMenuAction a)
        {
            HashSet<GraphElement> toDelete = new HashSet<GraphElement>();
            AddConnectionsToDeleteSet(inputContainer, ref toDelete);
            AddConnectionsToDeleteSet(outputContainer, ref toDelete);
            toDelete.Remove(null);
            if (view != null)
                view.DeleteElements(toDelete);
        }
        private DropdownMenuAction.Status DisconnectAllStatus(DropdownMenuAction a)
        {

            VisualElement[] array = new VisualElement[2] { inputContainer, outputContainer };
            VisualElement[] array2 = array;
            foreach (VisualElement e in array2)
            {
                List<Port> list = e.Query<Port>().ToList();
                foreach (Port item in list)
                {
                    if (item.connected)
                    {
                        return DropdownMenuAction.Status.Normal;
                    }
                }
            }

            return DropdownMenuAction.Status.Disabled;
        }
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Disconnect all", DisconnectAll, DisconnectAllStatus);
            evt.menu.AppendAction("Remove From Group", RemoveFromGroup, RemoveFromGroupStatus);
            evt.menu.AppendSeparator();
        }

        private DropdownMenuAction.Status RemoveFromGroupStatus(DropdownMenuAction arg)
        {
            return view.groups.Find(x => x.containedNodes.Contains(this)) != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }

        private void RemoveFromGroup(DropdownMenuAction obj)
        {
            view.groups.Find(x => x.containedNodes.Contains(this)).RemoveElement(this);
        }
    }
    public abstract class BaseNode<T> : BaseNode where T : BaseNodeData, new()
    {
        public T data = new T();
        public override string GUID => data.GUID;
        public override string NodeName => data.title;


        protected Port GeneratePort(Direction portDir, Type type, Port.Capacity capacity = Port.Capacity.Single)
        {
            return BasePort.Create(Orientation.Horizontal, portDir, capacity, type);
        }

    }
}
