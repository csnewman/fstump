using System;
using System.Text;

namespace FStump
{
    public class StumpWriter
    {
        private StringBuilder OutputBuilder { get; set; }

        public StumpWriter()
        {
            OutputBuilder = new StringBuilder();
        }

        public void WriteBlankLine()
        {
            OutputBuilder.AppendLine();
        }

        public void WriteComment(string msg)
        {
            OutputBuilder.AppendLine($"; {msg}");
        }

        public void WriteOrg(string pos)
        {
            OutputBuilder.AppendLine($"org {pos}");
        }

        public void WriteLabel(string name)
        {
            OutputBuilder.Append($"{name} ");
        }

        public void WriteData(string value)
        {
            OutputBuilder.AppendLine($"DEFW {value}");
        }

        public void WriteData(int value)
        {
            OutputBuilder.AppendLine($"DEFW {value}");
        }

        public void WriteNop()
        {
            OutputBuilder.AppendLine($"nop");
        }

        public void WriteAddReg(string dest, string srcA, string srcB, ShiftOp shift = ShiftOp.None, bool updateStatus = false)
        {
            WriteType1("add", dest, srcA, srcB, shift, updateStatus);
        }

        public void WriteAddCarryReg(string dest, string srcA, string srcB, ShiftOp shift = ShiftOp.None, bool updateStatus = false)
        {
            WriteType1("adc", dest, srcA, srcB, shift, updateStatus);
        }

        public void WriteSubtractReg(string dest, string srcA, string srcB, ShiftOp shift = ShiftOp.None, bool updateStatus = false)
        {
            WriteType1("sub", dest, srcA, srcB, shift, updateStatus);
        }

        public void WriteSubtractCarryReg(string dest, string srcA, string srcB, ShiftOp shift = ShiftOp.None, bool updateStatus = false)
        {
            WriteType1("sbc", dest, srcA, srcB, shift, updateStatus);
        }

        public void WriteAndReg(string dest, string srcA, string srcB, ShiftOp shift = ShiftOp.None, bool updateStatus = false)
        {
            WriteType1("and", dest, srcA, srcB, shift, updateStatus);
        }

        public void WriteOrReg(string dest, string srcA, string srcB, ShiftOp shift = ShiftOp.None, bool updateStatus = false)
        {
            WriteType1("or", dest, srcA, srcB, shift, updateStatus);
        }

        public void WriteMovReg(string dest, string srcB, ShiftOp shift = ShiftOp.None, bool updateStatus = false)
        {
            OutputBuilder.AppendLine($"mov{(updateStatus ? "s" : "")} {dest}, {srcB}{ConvertShiftOp(shift)}");
        }

        public void WriteCmpReg(string dest, string srcB, ShiftOp shift = ShiftOp.None)
        {
            OutputBuilder.AppendLine($"cmp {dest}, {srcB}{ConvertShiftOp(shift)}");
        }

        public void WriteTstReg(string dest, string srcB, ShiftOp shift = ShiftOp.None)
        {
            OutputBuilder.AppendLine($"tst {dest}, {srcB}{ConvertShiftOp(shift)}");
        }

        public void WriteNegReg(string dest, string srcB, ShiftOp shift = ShiftOp.None)
        {
            OutputBuilder.AppendLine($"neg {dest}, {srcB}{ConvertShiftOp(shift)}");
        }

        private void WriteType1(string op, string dest, string srcA, string srcB, ShiftOp shift = ShiftOp.None, bool updateStatus = false)
        {
            OutputBuilder.AppendLine($"{op}{(updateStatus ? "s" : "")} {dest}, {srcA}, {srcB}{ConvertShiftOp(shift)}");
        }

        public void WriteAddImme(string dest, string srcA, string imme, bool updateStatus = false)
        {
            WriteType2("add", dest, srcA, imme, updateStatus);
        }

        public void WriteAddCarryImme(string dest, string srcA, string imme, bool updateStatus = false)
        {
            WriteType2("adc", dest, srcA, imme, updateStatus);
        }

        public void WriteSubtractImme(string dest, string srcA, string imme, bool updateStatus = false)
        {
            WriteType2("sub", dest, srcA, imme, updateStatus);
        }

        public void WriteSubtractCarryImme(string dest, string srcA, string imme, bool updateStatus = false)
        {
            WriteType2("sbc", dest, srcA, imme, updateStatus);
        }

        public void WriteAndImme(string dest, string srcA, string imme, bool updateStatus = false)
        {
            WriteType2("and", dest, srcA, imme, updateStatus);
        }

        public void WriteOrImme(string dest, string srcA, string imme, bool updateStatus = false)
        {
            WriteType2("or", dest, srcA, imme, updateStatus);
        }

        public void WriteMovImme(string dest, string imme, bool updateStatus = false)
        {
            OutputBuilder.AppendLine($"mov{(updateStatus ? "s" : "")} {dest}, #{imme}");
        }

        public void WriteCmpImme(string dest, string imme)
        {
            OutputBuilder.AppendLine($"cmp {dest}, #{imme}");
        }

        public void WriteTstImme(string dest, string imme)
        {
            OutputBuilder.AppendLine($"tst {dest}, #{imme}");
        }

        private void WriteType2(string op, string dest, string srcA, string imme, bool updateStatus = false)
        {
            OutputBuilder.AppendLine($"{op}{(updateStatus ? "s" : "")} {dest}, {srcA}, #{imme}");
        }

        public void WriteStore(string value, string addrA, string addrB = null, ShiftOp shift = ShiftOp.None)
        {
            OutputBuilder.AppendLine($"st {value}, [{addrA}{(string.IsNullOrWhiteSpace(addrB) ? "" : $", {addrB}")}{ConvertShiftOp(shift)}]");
        }
        
        public void WriteStoreLabel(string value, string label)
        {
            OutputBuilder.AppendLine($"st {value}, {label}");
        }

        public void WriteLoad(string dest, string addrA, string addrB = null, ShiftOp shift = ShiftOp.None)
        {
            OutputBuilder.AppendLine($"ld {dest}, [{addrA}{(string.IsNullOrWhiteSpace(addrB) ? "" : $", {addrB}")}{ConvertShiftOp(shift)}]");
        }
        
        public void WriteLoadLabel(string dest, string label)
        {
            OutputBuilder.AppendLine($"ld {dest}, {label}");
        }

        public void WriteBranch(string condition, string label)
        {
            OutputBuilder.AppendLine($"b{condition} {label}");
        }

        private string ConvertShiftOp(ShiftOp op)
        {
            switch (op)
            {
                case ShiftOp.None:
                    return "";
                case ShiftOp.ArithmeticShiftRight:
                    return ", asr";
                case ShiftOp.ClockwiseCircularShift:
                    return ", ror";
                case ShiftOp.ClockwiseCircularShiftCarry:
                    return ", rrc";
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        public void WriteWriter(StumpWriter writer)
        {
            OutputBuilder.Append(writer.Write());
        }

        public string Write()
        {
            return OutputBuilder.ToString();
        }
        
        public void Print()
        {
            Console.WriteLine(OutputBuilder.ToString());
        }
        
        public enum ShiftOp
        {
            None,
            ArithmeticShiftRight, // (ASR) Copy first sign bit
            ClockwiseCircularShift, // (ROR) bit 0 moves to bit 15
            ClockwiseCircularShiftCarry // (RRC) carry moves to bit 15
        }
    }
}