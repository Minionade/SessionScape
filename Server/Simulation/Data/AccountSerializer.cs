namespace SessionScape.Server.Simulation
{
    public static class AccountSerializer
    {
        public static byte[] Serialize(PlayerAccount account)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(account.Name ?? string.Empty);

            WriteByteArray(writer, account.PasswordHash);
            WriteByteArray(writer, account.PasswordSalt);

            writer.Write(account.X);
            writer.Write(account.Y);
            writer.Write(account.Z);
            writer.Write(account.RunEnergy);

            writer.Flush();

            return stream.ToArray();
        }

        public static PlayerAccount Deserialize(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            PlayerAccount account = new()
            {
                Name = reader.ReadString(),
                PasswordHash = ReadByteArray(reader),
                PasswordSalt = ReadByteArray(reader),

                X = reader.ReadInt32(),
                Y = reader.ReadSingle(),
                Z = reader.ReadInt32(),
                RunEnergy = reader.ReadDouble()
            };

            return account;
        }

        public static void WriteByteArray(BinaryWriter writer, byte[] data)
        {
            if (data == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(data.Length);
            writer.Write(data);
        }

        public static byte[] ReadByteArray(BinaryReader reader)
        {
            int length = reader.ReadInt32();

            if (length < 0)
                return null;

            return reader.ReadBytes(length);
        }
    }
}
