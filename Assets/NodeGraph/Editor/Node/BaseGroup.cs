using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeGraph
{
    public class BaseGroup : Group
    {
        public GroupData data = new GroupData();
        public BaseGroup() : base()
        {
           
            ColorField c = new ColorField();
            c.style.width = 100;
            c.style.alignSelf = Align.FlexEnd;
            this.Insert(0, c);
            SetData(data);
            c.RegisterCallback<ChangeEvent<Color>>((evt) =>
            {
                SetColor(evt.newValue);
            });
        }

        public List<BaseNode> containedNodes => this.containedElements.Where(x => x is BaseNode)
              .Cast<BaseNode>()
              .ToList();

        public void SetData(GroupData data)
        {
            this.data = data;
            this.title = data.title;
            this.SetPosition(data.position);
            SetColor(data.color);
        }

        private void SetColor(Color color)
        {
            this.Q<ColorField>().value = color;
            data.color = color;
            this.style.borderLeftWidth = 5;
            this.style.borderLeftColor = color;
        }
        protected override void OnGroupRenamed(string oldName, string newName)
        {
            base.OnGroupRenamed(oldName, newName);
            data.title = newName;
        }

   public     void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Remove Nodes", RemoveNodes, RemoveNodesStatus);
        }

        private DropdownMenuAction.Status RemoveNodesStatus(DropdownMenuAction arg)
        {
            return this.containedNodes.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }

        private void RemoveNodes(DropdownMenuAction obj)
        {
            this.Clear();
        }
    }
}
