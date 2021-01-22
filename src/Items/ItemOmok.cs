using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AculemMods {

    public class ItemOmok : Item {

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {

            BEOmokTableTop bec = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BEOmokTableTop;

            // Called if attempting to place board
            if (bec == null && byEntity.Controls.Sneak) {

                IWorldAccessor world = byEntity.World;
                BlockOmokTableTop blockOmok = (BlockOmokTableTop) world.GetBlock(new AssetLocation("omok:blockomok"));

                BlockPos pos = blockSel.Position.AddCopy(blockSel.Face);

                if (blockOmok != null)
                    world.BlockAccessor.SetBlock(blockOmok.BlockId, pos);
                else
                    byEntity.World.Logger.Debug("Block is Null");

                if (blockOmok.Sounds != null)
                    world.PlaySoundAt(blockOmok.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);

                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
        }
    }
}