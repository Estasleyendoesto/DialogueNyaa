using System;
using System.Collections.Generic;
using System.Globalization;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Compila un guion a una lista plana de instrucciones.
    /// Los nodos se convierten en etiquetas que apuntan al código.
    /// </summary>
    public static class DialogueParser
    {
        private sealed class IfFrame
        {
            public int FalseJump;
            public readonly List<int> EndJumps = new();
        }

        private sealed class ChoiceFrame
        {
            public Instruction Instruction;
            public readonly List<ChoiceOption> Options = new();
            public readonly List<int> EndJumps = new();
        }

        private struct JumpFixup
        {
            public int InstructionIndex;
            public string Label;

            public JumpFixup(int instructionIndex, string label)
            {
                InstructionIndex = instructionIndex;
                Label = label;
            }
        }

        public static DialogueProgram Parse(string storyId, string rawText)
        {
            var program = new DialogueProgram();

            List<Instruction> code = program.Code;
            var ifs = new Stack<IfFrame>();
            var choices = new Stack<ChoiceFrame>();
            var jumpFixups = new List<JumpFixup>();

            string currentNode = storyId;
            int localizationIndex = 0;

            string normalizedText = NormalizeLineEndings(rawText);
            string[] lines = normalizedText.Split('\n');

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith("//"))
                    continue;

                if (line.StartsWith("node "))
                {
                    CloseNode(code, choices, ifs);

                    currentNode = ParseNodeName(line);
                    program.Labels[currentNode] = code.Count;
                    localizationIndex = 0;

                    continue;
                }

                if (line.StartsWith("-"))
                {
                    ParseChoice(
                        line,
                        storyId,
                        currentNode,
                        ref localizationIndex,
                        code,
                        choices);

                    continue;
                }

                if (line == ":: continue")
                {
                    CloseChoice(code, choices);
                    continue;
                }

                if (line.StartsWith("::"))
                {
                    ParseSystemLine(
                        line,
                        code,
                        ifs,
                        jumpFixups);

                    continue;
                }

                code.Add(new Instruction
                {
                    Op = OpCode.Say,
                    Line = ParseDialogueLine(
                        line,
                        storyId,
                        currentNode,
                        localizationIndex++)
                });
            }

            CloseNode(code, choices, ifs);
            ResolveJumps(program, jumpFixups);

            return program;
        }

        private static void ParseChoice(
            string line,
            string storyId,
            string currentNode,
            ref int localizationIndex,
            List<Instruction> code,
            Stack<ChoiceFrame> choices)
        {
            ChoiceFrame frame;

            if (choices.Count == 0)
            {
                frame = new ChoiceFrame
                {
                    Instruction = new Instruction
                    {
                        Op = OpCode.Choice
                    }
                };

                code.Add(frame.Instruction);
                choices.Push(frame);
            }
            else
            {
                frame = choices.Peek();
                CloseBody(code, frame.EndJumps);
            }

            ChoiceOption option = ParseChoiceOption(line);

            option.LocKey =
                $"{storyId}_{currentNode}_{localizationIndex++}";

            option.Target = code.Count;

            frame.Options.Add(option);
        }

        private static void CloseChoice(List<Instruction> code, Stack<ChoiceFrame> choices)
        {
            ChoiceFrame frame = choices.Pop();

            CloseBody(code, frame.EndJumps);

            frame.Instruction.Options = frame.Options.ToArray();

            Patch(code, frame.EndJumps, code.Count);
        }

        private static void ParseSystemLine(
            string line,
            List<Instruction> code,
            Stack<IfFrame> ifs,
            List<JumpFixup> jumpFixups)
        {
            string content = line.Substring(2).Trim();

            string[] parts = content.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return;

            string command = parts[0].ToLowerInvariant();

            switch (command)
            {
                case "jump":
                    ParseJump(parts, code, jumpFixups);
                    break;

                case "endstory":
                    code.Add(new Instruction
                    {
                        Op = OpCode.End
                    });
                    break;

                case "if":
                    ParseIf(parts, code, ifs);
                    break;

                case "elseif":
                    ParseElseIf(parts, code, ifs);
                    break;

                case "else":
                    ParseElse(code, ifs);
                    break;

                case "end":
                    ParseEndIf(code, ifs);
                    break;

                case "wait":
                    code.Add(new Instruction
                    {
                        Op = OpCode.Wait,
                        Seconds = ParseFloat(parts, 1, 0.5f)
                    });
                    break;

                case "auto":
                    code.Add(new Instruction
                    {
                        Op = OpCode.Auto,
                        Seconds = ParseFloat(parts, 1, -1f)
                    });
                    break;

                case "autoonce":
                    code.Add(new Instruction
                    {
                        Op = OpCode.AutoOnce,
                        Seconds = ParseFloat(parts, 1, -1f)
                    });
                    break;

                case "speed":
                    code.Add(new Instruction
                    {
                        Op = OpCode.Speed,
                        SpeedMultiplier = ParseFloat(parts, 1, 1f)
                    });
                    break;

                default:
                    code.Add(new Instruction
                    {
                        Op = OpCode.Command,
                        Arg = parts[0],
                        Args = Slice(parts, 1)
                    });
                    break;
            }
        }

        private static void ParseJump(
            string[] parts,
            List<Instruction> code,
            List<JumpFixup> jumpFixups)
        {
            string label = parts.Length > 1
                ? parts[1]
                : string.Empty;

            jumpFixups.Add(new JumpFixup(code.Count, label));

            code.Add(new Instruction
            {
                Op = OpCode.Jump
            });
        }

        private static void ParseIf(
            string[] parts,
            List<Instruction> code,
            Stack<IfFrame> ifs)
        {
            ifs.Push(new IfFrame
            {
                FalseJump = code.Count
            });

            code.Add(new Instruction
            {
                Op = OpCode.JumpIfFalse,
                Arg = Rest(parts, 1)
            });
        }

        private static void ParseElseIf(
            string[] parts,
            List<Instruction> code,
            Stack<IfFrame> ifs)
        {
            IfFrame frame = ifs.Peek();

            CloseBody(code, frame.EndJumps);

            code[frame.FalseJump].Target = code.Count;
            frame.FalseJump = code.Count;

            code.Add(new Instruction
            {
                Op = OpCode.JumpIfFalse,
                Arg = Rest(parts, 1)
            });
        }

        private static void ParseElse(
            List<Instruction> code,
            Stack<IfFrame> ifs)
        {
            IfFrame frame = ifs.Peek();

            CloseBody(code, frame.EndJumps);

            code[frame.FalseJump].Target = code.Count;
            frame.FalseJump = -1;
        }

        private static void ParseEndIf(
            List<Instruction> code,
            Stack<IfFrame> ifs)
        {
            IfFrame frame = ifs.Pop();

            if (frame.FalseJump >= 0)
                code[frame.FalseJump].Target = code.Count;

            Patch(code, frame.EndJumps, code.Count);
        }

        private static DialogueLine ParseDialogueLine(
            string line,
            string storyId,
            string currentNode,
            int localizationIndex)
        {
            int separator = line.IndexOf(": ", StringComparison.Ordinal);

            string speaker = separator > 0
                ? line.Substring(0, separator).Trim()
                : null;

            string text = separator > 0
                ? line.Substring(separator + 2).Trim()
                : line;

            return new DialogueLine
            {
                Speaker = speaker,

                SpeakerKey = speaker != null
                    ? "speaker_" + speaker
                    : null,

                Text = text,

                LocKey =
                    $"{storyId}_{currentNode}_{localizationIndex}"
            };
        }

        private static ChoiceOption ParseChoiceOption(string line)
        {
            var option = new ChoiceOption();

            string[] sections = line.Split(
                new[] { "::" },
                StringSplitOptions.None);

            string text = sections[0]
                .Trim()
                .Substring(1)
                .Trim();

            if (text.Length > 1 &&
                text.StartsWith("\"") &&
                text.EndsWith("\""))
            {
                text = text.Substring(1, text.Length - 2);
            }

            option.Text = text;

            if (sections.Length > 1)
            {
                string logic = sections[1].Trim();

                if (logic.StartsWith("if "))
                    option.Condition = logic.Substring(3).Trim();
            }

            return option;
        }

        private static void CloseNode(
            List<Instruction> code,
            Stack<ChoiceFrame> choices,
            Stack<IfFrame> ifs)
        {
            if (code.Count == 0)
                return;

            var pendingJumps = new List<int>();

            while (choices.Count > 0)
            {
                ChoiceFrame frame = choices.Pop();

                CloseBody(code, frame.EndJumps);

                frame.Instruction.Options =
                    frame.Options.ToArray();

                pendingJumps.AddRange(frame.EndJumps);
            }

            // El texto ya ha sido validado antes de llegar al parser.
            ifs.Clear();

            if (pendingJumps.Count == 0 && IsTerminated(code))
                return;

            int endTarget = code.Count;

            code.Add(new Instruction
            {
                Op = OpCode.End
            });

            Patch(code, pendingJumps, endTarget);
        }

        private static void CloseBody(
            List<Instruction> code,
            List<int> pendingJumps)
        {
            if (IsTerminated(code))
                return;

            pendingJumps.Add(code.Count);

            code.Add(new Instruction
            {
                Op = OpCode.Jump
            });
        }

        private static bool IsTerminated(List<Instruction> code)
        {
            if (code.Count == 0)
                return false;

            OpCode lastOp = code[code.Count - 1].Op;

            return lastOp == OpCode.Jump ||
                   lastOp == OpCode.End;
        }

        private static void Patch(
            List<Instruction> code,
            List<int> jumps,
            int target)
        {
            foreach (int jump in jumps)
                code[jump].Target = target;
        }

        private static void ResolveJumps(
            DialogueProgram program,
            List<JumpFixup> jumpFixups)
        {
            foreach (JumpFixup fixup in jumpFixups)
            {
                program.Code[fixup.InstructionIndex].Target =
                    program.Labels[fixup.Label];
            }
        }

        private static string ParseNodeName(string line)
        {
            return line
                .Substring(5)
                .Replace(":", string.Empty)
                .Trim();
        }

        private static float ParseFloat(
            string[] parts,
            int index,
            float defaultValue)
        {
            if (parts.Length <= index)
                return defaultValue;

            return float.TryParse(
                parts[index],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float value)
                    ? value
                    : defaultValue;
        }

        private static string Rest(string[] parts, int from)
        {
            return parts.Length <= from
                ? string.Empty
                : string.Join(
                    " ",
                    parts,
                    from,
                    parts.Length - from);
        }

        private static string[] Slice(string[] parts, int from)
        {
            if (parts.Length <= from)
                return Array.Empty<string>();

            var result = new string[parts.Length - from];

            Array.Copy(
                parts,
                from,
                result,
                0,
                result.Length);

            return result;
        }

        private static string NormalizeLineEndings(string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');
        }
    }
}