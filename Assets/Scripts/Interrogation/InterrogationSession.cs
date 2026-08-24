using System.Collections.Generic;

namespace GameData.Interrogation
{
    public class InterrogationSession
    {
        public string CaseId { get; set; }
        public List<string> QuestionsAsked { get; set; } = new List<string>();
        public int QuestionsRemaining { get; set; }
        public InterrogationState State { get; set; } = InterrogationState.NotStarted;
        public Judgment? PlayerJudgment { get; set; }
        public JudgmentResult Result { get; set; }
    }
}
