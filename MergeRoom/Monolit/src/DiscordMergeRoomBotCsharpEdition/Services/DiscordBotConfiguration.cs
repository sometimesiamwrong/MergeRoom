using DiscordMergeRoomBotCsharpEdition.Configs;

namespace DiscordMergeRoomBotCsharpEdition.Services
{
    public class DiscordBotConfiguration
    {
        public DiscordBotConfiguration(
            PossibleEventKinds possibleObjectKinds,
            PossiblePusherKinds possiblePusherKinds,
            PossibleSystemNoteKinds possibleSystemNoteKinds)
        {
            PossibleSystemNoteKinds = possibleSystemNoteKinds;
            PossibleObjectKinds = possibleObjectKinds;
            PossiblePusherKinds = possiblePusherKinds;
        }

        public PossibleEventKinds PossibleObjectKinds { get; }

        public PossiblePusherKinds PossiblePusherKinds { get; }

        public PossibleSystemNoteKinds PossibleSystemNoteKinds { get; }
    }
}
