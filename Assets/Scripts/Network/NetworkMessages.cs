using MemoryPack;
using System.Collections.Generic;

namespace Xiangsoft.Game.Network
{
    [MemoryPackable]
    public partial struct PlayerCommand
    {
        public int PlayerID { get; set; }
        public long MoveDirX { get; set; }
        public long MoveDirZ { get; set; }
        public bool IsCastSkill { get; set; }
    }

    [MemoryPackable]
    public partial class FrameData
    {
        public int FrameID { get; set; }
        public List<PlayerCommand> Commands { get; set; }

        [MemoryPackConstructor]
        public FrameData()
        {
            Commands = new List<PlayerCommand>();
        }
    }
}