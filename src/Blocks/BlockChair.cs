using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AculemMods {

    public class BlockChair : Block {

        readonly PlayerManager playerManager = PlayerManager.Instance;

        public override void OnLoaded(ICoreAPI api) {

            base.OnLoaded(api);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {

            bool isSitting = playerManager.GetPlayerData(byPlayer).IsPlayerSitting();

            if (isSitting)
                PlayerStandUp(byPlayer);
            else
                PlayerSitDown(world, byPlayer, blockSel.Position);

            world.Logger.Debug("Players: " + PlayerManager.Instance.GetPlayerData(byPlayer).IsPlayerSitting());

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null) {

            // TODO: Tuck/face table if applicable
            world.Logger.Error("Chair placed");
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        private void PlayerSitDown(IWorldAccessor world, IPlayer byPlayer, BlockPos chairPosition) {

            PlayerData playerData = PlayerManager.Instance.GetPlayerData(byPlayer);

            // Sets the player to the center of the chair
            Vec3d centerOfChair = chairPosition.ToVec3d().Add(0.5, 0.5, 0.5);
            byPlayer.Entity.Pos.SetPos(centerOfChair);

            // Apply sitting animation
            AnimationMetaData data = new AnimationMetaData { Animation = "sitflooridle", Code = "sitflooridle", AnimationSpeed = 1.0f };
            byPlayer.WorldData.EntityPlayer.AnimManager.StartAnimation(data);

            // Set player's status to sitting
            playerData.TogglePlayerSitting();

            // If there is a table next to chair, set player to face chair
            BlockPos adjacentTable = GetAdjacentTable(world, chairPosition);

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

            // Cancel sitting animation
            //byPlayer.Entity.AnimManager.StopAnimation("sitflooridle");
            byPlayer.WorldData.EntityPlayer.AnimManager.StopAnimation("sitflooridle");

            // Set player's status to not sitting
            PlayerManager.Instance.GetPlayerData(byPlayer).TogglePlayerSitting();
        }
    }
}