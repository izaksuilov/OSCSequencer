namespace OSCSequencer.SequencerData
{
    [Serializable]
    public class Project
    {
        public string Name { get; set; } = "New Project";
        public List<Pattern> Patterns { get; set; } = new();
        public int CurrentPatternIndex { get; set; }
        public int Bpm { get; set; } = 120;
    }
}