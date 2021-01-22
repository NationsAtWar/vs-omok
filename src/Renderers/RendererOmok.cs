using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

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