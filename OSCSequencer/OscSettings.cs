namespace OSCSequencer
{
    public class OscSettings
    {
        public Dictionary<string, string> Addresses { get; } = new()
        {
            ["noteon"] = "/synth/noteon",
            ["noteoff"] = "/synth/noteoff",
            ["tempo"] = "/global/tempo",
            ["pattern"] = "/global/pattern"
        };
    }
}