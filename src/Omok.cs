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

        private ICoreClientAPI clientAPI;
        IClientNetworkChannel clientChannel;

        private ICoreServerAPI serverAPI;
        IServerNetworkChannel serverChannel;

        public override void Start(ICoreAPI api) {

            base.Start(api);

            Harmony testPatch = new Harmony("org.nationsatwar.patch");
            testPatch.PatchAll();

            Harmony.DEBUG = true;

            playerManager = PlayerManager.Instance;

            api.RegisterBlockClass("blockchair", typeof(BlockChair));

            api.RegisterBlockClass("blockomoktabletop", typeof(BlockOmokTableTop));
            api.RegisterBlockEntityClass("beomoktabletop", typeof(BEOmokTableTop));
        }
        
        public override void StartServerSide(ICoreServerAPI api) {

            serverAPI = api;
            serverAPI.Event.PlayerJoin += OnPlayerJoinServer;

            serverChannel =
                api.Network.RegisterChannel("networksit")
                .RegisterMessageType(typeof(NetworkAnimationSit))
                .SetMessageHandler<NetworkAnimationSit>(OnClientMessage)
                ;
            ;
        }

        public override void StartClientSide(ICoreClientAPI api) {

            clientAPI = api;

            clientAPI.Event.MouseMove += OnMouseMove;
            clientAPI.Event.KeyDown += OnKeyDown;
            clientAPI.Event.PlayerJoin += OnPlayerJoinClient;

            clientChannel =
                api.Network.RegisterChannel("networksit")
                .RegisterMessageType(typeof(NetworkAnimationSit))
                .SetMessageHandler<NetworkAnimationSit>(OnServerMessage)
            ;
        }

        private void OnServerMessage(NetworkAnimationSit networkMessage) {

            IPlayer sittingPlayer = clientAPI.World.PlayerByUid(networkMessage.playerUID);

            if (sittingPlayer == null)
                return;

            clientAPI.Logger.Debug("Client: " + sittingPlayer.PlayerName + " is sitting: " + networkMessage.isSitting);

            if (networkMessage.isSitting) {

                AnimationMetaData data = new AnimationMetaData() { Animation = "sitflooridle", Code = "sitflooridle", AnimationSpeed = 1.0f, BlendMode = EnumAnimationBlendMode.Add, SupressDefaultAnimation = true, EaseOutSpeed = 10000, EaseInSpeed = 10000 }.Init();
                sittingPlayer.Entity.AnimManager.StartAnimation(data);
            } else
                sittingPlayer.Entity.AnimManager.StopAnimation("sitflooridle");
        }

        private void OnClientMessage(IPlayer fromPlayer, NetworkAnimationSit networkMessage) {
            
            serverAPI.Logger.Debug("Server: " + fromPlayer.PlayerName + " is sitting: " + networkMessage.isSitting);

            if (networkMessage.isSitting) {

                AnimationMetaData data = new AnimationMetaData() { Animation = "sitflooridle", Code = "sitflooridle", AnimationSpeed = 1.0f, BlendMode = EnumAnimationBlendMode.Add, SupressDefaultAnimation = true, EaseOutSpeed = 10000, EaseInSpeed = 10000 }.Init();
                fromPlayer.Entity.AnimManager.StartAnimation(data);
                fromPlayer.Entity.AnimManager.AnimationsDirty = true;

            } else {

                fromPlayer.Entity.AnimManager.StopAnimation("sitflooridle");
                fromPlayer.Entity.AnimManager.AnimationsDirty = true;
            }

            serverChannel.BroadcastPacket(new NetworkAnimationSit() { isSitting = networkMessage.isSitting, playerUID = networkMessage.playerUID });
        }

        private void OnPlayerJoinServer(IServerPlayer byPlayer) {

            playerManager.GetOrAddPlayerData(byPlayer);
        }

        private void OnPlayerJoinClient(IClientPlayer byPlayer) {

            playerManager.GetOrAddPlayerData(byPlayer);
        }

        private void OnMouseMove(MouseEvent e) {

            // Only executed on client
            if (clientAPI != null) {

                UpdateOmokBoard();
                SittingLogic(e);
            }
        }

        private void OnKeyDown(KeyEvent e) {

            // Only executed on client
            if (clientAPI != null) {

                int pressedKeyCode = e.KeyCode;
                int selectKeyCode = clientAPI.Input.GetHotKeyByCode("toolmodeselect").CurrentMapping.KeyCode;

                BEOmokTableTop beOmok = GetSelectedBEOmok();

                if (beOmok != null && pressedKeyCode == selectKeyCode && !beOmok.GuiDialog.IsOpened())
                    beOmok.GuiDialog.TryOpen();
            }
        }

        private void UpdateOmokBoard() {

            IPlayer player = clientAPI.World.Player;
            PlayerData playerData = PlayerManager.Instance.GetOrAddPlayerData(player);

            // Update renderer if no longer looking at Omok board
            BlockPos selectedOmokBoardPos = playerData.SelectedOmokBoardPos;
            bool isSelectingBlock = (player.CurrentBlockSelection != null);

            if (selectedOmokBoardPos == null)
                return;

            // Execute if the player is either not looking at a block, or if the current selected block isn't an Omok board
            if (!isSelectingBlock || (isSelectingBlock && player.CurrentBlockSelection.Position != playerData.SelectedOmokBoardPos)) {

                if (player.Entity.World.BlockAccessor.GetBlockEntity(selectedOmokBoardPos) is BEOmokTableTop beOmok) {

                    RendererOmok rendererOmok = beOmok.OmokRenderer;
                    if (rendererOmok != null && rendererOmok.AvailableMovesMesh != null && !rendererOmok.AvailableMovesMesh.Disposed)
                        rendererOmok.DisposeAvailableMovesMesh();
                }
            }
        }

        private void SittingLogic(MouseEvent e) {

            IPlayer player = clientAPI.World.Player;
            PlayerData playerData = PlayerManager.Instance.GetOrAddPlayerData(player);

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

        private BEOmokTableTop GetSelectedBEOmok() {
            
            IPlayer player = clientAPI.World.Player;
            PlayerData playerData = PlayerManager.Instance.GetOrAddPlayerData(player);

            BlockPos selectedOmokBoardPos = playerData.SelectedOmokBoardPos;
            bool isSelectingBlock = (player.CurrentBlockSelection != null);

            if (selectedOmokBoardPos == null)
                return null;

            if (isSelectingBlock && player.CurrentBlockSelection.Position == selectedOmokBoardPos)
                if (player.Entity.World.BlockAccessor.GetBlockEntity(selectedOmokBoardPos) is BEOmokTableTop beOmok)
                    return beOmok;

            return null;
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

                if (PlayerManager.Instance == null)
                    return;

                if (player == null)
                    return;

                PlayerData playerData = PlayerManager.Instance.GetOrAddPlayerData(player);

                // Sitting Logic
                if (playerData != null && playerData.IsPlayerSitting()) {

                    // Disable Movement
                    controls.StopAllMovement();
                }
            }
        }
    }
}