using System;
using System.Collections.Generic;
using System.Linq;

namespace FStump
{
    public class StackFrame
    {
        private readonly Dictionary<string, StackVariable> _variables;
//        private readonly List<string> _order;
        private int _currentOffset;

        public StackFrame()
        {
            _currentOffset = 0;
            _variables = new Dictionary<string, StackVariable>();
//            _order = new List<string>();
        }

        public void AddVariable(string name, DataType type)
        {
            if (_variables.ContainsKey(name))
            {
                throw new ArgumentException($"Name {name} is already in use", nameof(name));
            }

            _variables.Add(name, new StackVariable(name, type, _currentOffset));
//            _order.Add(name);
            _currentOffset += type.GetStackSize();
        }

        public void WriteFrame(StumpWriter writer)
        {
            writer.WriteComment("Allocate stackframe");

//            writer.WriteMovReg("r1", FStumpCompiler.SP);

//            foreach (var variable in _order.Select(name => _variables[name]))
//            {
//                writer.WriteComment($"Variable {variable.Name}");
//
//                for (var i = 0; i < variable.Type.GetStackSize(); i++)
//                {
//                    writer.WriteStore("r0", "r1", $"#{i}");
//                }
//
//                writer.WriteAddImme("r1", "r1", variable.Type.GetStackSize().ToString());
//            }

//            writer.WriteMovReg(FStumpCompiler.SP, "r1");

            writer.WriteMovImme(FStumpCompiler.G1, _currentOffset.ToString());
            writer.WriteAddReg(FStumpCompiler.SP, FStumpCompiler.SP, FStumpCompiler.G1);
        }

        private class StackVariable
        {
            public string Name { get; }

            public DataType Type { get; }

            public int Offset { get; }

            public StackVariable(string name, DataType type, int offset)
            {
                Name = name;
                Type = type;
                Offset = offset;
            }
        }
    }
}