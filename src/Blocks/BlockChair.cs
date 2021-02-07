using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AculemMods {

    public class BlockChair : Block {

        private static readonly double DEFAULTEYEHEIGHT = 1.7d;
        private static readonly double SITTINGEYEHEIGHT = 1.3d;

        readonly PlayerManager playerManager = PlayerManager.Instance;

        public override void OnLoaded(ICoreAPI api) {

            base.OnLoaded(api);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {

            bool isSitting = playerManager.GetOrAddPlayerData(byPlayer).IsPlayerSitting();

            if (isSitting)
                PlayerStandUp(byPlayer);
            else
                PlayerSitDown(world, byPlayer, blockSel.Position);

            return false;
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null) {

            // TODO: Tuck/face table if applicable
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {

            PlayerData playerData = PlayerManager.Instance.GetOrAddPlayerData(byPlayer);

            if (playerData.SatChairPos == pos && playerData.IsPlayerSitting())
                playerData.TogglePlayerSitting();

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        private void PlayerSitDown(IWorldAccessor world, IPlayer byPlayer, BlockPos chairPosition) {

            PlayerData playerData = PlayerManager.Instance.GetOrAddPlayerData(byPlayer);

            // Sets the player to the center of the chair
            Vec3d centerOfChair = chairPosition.ToVec3d().Add(0.5, 0.5, 0.5);
            byPlayer.Entity.Pos.SetPos(centerOfChair);

            // Set player's status to sitting
            playerData.TogglePlayerSitting();
            playerData.SatChairPos = chairPosition;

            // If there is a table next to chair, set player to face chair
            BlockPos adjacentTable = GetAdjacentTable(world, chairPosition);

            // Send packet to server to sync all clients
            IClientNetworkChannel networkChannel = (IClientNetworkChannel)api.Network.GetChannel("networksit");
            networkChannel.SendPacket(new NetworkAnimationSit() { isSitting = true, playerUID = byPlayer.PlayerUID });

            // Modify eye height
            byPlayer.Entity.Properties.SetEyeHeight(SITTINGEYEHEIGHT);

            // Set player's sitting direction
            if (adjacentTable != null) {

                if (byPlayer.Entity.Pos.AsBlockPos.East().Equals(adjacentTable)) {

                    playerData.SetSittingDirection(Cardinal.East);
                    ((ICoreClientAPI)api).Input.MouseYaw = 0;
                }

                if (byPlayer.Entity.Pos.AsBlockPos.West().Equals(adjacentTable)) {

                    playerData.SetSittingDirection(Cardinal.West);
                    ((ICoreClientAPI)api).Input.MouseYaw = 0.5f * GameMath.TWOPI;
                }

                if (byPlayer.Entity.Pos.AsBlockPos.South().Equals(adjacentTable)) {

                    playerData.SetSittingDirection(Cardinal.South);
                    ((ICoreClientAPI)api).Input.MouseYaw = 0.75f * GameMath.TWOPI;
                }

                if (byPlayer.Entity.Pos.AsBlockPos.North().Equals(adjacentTable)) {

                    playerData.SetSittingDirection(Cardinal.North);
                    ((ICoreClientAPI)api).Input.MouseYaw = 0.25f * GameMath.TWOPI;
                }
            }
        }

        private BlockPos GetAdjacentTable(IWorldAccessor world, BlockPos chairPosition) {

            BlockPos tablePosition = new BlockPos();

            if (world.BlockAccessor.GetBlock(tablePosition.Set(chairPosition.X - 1, chairPosition.Y, chairPosition.Z)).Code.FirstPathPart(0).Equals("table-normal"))
                return tablePosition;
            if (world.BlockAccessor.GetBlock(tablePosition.Set(chairPosition.X + 1, chairPosition.Y, chairPosition.Z)).Code.FirstPathPart(0).Equals("table-normal"))
                return tablePosition;
            if (world.BlockAccessor.GetBlock(tablePosition.Set(chairPosition.X, chairPosition.Y, chairPosition.Z - 1)).Code.FirstPathPart(0).Equals("table-normal"))
                return tablePosition;
            if (world.BlockAccessor.GetBlock(tablePosition.Set(chairPosition.X, chairPosition.Y, chairPosition.Z + 1)).Code.FirstPathPart(0).Equals("table-normal"))
                return tablePosition;

            return null;
        }

        private void PlayerStandUp(IPlayer byPlayer) {

            // Modify eye height
            byPlayer.Entity.Properties.SetEyeHeight(DEFAULTEYEHEIGHT);

            // Set sitting direction to something not used
            playerManager.GetOrAddPlayerData(byPlayer).SetSittingDirection(Cardinal.SouthWest);

            // Send packet to server to sync all clients
            IClientNetworkChannel networkChannel = (IClientNetworkChannel)api.Network.GetChannel("networksit");
            networkChannel.SendPacket(new NetworkAnimationSit() { isSitting = false, playerUID = byPlayer.PlayerUID });

            // Set player's status to not sitting
            PlayerManager.Instance.GetOrAddPlayerData(byPlayer).TogglePlayerSitting();
        }
    }
}