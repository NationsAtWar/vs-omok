using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AculemMods {

    public class BlockOmokTableTop : Block {

        RendererOmok rendererOmok;

        // Loads all available moves
        public override void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick) {

            if (api is ICoreClientAPI cAPI) {

                BlockPos blockPos = blockSel.Position;
                BEOmokTableTop beOmok = (BEOmokTableTop)cAPI.World.BlockAccessor.GetBlockEntity(blockPos);

                // Load available space mesh if the looked at spot is free
                if (!IsViableSpace(beOmok, blockSel, out int pieceX, out int pieceZ))
                    return;

                LoadAvailableMovesMesh(byPlayer, blockSel.Position, pieceX, pieceZ);
            }

            base.OnBeingLookedAt(byPlayer, blockSel, firstTick);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {

            if (api is ICoreClientAPI cAPI) {

                BlockPos blockPos = blockSel.Position;
                BEOmokTableTop beOmok = (BEOmokTableTop) cAPI.World.BlockAccessor.GetBlockEntity(blockPos);

                // Continue if the clicked spot is a move that makes sense
                if (!IsViableSpace(beOmok, blockSel, out int pieceX, out int pieceZ))
                    return false;

                bool whitesTurn = beOmok.WhitesTurn;

                if (whitesTurn) {

                    beOmok.PlaceWhitePiece(pieceX, pieceZ);
                    LoadPlacedMovesMesh(cAPI, blockPos, beOmok.PlacedWhitePieces, true);
                } else {

                    beOmok.PlaceBlackPiece(pieceX, pieceZ);
                    LoadPlacedMovesMesh(cAPI, blockPos, beOmok.PlacedBlackPieces, false);
                }

                // Reload Available Moves Mesh
                LoadAvailableMovesMesh(byPlayer, blockSel.Position, -1, -1);

                // Victory Condition Check
                if (!beOmok.GameIsOver) {

                    string victory = beOmok.CheckVictoryConditions();

                    if (victory != "")
                        cAPI.SendChatMessage(victory);
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        // Prevents the placement or use of other items/blocks while interacting with this block
        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {

            return false;
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos) {

            // Dispose and Destroy Renderers when block is removed
            if (api is ICoreClientAPI cAPI) {

                BEOmokTableTop beOmok = (BEOmokTableTop)cAPI.World.BlockAccessor.GetBlockEntity(pos);
                beOmok.OmokRenderer.Destroy();
            }
            
            base.OnBlockRemoved(world, pos);
        }

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

        private void LoadAvailableMovesMesh(IPlayer byPlayer, BlockPos blockPos, int pieceX, int pieceZ) {

            ICoreClientAPI cAPI = (ICoreClientAPI)api;

            IBlockAccessor blockAccessor = byPlayer.Entity.World.BlockAccessor;
            BlockSelection blockSelection = cAPI.World.Player.CurrentBlockSelection;

            if (blockSelection == null)
                return;

            // Player is selecting an Omok Board
            if (blockAccessor.GetBlock(blockPos) is BlockOmokTableTop) {

                MeshData availableMoveMesh = new MeshData(24, 36, false, false, true, false);
                availableMoveMesh.SetMode(EnumDrawMode.Lines);

                int greenCol = (156 << 24) | (100 << 16) | (200 << 8) | (100);

                MeshData greenVoxelMesh = LineMeshUtil.GetCube(greenCol);

                for (int i = 0; i < greenVoxelMesh.xyz.Length; i++)
                    greenVoxelMesh.xyz[i] = greenVoxelMesh.xyz[i] / 32f + 1 / 32f;

                MeshData voxelMeshOffset = greenVoxelMesh.Clone();

                // TODO: Optimize, no need to loop through entire grid
                for (int x = 3; x < 28; x++) {
                    int y = 3;
                    for (int z = 3; z < 28; z++) {

                        if (x % 3 != 0 || z % 3 != 0)
                            continue;

                        if (pieceX != (x / 3) - 1 || pieceZ != (z / 3) - 1)
                            continue;

                        float px = x / 32f;
                        float py = y / 40f + 0.001f;
                        float pz = z / 32f;

                        for (int i = 0; i < greenVoxelMesh.xyz.Length; i += 3) {

                            voxelMeshOffset.xyz[i] = px + greenVoxelMesh.xyz[i];
                            voxelMeshOffset.xyz[i + 1] = py + greenVoxelMesh.xyz[i + 1];
                            voxelMeshOffset.xyz[i + 2] = pz + greenVoxelMesh.xyz[i + 2];
                        }

                        voxelMeshOffset.Rgba = greenVoxelMesh.Rgba;

                        availableMoveMesh.AddMeshData(voxelMeshOffset);
                    }
                }

                if (byPlayer.Entity.World.BlockAccessor.GetBlockEntity(blockPos) is BEOmokTableTop beOmok) {

                    rendererOmok = beOmok.OmokRenderer;
                    rendererOmok.AvailableMovesMesh?.Dispose();
                    rendererOmok.AvailableMovesMesh = cAPI.Render.UploadMesh(availableMoveMesh);
                }

                // Store Selected Omok Board Position
                PlayerData playerData = PlayerManager.Instance.GetPlayerData(byPlayer);
                playerData.SelectedOmokBoardPos = blockPos;
            }
        }

        public void LoadPlacedMovesMesh(ICoreClientAPI cAPI, BlockPos blockPos, bool[,] placedPieces, bool whitesTurn) {

            MeshData placedMovesMesh = new MeshData(24, 36, false);

            float subPixelPaddingx = cAPI.BlockTextureAtlas.SubPixelPaddingX;
            float subPixelPaddingy = cAPI.BlockTextureAtlas.SubPixelPaddingY;

            TextureAtlasPosition tpos = cAPI.BlockTextureAtlas.UnknownTexturePosition;
            MeshData singleVoxelMesh = CubeMeshUtil.GetCubeOnlyScaleXyz(1 / 32f, 1 / 32f, new Vec3f(1 / 32f, 1 / 32f, 1 / 32f));
            singleVoxelMesh.Rgba = new byte[6 * 4 * 4].Fill((byte)255);
            CubeMeshUtil.SetXyzFacesAndPacketNormals(singleVoxelMesh);

            for (int i = 0; i < singleVoxelMesh.Uv.Length; i++) {
                if (i % 2 > 0) {
                    singleVoxelMesh.Uv[i] = tpos.y1 + singleVoxelMesh.Uv[i] * 2f / cAPI.BlockTextureAtlas.Size.Height - subPixelPaddingy;
                } else {
                    singleVoxelMesh.Uv[i] = tpos.x1 + singleVoxelMesh.Uv[i] * 2f / cAPI.BlockTextureAtlas.Size.Width - subPixelPaddingx;
                }
            }

            singleVoxelMesh.XyzFaces = (byte[])CubeMeshUtil.CubeFaceIndices.Clone();
            singleVoxelMesh.XyzFacesCount = 6;

            singleVoxelMesh.SeasonColorMapIds = new byte[6];
            singleVoxelMesh.ClimateColorMapIds = new byte[6];
            singleVoxelMesh.ColorMapIdsCount = 6;

            MeshData voxelMeshOffset = singleVoxelMesh.Clone();

            for (int x = 1; x < 10; x++) {
                for (int y = 2; y < 3; y++) {
                    for (int z = 1; z < 10; z++) {

                        if (!placedPieces[x - 1, z - 1])
                            continue;

                        float px = x / 16f * 1.5f;
                        float py = y / 24f;
                        float pz = z / 16f * 1.5f;

                        for (int i = 0; i < singleVoxelMesh.xyz.Length; i += 3) {

                            voxelMeshOffset.xyz[i] = px + singleVoxelMesh.xyz[i];
                            voxelMeshOffset.xyz[i + 1] = py + singleVoxelMesh.xyz[i + 1];
                            voxelMeshOffset.xyz[i + 2] = pz + singleVoxelMesh.xyz[i + 2];
                        }

                        float offsetX = ((((x + 4 * y) % 16f / 16f)) * 32f) / cAPI.BlockTextureAtlas.Size.Width;
                        float offsetY = (pz * 32f) / cAPI.BlockTextureAtlas.Size.Height;

                        for (int i = 0; i < singleVoxelMesh.Uv.Length; i += 2) {

                            voxelMeshOffset.Uv[i] = singleVoxelMesh.Uv[i] + offsetX;
                            voxelMeshOffset.Uv[i + 1] = singleVoxelMesh.Uv[i + 1] + offsetY;
                        }

                        placedMovesMesh.AddMeshData(voxelMeshOffset);
                    }
                }
            }

            if (cAPI.World.Player.Entity.World.BlockAccessor.GetBlockEntity(blockPos) is BEOmokTableTop beOmok) {

                RendererOmok rendererOmok = beOmok.OmokRenderer;

                if (whitesTurn) {

                    rendererOmok.PlacedWhiteMovesMesh?.Dispose();
                    rendererOmok.PlacedWhiteMovesMesh = cAPI.Render.UploadMesh(placedMovesMesh);
                } else {

                    rendererOmok.PlacedBlackMovesMesh?.Dispose();
                    rendererOmok.PlacedBlackMovesMesh = cAPI.Render.UploadMesh(placedMovesMesh);
                }
            }

            // Store Selected Omok Board Position
            PlayerData playerData = PlayerManager.Instance.GetPlayerData(cAPI.World.Player);
            playerData.SelectedOmokBoardPos = blockPos;
        }
    }
}