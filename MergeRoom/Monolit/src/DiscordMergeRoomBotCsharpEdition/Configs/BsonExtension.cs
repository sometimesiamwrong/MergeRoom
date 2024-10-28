using MongoDB.Bson;

namespace DiscordMergeRoomBotCsharpEdition.Configs
{
    public static class BsonExtension
    {
        public static BsonDateTime ToBsonDate(this string date)
        {
            date = date.Substring(0, 19) + ".000Z";

            return DateTime.Parse(date).ToUniversalTime();
        }

        public static string AsNullableString(this BsonValue value)
        {
            return value.IsBsonNull ? string.Empty : value.AsString;
        }

        public static long AsLong(this BsonValue value)
        {
            if (value is BsonInt64 bsonInt64)
            {
                return bsonInt64.Value;
            }

            if (value is BsonInt32 bsonInt32)
            {
                return bsonInt32.Value;
            }

            throw new ArgumentException("Cannot parse value as a number Long");
        }

        public static long? AsNullableLong(this BsonValue value)
        {
            if (value is BsonInt64 bsonInt64)
            {
                return bsonInt64.Value;
            }

            if (value is BsonInt32 bsonInt32)
            {
                return bsonInt32.Value;
            }

            if (value is BsonNull)
            {
                return null;
            }

            throw new ArgumentException("Cannot parse value as a number nullable Long");
        }

        public static int AsInt(this BsonValue value)
        {
            if (value is BsonInt32 bsonInt32)
            {
                return bsonInt32.Value;
            }

            throw new ArgumentException("Cannot parse value as a number Int");
        }

        public static int? AsNullableInt(this BsonValue value)
        {
            if (value is BsonInt32 bsonInt32)
            {
                return bsonInt32.Value;
            }

            if (value is BsonNull)
            {
                return null;
            }

            throw new ArgumentException("Cannot parse value as a number nullable Int");
        }
    }
}
