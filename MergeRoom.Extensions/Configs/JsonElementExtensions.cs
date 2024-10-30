using MongoDB.Bson;
using System.Text.Json;

namespace MergeRoom.Extensions.Configs
{
    public static class JsonElementExtensions
    {
        public static BsonDocument ToBd(this JsonElement element)
        {
            var jsonString = element.GetRawText();
            return BsonDocument.Parse(jsonString);
        }
    }
}
