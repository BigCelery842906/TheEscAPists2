using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BC_Dual_Ref_Library
{
    public enum PacketType
    {
        Connection,
        ChatMessage,
        Check,
        Deathlink,
        Goal,
    }

    public class Packet
    {
        [JsonProperty("Type")]
        public PacketType Type { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static Packet Deserialise(string json)
        {
            var options = new JsonSerializerSettings
            {
                Converters = { new PacketConverter() },
            };

            return JsonConvert.DeserializeObject<Packet>(json, options);
        }

        public class ConnectionPacket : Packet
        {
            // game, slotName, serverAddress, password, gameVersion
            [JsonProperty("GameName")]
            public string game = ModInfo.GameName;
            [JsonProperty("SlotName")]
            public string slotName;
            [JsonProperty("ServerAddress")]
            public string serverAddress;
            [JsonProperty("Password")]
            public string password = "";
            [JsonProperty("GameVersion")]
            public Version version = ModInfo.GameVersion;


            public ConnectionPacket()
            {
                Type = PacketType.Connection;
            }

            public ConnectionPacket(string slotName, string serverAddress, string password = "")
            {
                Type = PacketType.Connection;
                this.slotName = slotName;
                this.serverAddress = serverAddress;
                this.password = password;
            }
        }

        public class ChatMessagePacket : Packet
        {
            [JsonProperty("ChatMessage")]
            public string chatMessage = "";

            public ChatMessagePacket()
            {
                Type = PacketType.ChatMessage;
            }

            public ChatMessagePacket(string message)
            {
                Type = PacketType.ChatMessage;
                this.chatMessage = message;
            }
        }

        public class CheckPacket : Packet
        {
            [JsonProperty("CheckName")]
            public string checkName = "";

            public CheckPacket()
            {
                Type = PacketType.Check;
            }

            public CheckPacket(string checkName)
            {
                Type = PacketType.Check;
                this.checkName = checkName;
            }
        }

        public class DeathlinkPacket : Packet
        {
            public DeathlinkPacket()
            {
                Type = PacketType.Deathlink;
            }
        }

        public class GoalPacket : Packet
        {
            public GoalPacket()
            {
                Type = PacketType.Goal;
            }
        }
    }

    public class PacketConverter : JsonConverter<Packet>
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, Packet value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override Packet ReadJson(JsonReader reader, Type objectType, Packet existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);

            JToken TypeToken = jsonObject["Type"];

            if (TypeToken == null)
            {
                throw new JsonSerializationException("Packet does not contain a type property.");
            }
            
            switch (TypeToken.ToObject<PacketType>())
            {
                case PacketType.Connection:
                {
                    Console.WriteLine("Deserializing connection packet");
                    return jsonObject.ToObject<Packet.ConnectionPacket>(serializer);
                    break;
                }
                case PacketType.ChatMessage:
                {
                    Console.WriteLine("Deserializing chat message packet");
                    return jsonObject.ToObject<Packet.ChatMessagePacket>(serializer);
                    break;
                }
                case PacketType.Check:
                {
                    Console.WriteLine("Deserializing check packet");
                    return jsonObject.ToObject<Packet.CheckPacket>(serializer);
                    break;
                }
                case PacketType.Deathlink:
                {
                    Console.WriteLine("Deserializing deathlink packet");
                    return jsonObject.ToObject<Packet.DeathlinkPacket>(serializer);
                    break;
                }
                case PacketType.Goal:
                {
                    Console.WriteLine("Deserializing goal packet");
                    return jsonObject.ToObject<Packet.GoalPacket>(serializer);
                    break;
                }
            }

            throw new JsonException("Unknown Type");
        }

    }
}