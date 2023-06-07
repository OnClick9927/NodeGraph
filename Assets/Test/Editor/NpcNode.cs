using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using NodeGraph;

[Node("Test/Npc")]
public class NpcNode : BaseNode<NpcNodeData>
{

    public override void OnCreated(NodeGraphView view)
    {
        title = NodeName;

        var textField = new TextField("Npc Name");
        textField.RegisterValueChangedCallback(evt =>
        {
            data.NpcName = evt.newValue;
        });
        mainContainer.Add(textField);

        var intField = new IntegerField("Level");
        intField.RegisterValueChangedCallback(evt =>
        {
            data.NpcLevel = evt.newValue;
        });
        mainContainer.Add(intField);

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
public class MyNode : BaseNode<MyData>
{

    public override void OnCreated(NodeGraphView view)
    {
        title = NodeName;

        var textField = new TextField("Npc Name");
        textField.RegisterValueChangedCallback(evt =>
        {
            data.NpcName = evt.newValue;
        });
        mainContainer.Add(textField);

        var intField = new IntegerField("Level");
        intField.RegisterValueChangedCallback(evt =>
        {
            data.NpcLevel = evt.newValue;
        });
        mainContainer.Add(intField);

        var inputPort = GeneratePort(Direction.Input, typeof(int), Port.Capacity.Single);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var outputPort = GeneratePort(Direction.Output, typeof(int));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        RefreshExpandedState();
        RefreshPorts();
    }
}
