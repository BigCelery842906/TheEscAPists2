using System;
using Newtonsoft.Json;

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
        public PacketType Type { get; set; }

        public string ToJson()
        {
            var options = new JsonSerializerSettings()
            {
                Converters = { new PacketConverter() },
                
                
            };

            return JsonConvert.SerializeObject(this, options);
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
            public string game = ModInfo.GameName;
            public string slotName;
            public string serverAddress;
            public string password = "";
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

        public class MessagePacket : Packet
        {
            public string chatMessage = "";

            public MessagePacket()
            {
                Type = PacketType.ChatMessage;
            }

            public MessagePacket(string message)
            {
                Type = PacketType.ChatMessage;
                this.chatMessage = message;
            }
        }

        public class CheckPacket : Packet
        {
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
    }

    public class PacketConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }
}