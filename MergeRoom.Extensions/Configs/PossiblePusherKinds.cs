namespace MergeRoom.Extensions.Configs
{
    public class PossiblePusherKinds
    {
        public string Channel { get; init; } = null!;

        public string Thread { get; init; } = null!;

        public List<string> AllKinds => new List<string> { Channel, Thread };
    }
}
