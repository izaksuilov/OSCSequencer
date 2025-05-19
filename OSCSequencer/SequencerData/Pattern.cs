namespace OSCSequencer.SequencerData
{
    [Serializable]
    public class Pattern
    {
        public int[] Notes { get; set; }
        public int Length => Notes.Length;

        public Pattern() => Notes = Array.Empty<int>();

        public Pattern(int length)
        {
            Notes = new int[length];
        }

        public Pattern Clone() => new Pattern(Notes.Length)
        {
            Notes = (int[])Notes.Clone()
        };
    }
}