using System.Collections.Generic;

namespace EEsto.DialogueNyaa
{
    public interface IDialogueView
    {
        void Initialize(DialogueSystem dialogueSystem);

        void ShowLine(DialogueLine line);
        void ShowChoices(List<ChoiceOption> choices);

        void ChangeSpeed(float multiplier);
        void Close();
    }
}