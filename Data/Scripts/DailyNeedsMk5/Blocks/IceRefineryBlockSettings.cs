using ProtoBuf;

namespace Stollie.DailyNeeds
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class IceRefineryBlockSettings
    {
        [ProtoMember(1)]
        public int refineRatio;
        public bool colorChanged;
    }
}
