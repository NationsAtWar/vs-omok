using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AculemMods {

    public class RendererOmok : IRenderer  {

        private MeshRef availableMovesMesh;
        private MeshRef placedWhiteMovesMesh;
        private MeshRef placedBlackMovesMesh;

        private readonly BlockPos boardPos;
        private readonly ICoreClientAPI cAPI;

        public double RenderOrder {
            get { return 0.5; }
        }

        public int RenderRange {
            get { return 24; }
        }

        public MeshRef AvailableMovesMesh { get => availableMovesMesh; set => availableMovesMesh = value; }

        public MeshRef PlacedWhiteMovesMesh { get => placedWhiteMovesMesh; set => placedWhiteMovesMesh = value; }

        public MeshRef PlacedBlackMovesMesh { get => placedBlackMovesMesh; set => placedBlackMovesMesh = value; }

        public RendererOmok(BlockPos blockPos, ICoreClientAPI api) {

            cAPI = api;
            boardPos = blockPos;
        }

        public void LoadAvailableMovesMesh(ICoreClientAPI cAPI, IPlayer byPlayer, BlockPos blockPos, int pieceX, int pieceZ) {

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

                    RendererOmok rendererOmok = beOmok.OmokRenderer;
                    rendererOmok.AvailableMovesMesh?.Dispose();
                    rendererOmok.AvailableMovesMesh = cAPI.Render.UploadMesh(availableMoveMesh);
                }
            }
        }

        public void LoadPlacedMovesMesh(ICoreClientAPI cAPI, bool[,] placedPieces, bool whitesTurn) {

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

            if (whitesTurn) {

                placedWhiteMovesMesh?.Dispose();
                placedWhiteMovesMesh = cAPI.Render.UploadMesh(placedMovesMesh);
            } else {

                placedBlackMovesMesh?.Dispose();
                placedBlackMovesMesh = cAPI.Render.UploadMesh(placedMovesMesh);
            }
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage) {

            if (boardPos == null) {

                cAPI.Logger.Error("Board Pos is null");
                return;
            }

            if (availableMovesMesh != null && !availableMovesMesh.Disposed) {

                IRenderAPI rpi = cAPI.Render;
                IClientWorldAccessor worldAccess = cAPI.World;
                EntityPos plrPos = worldAccess.Player.Entity.Pos;
                Vec3d camPos = worldAccess.Player.Entity.CameraPos;

                Matrixf ModelMat = new Matrixf();
                Vec4f outLineColorMul = new Vec4f(0, 1, 0, 1);

                ModelMat.Identity().Translate(boardPos.X - camPos.X, boardPos.Y - camPos.Y, boardPos.Z - camPos.Z);
                outLineColorMul.A = 1 - GameMath.Clamp((float)Math.Sqrt(plrPos.SquareDistanceTo(boardPos.X, boardPos.Y, boardPos.Z)) / 5 - 1f, 0, 1);
                TextureAtlasPosition tpos = cAPI.BlockTextureAtlas.UnknownTexturePosition;
                int tposID = tpos.atlasTextureId;

                rpi.GlDisableCullFace();
                rpi.GlToggleBlend(true, EnumBlendMode.Standard);

                IStandardShaderProgram prog;

                prog = rpi.PreparedStandardShader(boardPos.X, boardPos.Y, boardPos.Z, outLineColorMul);
                rpi.BindTexture2d(tposID);

                prog.Tex2D = 0;
                prog.NormalShaded = 1;
                prog.ExtraGodray = 0;
                prog.SsaoAttn = 0;
                prog.AlphaTest = 0.05f;
                prog.OverlayOpacity = 1;

                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                prog.ModelMatrix = ModelMat.Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;

                rpi.RenderMesh(availableMovesMesh);
                prog.Stop();

                rpi.GlToggleBlend(true, EnumBlendMode.Standard);
            }

            if (placedWhiteMovesMesh != null && !placedWhiteMovesMesh.Disposed) {

                IRenderAPI rpi = cAPI.Render;
                IClientWorldAccessor worldAccess = cAPI.World;
                Vec3d camPos = worldAccess.Player.Entity.CameraPos;

                Matrixf ModelMat = new Matrixf();

                ModelMat.Identity().Translate(boardPos.X - camPos.X, boardPos.Y - camPos.Y, boardPos.Z - camPos.Z);

                IStandardShaderProgram prog = rpi.PreparedStandardShader(boardPos.X, boardPos.Y, boardPos.Z);

                prog.Use();

                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                prog.ModelMatrix = ModelMat.Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;

                prog.RgbaLightIn = ColorUtil.WhiteArgbVec;
                prog.Tex2D = cAPI.BlockTextureAtlas.AtlasTextureIds[0];

                rpi.RenderMesh(placedWhiteMovesMesh);

                prog.Stop();
            }

            if (placedBlackMovesMesh != null && !placedBlackMovesMesh.Disposed) {

                IRenderAPI rpi = cAPI.Render;
                IClientWorldAccessor worldAccess = cAPI.World;
                Vec3d camPos = worldAccess.Player.Entity.CameraPos;

                Matrixf ModelMat = new Matrixf();

                ModelMat.Identity().Translate(boardPos.X - camPos.X, boardPos.Y - camPos.Y, boardPos.Z - camPos.Z);

                IStandardShaderProgram prog = rpi.PreparedStandardShader(boardPos.X, boardPos.Y, boardPos.Z);

                prog.Use();

                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                prog.ModelMatrix = ModelMat.Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;

                prog.RgbaLightIn = ColorUtil.BlackArgbVec;
                prog.Tex2D = 0;

                rpi.RenderMesh(placedBlackMovesMesh);

                prog.Stop();
            }
        }

        public void DisposeAvailableMovesMesh() {

            availableMovesMesh?.Dispose();
        }

        public void Destroy() {

            availableMovesMesh?.Dispose();
            placedWhiteMovesMesh?.Dispose();
            placedBlackMovesMesh?.Dispose();

            availableMovesMesh = null;
            placedWhiteMovesMesh = null;
            placedBlackMovesMesh = null;
        }

        public void Dispose() {

            availableMovesMesh?.Dispose();
            placedWhiteMovesMesh?.Dispose();
            placedBlackMovesMesh?.Dispose();
        }
    }
}