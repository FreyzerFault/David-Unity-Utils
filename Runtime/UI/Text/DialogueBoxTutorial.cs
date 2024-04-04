using UnityEngine.UI;

namespace DavidUtils.UI.Text
{
    public class DialogueBoxTutorial : DialogueBox
    {
        public Image buttonPC;
        public Image buttonGamepad;

        protected override void UpdateText()
        {
            base.UpdateText();

            if (dialogue[index] is not TutorialMsg tutorial) return;

            buttonPC.sprite = tutorial.spritePC;
            buttonGamepad.sprite = tutorial.spriteGampepad;
        }
    }
}