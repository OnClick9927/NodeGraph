using NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class TestWindow : NodeGraphView<TestGraph>
{
    public TestWindow(GraphWindow window) : base(window)
    {
    }
    public override void Load(GraphObject data)
    {
        base.Load(data);
        titleContent = new UnityEngine.GUIContent("Test");
        this.selection.ConvertAll(x => x as GraphNode);
        Blackboard blackboard = new Blackboard()
        {
            windowed = false,
        };
        rootVisualElement.Add(blackboard);

        var _toolBar = new Toolbar();
        _toolBar.Add(new Button(() =>
        {
            Save();
        })
        { text = "Save Data" });
        rootVisualElement.Add(_toolBar);
    }

    protected override void AfterCreateNode(GraphElement element)
    {
        if (port == null) return;
        if (element.GetType() == port.node.GetType())
        {
            if (port.direction == Direction.Input)
            {
                ConnectPort(port, (element as GraphNode).ports.First(x => x.direction == Direction.Output));
            }
            else
            {
                ConnectPort(port, (element as GraphNode).ports.First(x => x.direction == Direction.Input));
            }
        }
    }
    GraphPort port;
    protected override List<Type> FitterNodeTypes(List<Type> src, GraphElement element)
    {
        if (element is GraphPort port)
        {
            this.port = port;
            src.RemoveAll(x => port.node.GetType() != x);
        }
        return src;
    }

    protected override bool OnCheckCouldLink(GraphNode startNode, GraphNode endNode, GraphPort start, GraphPort end)
    {
        return start.portType == end.portType;
    }

    protected override void OnSelectNode(GraphNode obj)
    {

    }
}
