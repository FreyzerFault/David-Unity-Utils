using System;
using System.Collections;
using System.Collections.Generic;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DavidUtils.UI.Text
{
    public class DialogueBox : MonoBehaviour
    {
        [SerializeField] private TMP_Text textUI;

        [SerializeField] private Animator animator;

        [SerializeField] private Button nextButton;

        public List<TextBlock> dialogue = new();
        public int index;

        public bool finished;
        private static readonly int ShowID = Animator.StringToHash("Show");

        public event Action OnNext;
        public event Action OnEnd;

        public bool showOnAwake = true;
        public bool isTemporal;

        [ConditionalField("isTemporal")] public float secondsUntilHide = 5;

        public bool IsShown => animator.GetBool(ShowID);

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator != null) animator.SetBool(ShowID, false);

            if (isTemporal) nextButton.gameObject.SetActive(false);

            if (!showOnAwake) return;

            ShowText();
        }

        public void ShowText(int i = -1)
        {
            // Si es -1 no cambia el texto
            if (i > -1)
                SkipToText(i);
            else
                UpdateText();

            if (isTemporal && secondsUntilHide > 0) StartCoroutine(TemporalShowCoroutine(secondsUntilHide));
        }

        private IEnumerator TemporalShowCoroutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            Next();
        }

        public void Next()
        {
            if (finished)
            {
                Hide();
                OnEnd?.Invoke();
                return;
            }

            ShowText(index + 1);

            OnNext?.Invoke();
        }

        public void SkipToText(int i)
        {
            index = i;
            UpdateText();
        }

        protected virtual void UpdateText()
        {
            var textBlock = dialogue[index];

            textUI.text = textBlock.text;
            textUI.color = textBlock.color;
            textUI.font = textBlock.fontAsset;

            // Size == -1 => AutoSizing
            if (textBlock.size == -1)
                textUI.enableAutoSizing = true;
            else
                textUI.fontSize = textBlock.size;

            if (index >= dialogue.Count - 1) finished = true;

            Show();
        }

        public void Show()
        {
            if (animator != null)
                animator.SetBool(ShowID, true);
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (animator != null)
                animator.SetBool(ShowID, false);
            else
                gameObject.SetActive(false);
        }
    }
}