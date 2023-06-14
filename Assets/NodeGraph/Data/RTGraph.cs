using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeGraph
{
    public enum PortDirection
    {
        In, Out
    }
    public class RTPort
    {
   
        public PortDirection direction;
        public RTNode node;
        public string type;
        public string name;
        public List<RTConnection> connections = new List<RTConnection>();
    }
    public abstract class RTNode<T> : RTNode where T : GraphData
    {
        public new T data { get { return base.data as T; } set { base.data = value; } }

    }
    public class RTNode
    {
        public string guid { get { return data.guid; } }
        public GraphData data;
        public List<RTPort> ports = new List<RTPort>();
    }
    public class RTConnection
    {
        public RTPort output;
        public RTPort input;
    }
    public class RTGraph
    {
        public List<RTNode> nodes;
        private RTNode CreateNode(List<Type> types, GraphData data)
        {
            var find = types
                .Where(x => x.BaseType.GetGenericArguments()[0] == data.GetType())
                .FirstOrDefault();
            if (find == null)
                find = typeof(RTNode);
            RTNode node = Activator.CreateInstance(find) as RTNode;
            node.data = data;
            return node;
        }
        private RTPort CreateNodePort(RTNode node, string type, string name, PortDirection direction)
        {
            RTPort result = node.ports.Find(x => x.name == name && x.direction == direction && x.type == type);
            if (result == null)
            {
                result = new RTPort()
                {
                    direction = direction,
                    type = type,
                    name = name,
                    node = node
                };
                node.ports.Add(result);
            }
            return result;
        }
        public virtual void Read(GraphObject obj)
        {
            var find = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(item => item.GetTypes())
                .Where(item => !item.IsAbstract && item.BaseType != null && item.IsSubclassOf(typeof(RTNode)))
                .ToList();
            Dictionary<string, RTNode> _ndoes = new Dictionary<string, RTNode>();
            List<ConnectionData> connections = obj.connections;
            List<GraphData> nodes = obj.GetNodes();
            foreach (var item in nodes)
            {
                _ndoes.Add(item.guid, CreateNode(find, item));
            }
            foreach (var item in connections)
            {
                RTNode _out = _ndoes[item.outNodeGuid];
                RTNode _in = _ndoes[item.InNodeGuid];
                RTPort out_p = CreateNodePort(_out, item.outPortType, item.outputPortName, PortDirection.Out);
                RTPort in_p = CreateNodePort(_in, item.inPortType, item.InPortName, PortDirection.In);
                RTConnection c = new RTConnection()
                {
                    output = out_p,
                    input = in_p,
                };
                out_p.connections.Add(c);
                in_p.connections.Add(c);

            }
            this.nodes = _ndoes.Values.ToList();
        }
    }
}