using System;
namespace NodeGraph
{
    [Serializable]
    public class ConnectionData
    {
        public string outNodeGuid;
        public string InNodeGuid;

        public string outPortType;
        public string inPortType;
        public string outputPortName;
        public string InPortName;
    }
}