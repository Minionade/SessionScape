using System;
using System.Net.Sockets;
using System.Text;

namespace SessionScape.Main.Protocol
{
    public static class MessageFramer
    {
        public static void WriteMessage(NetworkStream stream, MessageEnvelope envelope)
        {
            string json = envelope.ToJson();
            byte[] payload = Encoding.UTF8.GetBytes(json);
            byte[] lengthPrefix = BitConverter.GetBytes(payload.Length);

            stream.Write(lengthPrefix, 0, 4);
            stream.Write(payload, 0, payload.Length);
        }

        public static MessageEnvelope ReadMessage(NetworkStream stream)
        {
            byte[] lengthBuffer = ReadExact(stream, 4);
            if (lengthBuffer == null)
                return null;

            int length = BitConverter.ToInt32(lengthBuffer, 0);

            byte[] payload = ReadExact(stream, length);
            if (payload == null)
                return null;

            string json = Encoding.UTF8.GetString(payload);
            return MessageEnvelope.FromJson(json);
        }
        
        private static byte[] ReadExact(NetworkStream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;

            while(offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0)
                    return null;

                offset += read;
            }

            return buffer;
        }
    }
}