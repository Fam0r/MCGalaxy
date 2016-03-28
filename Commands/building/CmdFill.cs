/*
    Copyright 2011 MCForge
    
    Dual-licensed under the    Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;
using MCGalaxy.Drawing.Brushes;
using MCGalaxy.Drawing.Ops;

namespace MCGalaxy.Commands {
    
    public sealed class CmdFill : DrawCmd {
        
        public override string name { get { return "fill"; } }
        public override string shortcut { get { return "f"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }
        protected override string PlaceMessage { get { return "Destroy the block you wish to fill."; } }
        
        protected override DrawMode ParseMode(string msg) {
            if (msg == "normal") return DrawMode.solid;
            else if (msg == "up") return DrawMode.up;
            else if (msg == "down") return DrawMode.down;
            else if (msg == "layer") return DrawMode.layer;
            else if (msg == "vertical_x") return DrawMode.verticalX;
            else if (msg == "vertical_z") return DrawMode.verticalZ;
            return DrawMode.normal;
        }
        
        protected override void Blockchange1(Player p, ushort x, ushort y, ushort z, byte type, byte extType) {
            p.ClearBlockchange();
            CatchPos cpos = (CatchPos)p.blockchangeObject;
            byte oldType = p.level.GetTile(x, y, z), oldExtType = 0;
            if (oldType == Block.custom_block)
                oldExtType = p.level.GetExtTile(x, y, z);
            p.RevertBlock(x, y, z);
            GetRealBlock(type, extType, p, ref cpos);

            if (cpos.type == oldType) {
                if (cpos.type != Block.custom_block) {
                    Player.SendMessage(p, "Cannot fill with the same type."); return;
                }
                if (cpos.extType == oldExtType) {
                    Player.SendMessage(p, "Cannot fill with the same type."); return;
                }
            }
            if (!Block.canPlace(p, oldType) && !Block.BuildIn(oldType)) { Player.SendMessage(p, "Cannot fill with that."); return; }

            SparseBitSet bits = new SparseBitSet(p.level.Width, p.level.Height, p.level.Length);
            List<int> buffer = new List<int>(), origins = new List<int>();
            FloodFill(p, x, y, z, oldType, oldExtType, cpos.mode, bits, buffer, origins, 0);

            int totalFill = origins.Count;
            for (int i = 0; i < totalFill; i++) {
                int pos = origins[i];
                p.level.IntToPos(pos, out x, out y, out z);
                FloodFill(p, x, y, z, oldType, oldExtType, cpos.mode, bits, buffer, origins, 0);
                totalFill = origins.Count;
            }
            
            FillDrawOp op = new FillDrawOp();
            op.Positions = buffer;
            int brushOffset = cpos.mode == DrawMode.normal ? 0 : 1;
            Brush brush = GetBrush(p, cpos, brushOffset);
            if (brush == null) return;
            if (!DrawOp.DoDrawOp(op, brush, p, cpos.x, cpos.y, cpos.z, cpos.x, cpos.y, cpos.z))
                return;
            bits.Clear();
            op.Positions = null;

            if (p.staticCommands)
                p.Blockchange += new Player.BlockchangeEventHandler(Blockchange1);
        }
        
        protected override void Blockchange2(Player p, ushort x, ushort y, ushort z, byte type, byte extType) { }

        void FloodFill(Player p, ushort x, ushort y, ushort z, byte oldType, byte oldExtType, DrawMode fillType,
                       SparseBitSet bits, List<int> buffer, List<int> origins, int depth) {
            if (bits.Get(x, y, z)) return;
            int index = p.level.PosToInt(x, y, z);
            if (depth > 2000) { origins.Add(index); return; }
            bits.Set(x, y, z, true);
            buffer.Add(index);

            if (fillType != DrawMode.verticalX) { // x
                if (CheckTile(p, (ushort)(x + 1), y, z, oldType, oldExtType))
                    FloodFill(p, (ushort)(x + 1), y, z, oldType, oldExtType, fillType, bits, buffer, origins, depth + 1);
                if (CheckTile(p, (ushort)(x - 1), y, z, oldType, oldExtType))
                    FloodFill(p, (ushort)(x - 1), y, z, oldType, oldExtType, fillType, bits, buffer, origins, depth + 1);
            }

            if (fillType != DrawMode.verticalZ) { // z
                if (CheckTile(p, x, y, (ushort)(z + 1), oldType, oldExtType))
                    FloodFill(p, x, y, (ushort)(z + 1), oldType, oldExtType, fillType, bits, buffer, origins, depth + 1);
                if (CheckTile(p, x, y, (ushort)(z - 1), oldType, oldExtType))
                    FloodFill(p, x, y, (ushort)(z - 1), oldType, oldExtType, fillType, bits, buffer, origins, depth + 1);
            }

            if (!(fillType == DrawMode.down || fillType == DrawMode.layer)) { // y up
                if (CheckTile(p, x, (ushort)(y + 1), z, oldType, oldExtType))
                    FloodFill(p, x, (ushort)(y + 1), z, oldType, oldExtType, fillType, bits, buffer, origins, depth + 1);
            }

            if (!(fillType == DrawMode.up || fillType == DrawMode.layer)) { // y down
                if (CheckTile(p, x, (ushort)(y - 1), z, oldType, oldExtType))
                    FloodFill(p, x, (ushort)(y - 1), z, oldType, oldExtType, fillType, bits, buffer, origins, depth + 1);
            }
        }
        
        bool CheckTile(Player p, ushort x, ushort y, ushort z, byte oldTile, byte oldExtTile) {
            byte tile = p.level.GetTile(x, y, z);

            if (tile == oldTile && tile == Block.custom_block) {
                byte extTile = p.level.GetExtTile(x, y, z);
                return extTile == oldExtTile;
            }
            return tile == oldTile;
        }
        
        public override void Help(Player p) {
            Player.SendMessage(p, "%T/fill [brush args] <mode>");
            Player.SendMessage(p, "%HFills the area specified with the output of the current brush.");
            Player.SendMessage(p, "   %HFor help about brushes, type %T/help brush%H.");
            Player.SendMessage(p, "   %HModes: &fnormal/up/down/layer/vertical_x/vertical_z");            
        }
    }
}
