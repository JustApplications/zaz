﻿using System;

namespace Zaz.Server.Advanced.Registry
{
    public class CommandInfo
    {
        public Type Type { get; set; }
        public string Key { get; set; }
        public string[] Aliases { get; set; }
        public string Description { get; set; }
        public string[] Tags { get; set; }
        public CommandParameterInfo[] Parameters { get; set; }
    }
}
