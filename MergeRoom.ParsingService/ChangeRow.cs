namespace MergeRoom.Parsing
{
    public record ChangeRow
    {
        private string _text;
        
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                TextHash = _text.GetHashCode();
            }
        }

        public int TextHash { get; private set; }

        public int Number { get; set; }
    }
}
