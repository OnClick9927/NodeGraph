﻿using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using NodeGraph;

[Node("Test/Npc")]
public class NpcNode : GraphNode<NpcNodeData>
{

    public override void OnCreated(NodeGraphView view)
    {
        base.OnCreated(view);
        title = NodeName;

        var textField = new TextField("Npc Name")
        {
            value = data.NpcName,
        };
        textField.RegisterValueChangedCallback(evt =>
        {
            data.NpcName = evt.newValue;
        });
        mainContainer.Add(textField);

        var inputPort = GeneratePort(Direction.Input, typeof(string), Port.Capacity.Single);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var outputPort = GeneratePort(Direction.Output, typeof(string));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        RefreshExpandedState();
        RefreshPorts();
    }
}


[Node("Test/My")]
public class MyNode : GraphNode<MyData>
{

    public override void OnCreated(NodeGraphView view)
    {
        base.OnCreated(view);
        title = NodeName;

        var intField = new IntegerField("Level")
        {
            value = data.NpcLevel,
        };
        intField.RegisterValueChangedCallback(evt =>
        {
            data.NpcLevel = evt.newValue;
        });
        mainContainer.Add(intField);

        var inputPort = GeneratePort(Direction.Input, typeof(int));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var outputPort = GeneratePort(Direction.Output, typeof(int));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        RefreshExpandedState();
        RefreshPorts();
    }
}
