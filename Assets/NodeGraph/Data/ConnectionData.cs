using System;
namespace NodeGraph
{
    [Serializable]
    public class ConnectionData
    {
        public string OutNodeGUID;
        public string OutputPortName;
        public string InNodeGUID;
        public string InPortName;
    }
}