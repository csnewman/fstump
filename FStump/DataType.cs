using System;

namespace FStump
{
    public class DataType
    {
        public PrimitiveType PrimitiveType { get; }

        public int PointerDepth { get; }

        public DataType(PrimitiveType primitiveType, int pointerDepth)
        {
            PrimitiveType = primitiveType;
            PointerDepth = pointerDepth;
        }

        public int GetStackSize()
        {
            if (PointerDepth > 0)
            {
                return 2;
            }
            
            switch (PrimitiveType)
            {
                case PrimitiveType.Bool:
                case PrimitiveType.I8:
                    return 1;
                case PrimitiveType.I16:
                    return 2;
                case PrimitiveType.I32:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}