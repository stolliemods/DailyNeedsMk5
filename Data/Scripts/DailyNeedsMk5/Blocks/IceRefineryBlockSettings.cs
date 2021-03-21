using ProtoBuf;

namespace Stollie.DailyNeeds
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class IceRefineryBlockSettings
    {
        [ProtoMember(1)]
        public float refineRatio;
        public bool colorChanged;
    }
}
