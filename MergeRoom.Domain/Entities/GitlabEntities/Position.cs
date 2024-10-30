namespace MergeRoom.Domain.Entities.GitlabEntities
{
    public record Position
    {
        /// <summary>
        /// Base sha
        /// </summary>
        public string BaseSha { get; set; }

        /// <summary>
        /// Start sha
        /// </summary>
        public string StartSha { get; set; }

        /// <summary>
        /// Head sha
        /// </summary>
        public string HeadSha { get; set; }

        /// <summary>
        /// OldPath to file in repository
        /// </summary>
        public string OldPath { get; set; }

        /// <summary>
        /// NewPath to file in repository
        /// </summary>
        public string NewPath { get; set; }

        /// <summary>
        /// Start line range
        /// </summary>
        public LineRange Start { get; set; }

        /// <summary>
        /// End line range
        /// </summary>
        public LineRange End { get; set; }
    }

    public record LineRange
    {
        private string _lineCode;

        public string LineCode
        {
            get => _lineCode;
            set
            {
                _lineCode = value;
                Range = new(int.Parse(value.Split('_')[1]) - 1, int.Parse(value.Split('_')[2]) - 1);
            }
        }

        public (int OldNumber, int NewNumber) Range { get; set; }

        public string Type { get; set; }

        public int? OldLine { get; set; }

        public int? NewLine { get; set; }
    }
}
