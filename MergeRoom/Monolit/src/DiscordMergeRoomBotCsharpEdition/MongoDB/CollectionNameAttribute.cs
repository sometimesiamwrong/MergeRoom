namespace DiscordMergeRoomBotCsharpEdition.MongoDB
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CollectionNameAttribute : Attribute
    {
        public CollectionNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
