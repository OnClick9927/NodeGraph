﻿using System;

namespace NodeGraph
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeAttribute : System.Attribute
    {
        public string path;

        public NodeAttribute(string path)
        {
            this.path = path;
        }
    }
}
