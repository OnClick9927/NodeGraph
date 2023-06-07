using UnityEditor.Experimental.GraphView;
using System;

namespace NodeGraph
{
    public abstract class BaseNode : Node
    {
        public Action<BaseNode> onSelected;
        public abstract string GUID { get; }
        public abstract string NodeName { get; }

        public abstract void OnCreated(NodeGraphView view);
        public override void OnSelected()
        {
            base.OnSelected();
            onSelected?.Invoke(this);

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
