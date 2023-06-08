using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace NodeGraph
{


    public class NodeGraphView : GraphView
    {
        public IGraphWindow window => _window;
        private GraphWindow _window;
        protected sealed override bool canCopySelection => false;
        protected sealed override bool canCutSelection => false;
        protected sealed override bool canDuplicateSelection => false;
        protected sealed override bool canPaste => false;
        public new List<BaseNode> nodes => base.nodes.ToList().ConvertAll(x => x as BaseNode);
        public List<BaseConnection> connections => base.edges.ToList().ConvertAll(x => x as BaseConnection);
        public List<BaseGroup> groups => graphElements.ToList().Where(x => x is BaseGroup).Cast<BaseGroup>().ToList();

        public NodeGraphView(GraphWindow window, StyleSheet styleSheet)
        {
            this._window = window;
            if (styleSheet != null) styleSheets.Add(styleSheet);
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);//zoom     

            //拖拽背景
            this.AddManipulator(new ContentDragger());
            //拖拽节点
            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());


            //背景 这个需要在 uss/styleSheet 中定义GridBackground类来描述类型
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            
            nodeCreationRequest = context =>
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), window);

        }
  
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView && nodeCreationRequest != null)
            {
                evt.menu.AppendAction("Create Node", (x) =>
                {
                    nodeCreationRequest?.Invoke(new NodeCreationContext()
                    {
                        screenMousePosition = x.eventInfo.mousePosition + _window.position.position,
                        target = evt.target as VisualElement,
                        index = 0
                    });
                }, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }
        
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(x => x.direction != startPort.direction &&
            x.node != startPort.node && window.CheckCouldLink(startPort as BasePort,x as BasePort)).ToList();
        }

    }
}