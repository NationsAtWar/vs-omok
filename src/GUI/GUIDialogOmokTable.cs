using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace AculemMods {

    public class GUIDialogOmokTable : GuiDialogBlockEntity {

        private BEOmokTableTop beOmok;

        public override string ToggleKeyCombinationCode => DialogTitle;

        public GUIDialogOmokTable(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi, BEOmokTableTop beOmok) : base(dialogTitle, blockEntityPos, capi) {

            DialogTitle = dialogTitle;
            this.beOmok = beOmok;

            SetupDialog();
        }

        private void SetupDialog() {

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds testBounds = ElementBounds.Fixed(0, 20, 200, 100);

            // Restart Button Bounds
            ElementBounds oneplayerBounds = ElementBounds.Fixed(20, 40, 200, 50);

            // Restart Button Bounds
            ElementBounds twoPlayerBounds = ElementBounds.Fixed(20, 100, 200, 50);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(testBounds);

            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("dialog-" + DialogTitle, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Start New Game", OnTitleBarCloseClicked)
                .AddButton("Single Player", OnOnePlayerButtonClicked, oneplayerBounds, EnumButtonStyle.Normal, EnumTextOrientation.Center, "oneplayerbutton")
                .AddButton("Two Player", OnTwoPlayerButtonClicked, twoPlayerBounds, EnumButtonStyle.Normal, EnumTextOrientation.Center, "twoplayerbutton")
                .EndChildElements()
                .Compose()
            ;
        }

        private void OnTitleBarCloseClicked() {

            TryClose();
        }

        private bool OnOnePlayerButtonClicked() {

            beOmok.RestartGame(false);
            TryClose();
            return true;
        }

        private bool OnTwoPlayerButtonClicked() {

            beOmok.RestartGame(true);
            TryClose();
            return true;
        }

        public override bool TryClose() {

            return base.TryClose();
        }
    }
}
