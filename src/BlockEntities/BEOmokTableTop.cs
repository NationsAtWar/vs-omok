using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace AculemMods {

    public class BEOmokTableTop : BlockEntity {

        private ICoreClientAPI cAPI;
        private RendererOmok omokRenderer;

        private GuiDialog guiDialog;

        private string firstPlayerID;
        private string secondPlayerID;

        private bool isTwoPlayer = false;

        private bool[,] placedWhitePieces = new bool[9,9];
        private bool[,] placedBlackPieces = new bool[9,9];

        private bool whitesTurn = false;
        private bool whiteWon = false;
        private bool gameIsOver = false;

        public RendererOmok OmokRenderer { get => omokRenderer; }
        public bool[,] PlacedWhitePieces { get => placedWhitePieces; }
        public bool[,] PlacedBlackPieces { get => placedBlackPieces; }
        public bool WhitesTurn { get => whitesTurn; }
        public bool WhiteWon { get => whiteWon; }
        public bool GameIsOver { get => gameIsOver; }
        public GuiDialog GuiDialog { get => guiDialog; }
        public string FirstPlayerID { get => firstPlayerID; set => firstPlayerID = value; }
        public string SecondPlayerID { get => secondPlayerID; set => secondPlayerID = value; }
        public bool IsTwoPlayer { get => isTwoPlayer; set => isTwoPlayer = value; }

        private enum EnumPacketType {

            StartOnePlayer,
            StartTwoPlayer
        }

        public BEOmokTableTop() : base() { }

        public override void Initialize(ICoreAPI api) {

            base.Initialize(api);

            if (api is ICoreClientAPI capi) {

                cAPI = capi;
                omokRenderer = RegisterRenderer();

                omokRenderer.LoadPlacedMovesMesh(capi, PlacedWhitePieces, true);
                omokRenderer.LoadPlacedMovesMesh(capi, PlacedBlackPieces, false);

                guiDialog = new GUIDialogOmokTable("guiomoktable-" + Pos.X + "," + Pos.Y + "," + Pos.Z, Pos, cAPI, this);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {

            base.FromTreeAttributes(tree, worldAccessForResolve);

            byte[] deserializedWhitePieces = tree.GetBytes("placedWhitePieces");
            byte[] deserializedBlackPieces = tree.GetBytes("placedBlackPieces");

            whitesTurn = tree.GetBool("whitesTurn", whitesTurn);
            gameIsOver = tree.GetBool("gameIsOver", gameIsOver);

            if (deserializedWhitePieces == null || deserializedBlackPieces == null)
                return;

            placedWhitePieces = PostserializePieces(SerializerUtil.Deserialize<bool[]>(deserializedWhitePieces));
            placedBlackPieces = PostserializePieces(SerializerUtil.Deserialize<bool[]>(deserializedBlackPieces));
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {

            base.ToTreeAttributes(tree);

            byte[] serializedWhitePieces = SerializerUtil.Serialize<bool[]>(PreserializePieces(placedWhitePieces));
            byte[] serializedBlackPieces = SerializerUtil.Serialize<bool[]>(PreserializePieces(placedBlackPieces));

            tree.SetBytes("placedWhitePieces", serializedWhitePieces);
            tree.SetBytes("placedBlackPieces", serializedBlackPieces);
            tree.SetBool("whitesTurn", whitesTurn);
            tree.SetBool("gameIsOver", gameIsOver);
        }

        private bool[] PreserializePieces(bool[,] piecesToPreserialize) {

            bool[] preserializedPieces = new bool[81];

            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    preserializedPieces[x * 9 + y] = piecesToPreserialize[x, y];

            return preserializedPieces;
        }

        private bool[,] PostserializePieces(bool[] piecesToPostserialize) {

            bool[,] postserializedPieces = new bool[9,9];

            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    postserializedPieces[x,y] = piecesToPostserialize[x * 9 + y];

            return postserializedPieces;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {

            base.GetBlockInfo(forPlayer, sb);
        }

        private RendererOmok RegisterRenderer() {

            cAPI.Event.RegisterRenderer(omokRenderer = new RendererOmok(Pos, cAPI), EnumRenderStage.Opaque);
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

                        if ((x <= 4 && placedWhitePieces[x + 1, y] && placedWhitePieces[x + 2, y] && placedWhitePieces[x + 3, y] && placedWhitePieces[x + 4, y]) ||
                            (y <= 4 && placedWhitePieces[x, y + 1] && placedWhitePieces[x, y + 2] && placedWhitePieces[x, y + 3] && placedWhitePieces[x, y + 4]) ||
                            (x >= 4 && y >= 4 && placedWhitePieces[x - 1, y - 1] && placedWhitePieces[x - 2, y - 2] && placedWhitePieces[x - 3, y - 3] && placedWhitePieces[x - 4, y - 4]) ||
                            (x <= 4 && y >= 4 && placedWhitePieces[x + 1, y - 1] && placedWhitePieces[x + 2, y - 2] && placedWhitePieces[x + 3, y - 3] && placedWhitePieces[x + 4, y - 4]) ||
                            (x >= 4 && y <= 4 && placedWhitePieces[x - 1, y + 1] && placedWhitePieces[x - 2, y + 2] && placedWhitePieces[x - 3, y + 3] && placedWhitePieces[x - 4, y + 4]) ||
                            (x <= 4 && y <= 4 && placedWhitePieces[x + 1, y + 1] && placedWhitePieces[x + 2, y + 2] && placedWhitePieces[x + 3, y + 3] && placedWhitePieces[x + 4, y + 4])) {


                            gameIsOver = true;
                            whiteWon = true;
                            return "White Wins!";
                        }
                    }

                    if (placedBlackPieces[x,y]) {

                        if ((x <= 4 && placedBlackPieces[x + 1, y] && placedBlackPieces[x + 2, y] && placedBlackPieces[x + 3, y] && placedBlackPieces[x + 4, y]) ||
                            (y <= 4 && placedBlackPieces[x, y + 1] && placedBlackPieces[x, y + 2] && placedBlackPieces[x, y + 3] && placedBlackPieces[x, y + 4]) ||
                            (x >= 4 && y >= 4 && placedBlackPieces[x - 1, y - 1] && placedBlackPieces[x - 2, y - 2] && placedBlackPieces[x - 3, y - 3] && placedBlackPieces[x - 4, y - 4]) ||
                            (x <= 4 && y >= 4 && placedBlackPieces[x + 1, y - 1] && placedBlackPieces[x + 2, y - 2] && placedBlackPieces[x + 3, y - 3] && placedBlackPieces[x + 4, y - 4]) ||
                            (x >= 4 && y <= 4 && placedBlackPieces[x - 1, y + 1] && placedBlackPieces[x - 2, y + 2] && placedBlackPieces[x - 3, y + 3] && placedBlackPieces[x - 4, y + 4]) ||
                            (x <= 4 && y <= 4 && placedBlackPieces[x + 1, y + 1] && placedBlackPieces[x + 2, y + 2] && placedBlackPieces[x + 3, y + 3] && placedBlackPieces[x + 4, y + 4])) {


                            gameIsOver = true;
                            whiteWon = false;
                            return "Black Wins!";
                        }
                    }
                }

            return "";
        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data) {

            if (packetid == (int)EnumPacketType.StartOnePlayer) {

                RestartGame(false);
            }

            else if (packetid == (int)EnumPacketType.StartTwoPlayer) {

                RestartGame(true);
            }

            base.OnReceivedClientPacket(fromPlayer, packetid, data);
        }

        public int PiecesPlayed() {

            int playedPieces = 0;

            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    if (placedWhitePieces[x, y] || placedBlackPieces[x, y])
                        playedPieces++;

            return playedPieces;
        }

        public void RestartGame(bool twoPlayerGame) {

            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++) {

                    placedWhitePieces[x, y] = false;
                    placedBlackPieces[x, y] = false;
                }

            gameIsOver = false;
            whitesTurn = false;

            if (twoPlayerGame)
                isTwoPlayer = true;
            else
                isTwoPlayer = false;

            if (cAPI != null && cAPI.Side == EnumAppSide.Client) {

                omokRenderer.Dispose();
                MarkDirty(true);

                if (twoPlayerGame)
                    cAPI.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)EnumPacketType.StartTwoPlayer);
                else
                    cAPI.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)EnumPacketType.StartOnePlayer);
            }
        }
    }
}