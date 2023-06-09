using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeGraph
{
    public class NodeGraphView : GraphView
    {
        public IGraphWindow window { get; set; }
        protected sealed override bool canCopySelection => false;
        protected sealed override bool canCutSelection => false;
        protected sealed override bool canDuplicateSelection => false;
        protected sealed override bool canPaste => false;
        public new List<GraphPort> ports => base.ports.ToList().ConvertAll(x => x as GraphPort);

        public new List<GraphNode> nodes => base.nodes.ToList().ConvertAll(x => x as GraphNode);
        public List<GraphConnection> connections => base.edges.ToList().ConvertAll(x => x as GraphConnection);
        public List<GraphGroup> groups => graphElements.ToList().Where(x => x is GraphGroup).Cast<GraphGroup>().ToList();
        public List<GraphNode> selectedNodes { get { return selection.Where(x => x is GraphNode).Select(x => x as GraphNode).ToList(); } }

        public NodeGraphView(GraphWindow window, StyleSheet styleSheet)
        {
            this.window = window;
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
            {
                this.window.CollectNodeTypes(context);
                if (context.screenMousePosition == Vector2.zero)
                    SearchWindow.Open(new SearchWindowContext(Event.current.mousePosition + window.position.position), window);
                else
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), window);

            };

        }
        public void OpenSearchPop(VisualElement target, Vector2 position)
        {
            nodeCreationRequest?.Invoke(new NodeCreationContext
            {
                target = target,
                index = 0,
                screenMousePosition = position
            });
        }
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphConnection)
            {
                GraphConnection con = (GraphConnection)evt.target;
                evt.menu.AppendAction("Delete", (x) =>
                {
                    this.DeleteElements(new List<GraphElement>() { con });
                }, DropdownMenuAction.AlwaysEnabled);
            }
            if (!(evt.target is NodeGraphView)) return;
            if (nodeCreationRequest != null)
            {
                evt.menu.AppendAction("Create Node", (x) =>
                {
                    OpenSearchPop(null, x.eventInfo.mousePosition + window.position.position);

                }, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }
            evt.menu.AppendAction("Delete Selection", (x) =>
            {
                this.DeleteSelection();
            }, DeleteSelectionStutas);
        }

        private DropdownMenuAction.Status DeleteSelectionStutas(DropdownMenuAction arg)
        {
            return selection.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }


        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList()
                .Where(x =>
                    x.direction != startPort.direction &&
                    x.node != (startPort as GraphPort).node

                    && window.CheckCouldLink(startPort as GraphPort, x)
                )
                .ToList()
                .ConvertAll(x => x as Port);
        }

    }
}