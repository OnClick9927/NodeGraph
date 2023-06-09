using UnityEditor.Experimental.GraphView;
using System;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace NodeGraph
{
    public abstract class GraphNode<T> : GraphNode where T : GraphData, new()
    {
        public T data = new T();
        public override string GUID => data.guid;
        public override string NodeName => data.title;


        protected Port GeneratePort(Direction portDir, Type type, Port.Capacity capacity = Port.Capacity.Single, Orientation orientation = Orientation.Horizontal)
        {
            return GraphPort.Create(orientation, portDir, capacity, type);
        }

    }
    public abstract class GraphNode : Node
    {
        public List<GraphConnection> connections => view.connections
                    .FindAll(x => x.output.node == this || x.input.node == this);
        public Action<GraphNode> onSelected;
        public abstract string GUID { get; }
        public abstract string NodeName { get; }
        public NodeGraphView view { get; private set; }
        public List<GraphPort> ports { get { return this.view.ports.FindAll(x => x.node == this); } }






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
            if (!(evt.target is Node)) return;
            evt.menu.AppendAction("Delete", Delete, DeleteStatus);

            evt.menu.AppendAction("Disconnect all", DisconnectAll, DisconnectAllStatus);
            evt.menu.AppendAction("Remove From Group", RemoveFromGroup, RemoveFromGroupStatus);
            evt.menu.AppendSeparator();
        }

        private void Delete(DropdownMenuAction obj)
        {
            DisconnectAll(obj);
            view.DeleteElements(new List<GraphElement>() { this });
        }

        private DropdownMenuAction.Status DeleteStatus(DropdownMenuAction arg)
        {
            return DropdownMenuAction.Status.Normal;
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
}
