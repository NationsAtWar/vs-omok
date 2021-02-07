using ProtoBuf;

namespace AculemMods {

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class NetworkAnimationSit {

        public string playerUID;
        public bool isSitting;
    }
}