using System.Collections.Generic;

namespace EEsto.DialogueNyaa
{
    public class DialogueProgram
    {
        public List<Instruction> Code = new();
        public Dictionary<string, int> Labels = new();
    }
}