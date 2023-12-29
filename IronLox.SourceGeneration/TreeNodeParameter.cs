﻿using System.Text;

namespace IronLox.GenerateAst;

record TreeNodeParameter(string Type, string Name, bool IsReadonly = true)
{
    public override string ToString()
    {
        var builder = new StringBuilder()
            .Append(Type)
            .Append(' ')
            .Append(Name);
        return builder.ToString();
    }
}