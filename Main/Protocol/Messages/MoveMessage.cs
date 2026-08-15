using System;

namespace SessionScape.Main.Protocol.Messages
{
    public class MoveRequest
    {
        public int TargetX { get; set; }
        public int TargetZ { get; set; }
    }

    public class MoveResponse
    {
        public int X { get; set; }
        public int Z { get; set; }
        public bool Accepted { get; set; }
    }

    public struct Waypoint
    {
        public int X { get; set; }
        public float Y { get; set; }
        public int Z { get; set; }
    }
}