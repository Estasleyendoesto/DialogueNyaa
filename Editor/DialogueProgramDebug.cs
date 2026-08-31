#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using EEsto.DialogueNyaa;

namespace EEsto.EditorTools
{
    public static class DialogueProgramDebug
    {
        public static string Dump(this DialogueProgram p)
        {
            var rev = new Dictionary<int, string>();
            foreach (var kv in p.Labels) rev[kv.Value] = kv.Key;

            var sb = new StringBuilder();

            for (int i = 0; i < p.Code.Count; i++)
            {
                if (rev.TryGetValue(i, out string label))
                    sb.AppendLine().AppendLine($"== {label} ==");

                Instruction c = p.Code[i];
                sb.Append(i.ToString("000")).Append("  ").Append(c.Op.ToString().PadRight(12));

                switch (c.Op)
                {
                    case OpCode.Say:
                        if (!string.IsNullOrEmpty(c.Line.Speaker)) sb.Append(c.Line.Speaker).Append(": ");
                        sb.Append(Cut(c.Line.Text)).Append("   [").Append(c.Line.LocKey).Append(']');
                        break;

                    case OpCode.Choice:
                        int optionCount =
                            c.Options?.Length ?? 0;

                        sb.Append(optionCount)
                            .Append(" opciones");

                        if (c.Options == null)
                            break;

                        foreach (ChoiceOption option in c.Options)
                        {
                            sb.AppendLine()
                                .Append("                  -> ")
                                .Append(option.Target.ToString("000"))
                                .Append("  ");

                            if (!string.IsNullOrEmpty(option.Condition))
                            {
                                sb.Append("if ")
                                    .Append(option.Condition)
                                    .Append("  ");
                            }

                            sb.Append(Cut(option.Text))
                                .Append("   [")
                                .Append(option.LocKey)
                                .Append(']');
                        }

                        break;

                    case OpCode.Command:
                        sb.Append(c.Arg);
                        if (c.Args != null && c.Args.Length > 0) sb.Append(' ').Append(string.Join(' ', c.Args));
                        break;

                    case OpCode.Jump:
                        sb.Append("-> ").Append(Where(c.Target, rev));
                        break;

                    case OpCode.JumpIfFalse:
                        sb.Append(c.Arg).Append("   si falso -> ").Append(Where(c.Target, rev));
                        break;
                    
                    case OpCode.Wait:
                        sb.Append(c.Seconds)
                            .Append(" segundos");
                        break;

                    case OpCode.Auto:
                        sb.Append(c.Seconds < 0f
                            ? "off"
                            : $"{c.Seconds} segundos");
                        break;

                    case OpCode.AutoOnce:
                        sb.Append(c.Seconds)
                            .Append(" segundos");
                        break;

                    case OpCode.Speed:
                        sb.Append("x")
                            .Append(c.SpeedMultiplier);
                        break;
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string Where(int t, Dictionary<int, string> rev) =>
            rev.TryGetValue(t, out string n) ? $"{t:000} ({n})" : t.ToString("000");

        private static string Cut(string s) =>
            s != null && s.Length > 44 ? s.Substring(0, 44) + "…" : s;
    }
}
#endif