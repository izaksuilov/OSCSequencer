namespace OSCSequencer
{
    [Serializable]
    public class Project
    {
        public List<Pattern> Patterns { get; set; } = new();
        public int CurrentPatternIndex { get; set; }
        public int Bpm { get; set; } = 120;
    }
}