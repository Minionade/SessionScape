using System;
using System.Collections.Generic;
using System.Text;

namespace SessionScape.Server.Simulation
{
    public class PlayerAccount
    {
        public string Name;
        public byte[] PasswordHash;
        public byte[] PasswordSalt;

        public int X, Z;
        public float Y;
        public double RunEnergy;
    }
}
