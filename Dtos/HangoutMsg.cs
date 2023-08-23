namespace CAF.Dtos
{
    public class HangoutMsg
    {
        public HangoutMsg(string text)
        {
            this.text = text;
        }

        public string text { get; set; }
    }
}