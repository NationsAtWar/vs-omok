using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AculemMods {

    public class Omok : ModSystem {

        PlayerManager playerManager;
        private ICoreServerAPI serverAPI;
        private ICoreClientAPI clientAPI;

        public override void Start(ICoreAPI api) {

            base.Start(api);

            Harmony testPatch = new Harmony("org.nationsatwar.patch");
            testPatch.PatchAll();

            Harmony.DEBUG = true;

            playerManager = PlayerManager.Instance;

            api.RegisterBlockClass("blockchair", typeof(BlockChair));

            api.RegisterBlockClass("blockomoktabletop", typeof(BlockOmokTableTop));
            api.RegisterBlockEntityClass("beomoktabletop", typeof(BEOmokTableTop));
            api.RegisterItemClass("itemomok", typeof(ItemOmok));
        }
        
        public override void StartServerSide(ICoreServerAPI api) {

            serverAPI = api;
            serverAPI.Event.PlayerJoin += OnPlayerJoin;
        }

        public override void StartClientSide(ICoreClientAPI api) {

            clientAPI = api;

            clientAPI.Event.MouseMove += OnMouseMove;
        }

        private void OnPlayerJoin(IServerPlayer byPlayer) {

            playerManager.AddPlayer(byPlayer);
        }

        private void OnMouseMove(MouseEvent e) {

            // Only executed on client
            if (clientAPI != null) {

                UpdateOmokBoard();
                SittingLogic(e);
            }
        }

        private void UpdateOmokBoard() {

            IPlayer player = clientAPI.World.Player;
            PlayerData playerData = PlayerManager.Instance.GetPlayerData(player);

            // Update renderer if no longer looking at Omok board
            BlockPos selectedOmokBoardPos = playerData.SelectedOmokBoardPos;
            bool isSelectingBlock = (player.CurrentBlockSelection != null);

            if (selectedOmokBoardPos == null)
                return;

            // Execute if the player is either not looking at a block, or if the current selected block isn't an Omok board
            if (!isSelectingBlock || (isSelectingBlock && player.CurrentBlockSelection.Position != playerData.SelectedOmokBoardPos)) {

                if (player.Entity.World.BlockAccessor.GetBlockEntity(selectedOmokBoardPos) is BEOmokTableTop beOmok) {

                    RendererOmok rendererOmok = beOmok.OmokRenderer;
                    if (rendererOmok != null)
                        rendererOmok.DisposeAvailableMovesMesh();
                }
            }
        }

        private void SittingLogic(MouseEvent e) {

            IPlayer player = clientAPI.World.Player;
            PlayerData playerData = PlayerManager.Instance.GetPlayerData(player);

            // Only executed when player is sitting
            if (!playerData.IsPlayerSitting() || clientAPI.World.Player.CameraMode.Equals(EnumCameraMode.Overhead))
                return;

            float yawCap = 0.2f;
            float tableYaw;
            float normalYaw = clientAPI.Input.MouseYaw % GameMath.TWOPI / GameMath.TWOPI;

            // Determine default yaw based on sitting direction if applicable
            if (playerData.GetSittingDirection().Equals(Cardinal.East))
                tableYaw = 0;
            else if (playerData.GetSittingDirection().Equals(Cardinal.North))
                tableYaw = 0.25f;
            else if (playerData.GetSittingDirection().Equals(Cardinal.West))
                tableYaw = 0.5f;
            else if (playerData.GetSittingDirection().Equals(Cardinal.South))
                tableYaw = 0.75f;
            else
                return; // No need to clamp rotation if there's no sitting direction

            // Clamp how far the player can rotate while sitting
            if (normalYaw > (tableYaw + yawCap) || normalYaw < (tableYaw - yawCap)) {

                if ((e.DeltaX < 0 && normalYaw > tableYaw) || (e.DeltaX > 0 && normalYaw < tableYaw)) {

                    e.Handled = true;
                    // TODO: Add DeltaY movement to player's pitch
                }
            }
        }
    }

    // Handles Game Tick Logic
    [HarmonyPatch(typeof(EntityBehaviorPlayerPhysics))]
    [HarmonyPatch("GameTick")]
    class OnEntityGameTick {

        static void Prefix(EntityBehaviorPlayerPhysics __instance, Entity entity, float dt) {

            // FileLog.Log("Logs onto desktop Logger");

            // Continue if entity is a player
            if (entity is EntityPlayer entityPlayer) {

                // Assign useful variables
                EntityControls controls = entityPlayer.Controls;
                string playerUID = entity.WatchedAttributes.GetString("playerUID");
                IPlayer player = entity.World.PlayerByUid(playerUID);
                PlayerData playerData = PlayerManager.Instance.GetPlayerData(player);

                // Sitting Logic
                if (playerData.IsPlayerSitting()) {

                    // Disable Movement
                    controls.StopAllMovement();
                }
            }
        }
    }
}