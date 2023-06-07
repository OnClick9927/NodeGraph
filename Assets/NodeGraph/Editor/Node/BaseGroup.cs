using UnityEditor.Experimental.GraphView;
using static NodeGraph.GraphObject;

namespace NodeGraph
{
    public class BaseGroup : Group
    {
        public GroupData data = new GroupData();
        public BaseGroup() : base()
        {
            autoUpdateGeometry = true;
            SetData(data);
        }
        public void SetData(GroupData data)
        {
            this.data = data;
            this.title = data.title;
            this.SetPosition(data.position);
        }
        protected override void OnGroupRenamed(string oldName, string newName)
        {
            base.OnGroupRenamed(oldName, newName);
            data.title = newName;
        }
    }
}
