using Newtonsoft.Json;

namespace ComponentClassifier.Models
{
    public class Reading
    {
        public string TimeStamp { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public double Fault { get; set; }

        // These fields are not in the JSON, we will populate them.
        public string Result { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
