using System;
using TMPro;
using UnityEngine;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// Adult check before destructive actions: a written sum with three big answers. Pre-readers cannot pass it by
    /// chance more than occasionally, and a wrong answer simply closes the gate (no penalty, no message).
    /// </summary>
    public sealed class ParentGate : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_Question;
        [SerializeField] private TapButton[] m_Answers = new TapButton[3];
        [SerializeField] private TextMeshProUGUI[] m_AnswerLabels = new TextMeshProUGUI[3];
        [SerializeField] private TapButton m_Cancel;

        private int m_Correct;
        private Action m_OnPass;

        private void Awake()
        {
            for (int i = 0; i < m_Answers.Length; i++)
            {
                int captured = i;
                if (m_Answers[i] != null) m_Answers[i].Pressed += () => Choose(captured);
            }
            if (m_Cancel != null) m_Cancel.Pressed += Close;
            gameObject.SetActive(false);
        }

        public void Ask(Action onPass)
        {
            m_OnPass = onPass;
            int a = UnityEngine.Random.Range(6, 10), b = UnityEngine.Random.Range(5, 10);
            int answer = a + b;
            m_Question.text = $"Grown-ups: what is {a} + {b}?";
            m_Correct = UnityEngine.Random.Range(0, m_Answers.Length);
            int[] offsets = { -3, 2, 4, -2, 3 };
            int o = 0;
            for (int i = 0; i < m_AnswerLabels.Length; i++)
            {
                int value = i == m_Correct ? answer : answer + offsets[o++ % offsets.Length];
                if (m_AnswerLabels[i] != null) m_AnswerLabels[i].text = value.ToString();
            }
            gameObject.SetActive(true);
        }

        private void Choose(int index)
        {
            bool pass = index == m_Correct;
            Action cb = m_OnPass;
            Close();
            if (pass) cb?.Invoke();
        }

        public void Close()
        {
            m_OnPass = null;
            gameObject.SetActive(false);
        }

        /// <summary>Tests: answer correctly.</summary>
        public void SimulateCorrectAnswer() => Choose(m_Correct);
    }
}
