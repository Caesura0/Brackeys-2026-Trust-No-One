namespace GameData.Interrogation
{
    public class JudgmentResult
    {
        public Judgment PlayerJudgment { get; set; }
        public Judgment ActualJudgment { get; set; }
        public bool WasCorrect { get; set; }
        public JudgmentOutcome Outcome { get; set; }
        public string SuspectId { get; set; }
        public string CaseId { get; set; }
    }
}
