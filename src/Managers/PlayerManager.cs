using System.Collections.Generic;
using Vintagestory.API.Common;

namespace AculemMods {

    public class PlayerManager {

        // Master Player List
        private readonly List<PlayerData> playerList = new List<PlayerData>();

        // Singleton Constructor
        private static readonly PlayerManager instance = new PlayerManager();
        private PlayerManager() { }

        public static PlayerManager Instance {

            get { return instance; }
        }

        public PlayerData GetOrAddPlayerData(IPlayer player) {

            string playerUID = player.PlayerUID;

            foreach (PlayerData playerData in playerList)
                if (playerData.GetPlayerUID().Equals(playerUID))
                    return playerData;

            // If no PlayerData is found, create one and return it
            PlayerData newPlayerData = new PlayerData(playerUID);
            playerList.Add(newPlayerData);

            return newPlayerData;
        }
    }
}