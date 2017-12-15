using Newtonsoft.Json;
public class SpeechStartDetectedMessage : MessageBase
{
    /// <summary>
    /// Offset in 100 nanosecond units
    /// </summary>
    public int Offset { get; set; }
}
