using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace AculemMods {

    public class BlockOmokTableTop : Block {

        private ICoreAPI api;

        private WorldInteraction helpBlacksTurn;
        private WorldInteraction helpWhitesTurn;
        private WorldInteraction helpBlackWon;
        private WorldInteraction helpWhiteWon;

        public override void OnLoaded(ICoreAPI api) {

            this.api = api;

            helpBlacksTurn = new WorldInteraction() {

                ActionLangCode = "omok:blockhelp-blacksturn",
                MouseButton = EnumMouseButton.None
            };

            helpWhitesTurn = new WorldInteraction() {

                ActionLangCode = "omok:blockhelp-whitesturn",
                MouseButton = EnumMouseButton.None
            };

            helpBlackWon = new WorldInteraction() {

                ActionLangCode = "omok:blockhelp-blackwins",
                HotKeyCode = "toolmodeselect",
                MouseButton = EnumMouseButton.None
            };

            helpWhiteWon = new WorldInteraction() {

                ActionLangCode = "omok:blockhelp-whitewins",
                HotKeyCode = "toolmodeselect",
                MouseButton = EnumMouseButton.None
            };
        }

        // Loads all available moves
        public override void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick) {

            if (api is ICoreClientAPI cAPI) {

                BlockPos blockPos = blockSel.Position;
                BEOmokTableTop beOmok = (BEOmokTableTop)cAPI.World.BlockAccessor.GetBlockEntity(blockPos);

                if (beOmok == null)
                    return;

                RendererOmok renderer = beOmok.OmokRenderer;

                // Load available space mesh if the looked at spot is free
                if (beOmok == null || !IsViableSpace(beOmok, blockSel, out int pieceX, out int pieceZ)) {

                    renderer.DisposeAvailableMovesMesh();
                    return;
                }

                // Store Selected Omok Board Position
                PlayerData playerData = PlayerManager.Instance.GetOrAddPlayerData(byPlayer);
                playerData.SelectedOmokBoardPos = blockPos;

                renderer.LoadAvailableMovesMesh(cAPI, byPlayer, blockSel.Position, pieceX, pieceZ);
            }

            base.OnBeingLookedAt(byPlayer, blockSel, firstTick);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {

            BlockPos blockPos = blockSel.Position;
            BEOmokTableTop beOmok = (BEOmokTableTop) api.World.BlockAccessor.GetBlockEntity(blockPos);

            // Continue if the clicked spot is an available space
            if (beOmok == null || !IsViableSpace(beOmok, blockSel, out int pieceX, out int pieceZ))
                return true;

            // Determine next piece to be placed
            bool whitesTurn = beOmok.WhitesTurn;
            int piecesPlayed = beOmok.PiecesPlayed();

            // If two player, determine who's playing and whose turn it is before placing a piece
            if (beOmok.IsTwoPlayer) {

                // Assign ID if it's a player's first turn
                if (piecesPlayed == 0) // Assign first player
                    beOmok.FirstPlayerID = byPlayer.PlayerUID;

                else if (piecesPlayed == 1) { // Assign second player

                    // If second player is a new player, assign second player, otherwise return
                    if (byPlayer.PlayerUID != beOmok.FirstPlayerID)
                        beOmok.SecondPlayerID = byPlayer.PlayerUID;
                    else
                        return true;
                }

                // Return if it's not the player's turn
                if (!whitesTurn && byPlayer.PlayerUID != beOmok.FirstPlayerID)
                    return true;

                if (whitesTurn && byPlayer.PlayerUID != beOmok.SecondPlayerID)
                    return true;
            }

            if (whitesTurn)
                beOmok.PlaceWhitePiece(pieceX, pieceZ);
            else
                beOmok.PlaceBlackPiece(pieceX, pieceZ);

            // Victory Condition Check
            string victoryText = "";

            if (!beOmok.GameIsOver)
                victoryText = beOmok.CheckVictoryConditions();

            // Play piece setting sound
            int random = new Random().Next(1, 3);
            AssetLocation placeSound = new AssetLocation("game:sounds/block/loosestone" + random);
            world.PlaySoundAt(placeSound, blockPos.X, blockPos.Y, blockPos.Z, byPlayer, true, 10, 1);

            /*
            // Reload Available Moves Mesh
            if (world.Side == EnumAppSide.Client) {
                
                RendererOmok renderer = beOmok.OmokRenderer;

                ICoreClientAPI cAPI = (ICoreClientAPI) api;

                if (whitesTurn)
                    renderer.LoadPlacedMovesMesh(cAPI, beOmok.PlacedWhitePieces, true);
                else
                    renderer.LoadPlacedMovesMesh(cAPI, beOmok.PlacedBlackPieces, false);

                renderer.LoadAvailableMovesMesh(cAPI, byPlayer, blockSel.Position, -1, -1);
            }
            */

            if (world.Side == EnumAppSide.Server) {

                ICoreServerAPI sAPI = (ICoreServerAPI)api;

                if (victoryText != "")
                    foreach (IPlayer nearbyPlayer in api.World.GetPlayersAround(blockSel.Position.ToVec3d(), 10, 10))
                        sAPI.SendMessage(nearbyPlayer, 0, victoryText, EnumChatType.OwnMessage);
            }

            beOmok.MarkDirty(true);
            beOmok.UpdateClients();

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

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {

            if (api is ICoreClientAPI cAPI) {

                BlockPos blockPos = selection.Position;
                BEOmokTableTop beOmok = (BEOmokTableTop)cAPI.World.BlockAccessor.GetBlockEntity(blockPos);

                // Display helpful text over the board
                if (beOmok == null)
                    return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);

                else if (beOmok.GameIsOver && beOmok.WhiteWon)
                    return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append<WorldInteraction>(helpWhiteWon);

                else if (beOmok.GameIsOver && !beOmok.WhiteWon)
                    return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append<WorldInteraction>(helpBlackWon);

                else if (beOmok.WhitesTurn)
                    return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append<WorldInteraction>(helpWhitesTurn);

                else if (!beOmok.WhitesTurn)
                    return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append<WorldInteraction>(helpBlacksTurn);
            }

            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        }

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