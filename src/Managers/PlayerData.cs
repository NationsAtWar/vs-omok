using Vintagestory.API.MathTools;
namespace AculemMods {

    public class PlayerData {

        private readonly string playerUID;

        private bool isSitting = false;
        private Cardinal sittingDirection = Cardinal.NorthWest;

        private BlockPos selectedOmokBoardPos;

        public BlockPos SelectedOmokBoardPos { get => selectedOmokBoardPos; set => selectedOmokBoardPos = value; }

        public PlayerData(string playerUID) {

            this.playerUID = playerUID;
        }

        public string GetPlayerUID() {

            return playerUID;
        }

        public bool IsPlayerSitting() {

            return isSitting;
        }

        public void TogglePlayerSitting() {

            isSitting = !isSitting;
        }

        public void SetSittingDirection(Cardinal direction) {

            sittingDirection = direction;
        }

        public Cardinal GetSittingDirection() {

            return sittingDirection;
        }

        public void SaveData() {

            // TODO: Add functionality for world saving and world loading

            /*
            playerToAdd.WorldData.SetModdata("issitting", SerializerUtil.Serialize<bool>(false));

            byte[] modData = playerToAdd.WorldData.GetModdata("issitting");

            if (modData == null)
                modData = SerializerUtil.Serialize<bool>(false);

            */
        }
    }
}