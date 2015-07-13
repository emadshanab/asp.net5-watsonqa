namespace wqa.Models
{
    public class QAWatson
    {
        public string name { get; set; }
        public string question { get; set; }
        public string answer { get; set; }
        public int[] id { get; set; }
        public string[] text { get; set; }
        public float[] confidence { get; set; }
    }

}
