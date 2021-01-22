using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AculemMods {

    public class BEOmokTableTop : BlockEntity {

        private ICoreClientAPI cAPI;
        private RendererOmok omokRenderer;

        private bool[,] placedWhitePieces = new bool[9,9];
        private bool[,] placedBlackPieces = new bool[9,9];

        private bool whitesTurn = false;
        private bool gameIsOver = false;

        public RendererOmok OmokRenderer { get => omokRenderer; set => omokRenderer = value; }
        public bool[,] PlacedWhitePieces { get => placedWhitePieces; }
        public bool[,] PlacedBlackPieces { get => placedBlackPieces; }
        public bool WhitesTurn { get => whitesTurn; set => whitesTurn = value; }
        public bool GameIsOver { get => gameIsOver; }

        public override void Initialize(ICoreAPI api) {

            if (api is ICoreClientAPI capi) {

                cAPI = capi;
                omokRenderer = RegisterRenderer();
            }

            base.Initialize(api);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {

            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {

            base.ToTreeAttributes(tree);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {

            base.GetBlockInfo(forPlayer, sb);
        }

        private RendererOmok RegisterRenderer() {

            cAPI.Event.RegisterRenderer(OmokRenderer = new RendererOmok(Pos, cAPI), EnumRenderStage.Opaque);
            return OmokRenderer;
        }

        public void PlaceWhitePiece(int xPos, int zPos) {

            placedWhitePieces[xPos, zPos] = true;
            whitesTurn = false;
        }

        public void PlaceBlackPiece(int xPos, int zPos) {

            placedBlackPieces[xPos, zPos] = true;
            whitesTurn = true;
        }

        public bool[,] GetAllPlacedPieces() {

            bool[,] allPlacedPieces = new bool[9, 9];

            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    if (placedWhitePieces[x, y] || placedBlackPieces[x, y])
                        allPlacedPieces[x, y] = true;

            return allPlacedPieces;
        }

        public bool IsPiecePlaced(int x, int y) {

            if (placedWhitePieces[x, y] || placedBlackPieces[x, y])
                return true;

            return false;
        }

        public string CheckVictoryConditions() {

            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++) {

                    if (placedWhitePieces[x,y]) {

                        if ((x <= 4 && placedWhitePieces[x+1,y] && placedWhitePieces[x+2,y] && placedWhitePieces[x+3,y] && placedWhitePieces[x+4,y]) ||
                            (y <= 4 && placedWhitePieces[x,y+1] && placedWhitePieces[x,y+2] && placedWhitePieces[x,y+3] && placedWhitePieces[x,y+4]) ||
                            (x <= 4 && y <=4 && placedWhitePieces[x+1,y+1] && placedWhitePieces[x+2,y+2] && placedWhitePieces[x+3,y+3] && placedWhitePieces[x+4,y+4])) {

                            gameIsOver = true;
                            return "White Wins!";
                        }
                    }

                    if (placedBlackPieces[x,y]) {

                        if ((x <= 4 && placedBlackPieces[x+1,y] && placedBlackPieces[x+2,y] && placedBlackPieces[x+3,y] && placedBlackPieces[x+4,y]) ||
                            (y <= 4 && placedBlackPieces[x,y+1] && placedBlackPieces[x,y+2] && placedBlackPieces[x,y+3] && placedBlackPieces[x,y+4]) ||
                            (x <= 4 && y <= 4 && placedBlackPieces[x+1,y+1] && placedBlackPieces[x+2,y+2] && placedBlackPieces[x+3,y+3] && placedBlackPieces[x+4,y+4])) {

                            gameIsOver = true;
                            return "Black Wins!";
                        }
                    }
                }

            return "";
        }

        public void RestartGame() {

            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++) {

                    placedWhitePieces[x, y] = false;
                    placedBlackPieces[x, y] = false;
                }

            gameIsOver = false;
            whitesTurn = false;

            omokRenderer.Dispose();
            return;
        }
    }
}