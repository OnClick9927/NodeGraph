using UnityEditor.Experimental.GraphView;

namespace NodeGraph
{
    public class BaseConnection : Edge
    {
        public new BasePort output { get { return base.output as BasePort; }set { base.output = value; } }
        public new BasePort input { get { return base.input as BasePort; } set { base.input = value; } }

   
    }
}
