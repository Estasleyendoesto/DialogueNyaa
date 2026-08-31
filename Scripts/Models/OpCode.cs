namespace EEsto.DialogueNyaa
{
    public enum OpCode
    {
        Say,
        Choice,

        Jump,
        JumpIfFalse,

        Wait,
        Auto,
        AutoOnce,
        Speed,

        Command,
        End
    }
}