using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AculemMods {

    public class BlockOmokTableTop : Block {

        // Loads all available moves
        public override void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick) {

            if (api is ICoreClientAPI cAPI) {

                BlockPos blockPos = blockSel.Position;
                BEOmokTableTop beOmok = (BEOmokTableTop)cAPI.World.BlockAccessor.GetBlockEntity(blockPos);

                // Load available space mesh if the looked at spot is free
                if (beOmok == null || !IsViableSpace(beOmok, blockSel, out int pieceX, out int pieceZ))
                    return;

                RendererOmok renderer = beOmok.OmokRenderer;

                // Store Selected Omok Board Position
                PlayerData playerData = PlayerManager.Instance.GetPlayerData(byPlayer);
                playerData.SelectedOmokBoardPos = blockPos;

                renderer.LoadAvailableMovesMesh(cAPI, byPlayer, blockSel.Position, pieceX, pieceZ);
            }

            base.OnBeingLookedAt(byPlayer, blockSel, firstTick);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {

            BlockPos blockPos = blockSel.Position;
            BEOmokTableTop beOmok = (BEOmokTableTop) api.World.BlockAccessor.GetBlockEntity(blockPos);

            // Continue if the clicked spot is a move that makes sense
            if (beOmok == null || !IsViableSpace(beOmok, blockSel, out int pieceX, out int pieceZ))
                return false;

            RendererOmok renderer = beOmok.OmokRenderer;

            bool whitesTurn = beOmok.WhitesTurn;

            if (whitesTurn)
                beOmok.PlaceWhitePiece(pieceX, pieceZ);
            else
                beOmok.PlaceBlackPiece(pieceX, pieceZ);

            // Reload Available Moves Mesh
            if (world.Side == EnumAppSide.Client) {

                ICoreClientAPI cAPI = (ICoreClientAPI) api;

                if (whitesTurn)
                    renderer.LoadPlacedMovesMesh(cAPI, beOmok.PlacedWhitePieces, true);
                else
                    renderer.LoadPlacedMovesMesh(cAPI, beOmok.PlacedBlackPieces, false);

                renderer.LoadAvailableMovesMesh(cAPI, byPlayer, blockSel.Position, -1, -1);

                // Victory Condition Check
                if (!beOmok.GameIsOver) {

                    string victory = beOmok.CheckVictoryConditions();

                    if (victory != "" && cAPI != null)
                        cAPI.SendChatMessage(victory);
                }
            }

            beOmok.MarkDirty(true);

            return true;
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos) {

            // Dispose and Destroy Renderers when block is removed
            if (api is ICoreClientAPI cAPI) {

                BEOmokTableTop beOmok = (BEOmokTableTop)cAPI.World.BlockAccessor.GetBlockEntity(pos);

                if (beOmok != null)
                    beOmok.OmokRenderer.Destroy();
            }
            
            base.OnBlockRemoved(world, pos);
        }

        /*
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {

            api.Logger.Debug("Hey ooooh");

            // Restart game if middle mouse is clicked
            if (api is ICoreClientAPI cAPI) {

                BEOmokTableTop beOmok = (BEOmokTableTop)cAPI.World.BlockAccessor.GetBlockEntity(pos);
                beOmok.RestartGame();
                return null;
            }

            return base.OnPickBlock(world, pos);
        }
        */

        private bool IsViableSpace(BEOmokTableTop beOmok, BlockSelection blockSel, out int pieceX, out int pieceZ) {

            double hitX = blockSel.HitPosition.X;
            double hitZ = blockSel.HitPosition.Z;

            int pixelX = (int)(hitX * 32);
            int pixelZ = (int)(hitZ * 32);

            pieceX = ((pixelX - 3) / 3);
            pieceZ = ((pixelZ - 3) / 3);

            // Return if clicked on a nonviable space
            if (pixelX <= 2 || pixelX >= 30 || pixelZ <= 2 || pixelZ >= 30 || pixelX % 3 == 2 || pixelZ % 3 == 2)
                return false;

            // Return if piece is already placed
            if (beOmok.IsPiecePlaced(pieceX, pieceZ))
                return false;

            return true;
        }
    }
}