using NodeGraph;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TestGraph : GraphObject
{
    public List<NpcNodeData> _npcNodeDatas = new List<NpcNodeData>();
    public List<MyData> _my = new List<MyData>();

}
