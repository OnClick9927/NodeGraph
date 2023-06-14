using NodeGraph;
using System;

[Serializable]
public class NpcNodeData : GraphData
{
    public string NpcName;
}

[Serializable]
public class MyData : GraphData
{
    public int NpcLevel;
}
