using System;
using System.Collections.Generic;
using System.Text;

namespace SessionScape.Server.Simulation
{
    public enum MobAiState
    {
        Idle,
        Wandering
    }

    public class MobState : Entity
    {
        public MobAiState AiState { get; set; } = MobAiState.Idle;

        public (int x, int z) HomePosition { get; set; }

        public long NextDecisionTick { get; set; }

        public MobState()
        {
            Type = EntityType.Npc;
        }
    }
}
