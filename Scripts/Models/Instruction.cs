namespace EEsto.DialogueNyaa
{
    public class Instruction
    {
        public OpCode Op;

        public DialogueLine Line;
        public ChoiceOption[] Options;

        public string Arg;
        public string[] Args;

        public int Target = -1;
        public float Seconds;
        public float SpeedMultiplier;
    }
}