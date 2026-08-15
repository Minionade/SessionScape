using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SessionScape.Main.Protocol
{
    public class MessageEnvelope
    {
        [JsonProperty("sequence")]
        public int Sequence { get; set; }

        [JsonProperty("tick")]
        public long Tick { get; set; }

        [JsonProperty("type")]
        public MessageType Type { get; set; }

        [JsonProperty("data")]
        public JObject Data { get; set; } = new JObject();

        public T GetData<T>()
        {
            return Data.ToObject<T>();
        }

        public static MessageEnvelope Create<T>(MessageType type, int seq, long tick, T data)
        {
            return new MessageEnvelope()
            {
                Sequence = seq,
                Tick = tick,
                Type = type,
                Data = JObject.FromObject(data)
            };
        }

        public string ToJson() => JsonConvert.SerializeObject(this);

        public static MessageEnvelope FromJson(string json) =>
            JsonConvert.DeserializeObject<MessageEnvelope>(json);
    }
}