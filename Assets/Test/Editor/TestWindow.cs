using NodeGraph;
using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class TestWindow : GraphWindow<TestGraph>
{


    protected override void CreateWindowView(NodeGraphView view)
    {
        view.selection.ConvertAll(x => x as BaseNode);
        var _toolBar = new Toolbar();
        _toolBar.Add(new Button(() =>
        {
            SaveGraph();
        })
        { text = "Save Data" });
        rootVisualElement.Add(_toolBar);
    }


    protected override void FitterNodeTypes(List<Type> result)
    {

    }

    protected override bool OnCheckCouldLink(BaseNode startNode, BaseNode endNode, BasePort start, BasePort end)
    {
        return start.portType == end.portType;
    }

    protected override void OnSelectNode(BaseNode node)
    {
        
    }
}
