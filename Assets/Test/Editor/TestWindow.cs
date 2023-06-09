using NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class TestWindow : GraphWindow<TestGraph>
{
    protected override void AfterLoadGraph()
    {
        view.selection.ConvertAll(x => x as GraphNode);
        Blackboard blackboard = new Blackboard()
        {
            windowed = false,

        };
        rootVisualElement.Add(blackboard);

        var _toolBar = new Toolbar();
        _toolBar.Add(new Button(() =>
        {
            SaveGraph();
        })
        { text = "Save Data" });
        rootVisualElement.Add(_toolBar);

    }

    GraphPort port;
    protected override void FitterNodeTypes(List<Type> result, GraphElement element)
    {
        if (element is GraphPort port)
        {
            this.port = port;
            result.RemoveAll(x => port.node.GetType() != x);
        }
    }
    protected override void AfterCreateNode(GraphElement element)
    {
        if (port == null) return;
        if (element.GetType() == port.node.GetType())
        {
            if (port.direction == Direction.Input)
            {
                GraphEditorTool.ConnectPort(view, port,
                    (element as GraphNode).ports.First(x => x.direction == Direction.Output));
            }
            else
            {
                GraphEditorTool.ConnectPort(view, port,
                   (element as GraphNode).ports.First(x => x.direction == Direction.Input));
            }
        }
    }

    protected override bool OnCheckCouldLink(GraphNode startNode, GraphNode endNode, GraphPort start, GraphPort end)
    {
        return start.portType == end.portType;
    }

    protected override void OnSelectNode(GraphNode node)
    {

    }
}
