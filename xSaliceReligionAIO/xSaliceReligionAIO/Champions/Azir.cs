using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace xSaliceReligionAIO.Champions
{
    class Azir : Champion
    {
        public Azir()
        {
            LoadSpells();
            LoadMenu();
        }

        private static Obj_AI_Hero wTargetsss = null;
        private static Vector3 rVec;

        private void LoadSpells()
        {
            //intalize spell
            Q = new Spell(SpellSlot.Q, 850);
            QExtend = new Spell(SpellSlot.Q, 1150);
            W = new Spell(SpellSlot.W, 450);
            E = new Spell(SpellSlot.E, 2000);
            R = new Spell(SpellSlot.R, 450);

            Q.SetSkillshot(0.1f, 100, 1700, false, SkillshotType.SkillshotLine);
            QExtend.SetSkillshot(0.1f, 100, 1700, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 100, 1200, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.5f, 700, 1400, false, SkillshotType.SkillshotLine);
        }

        private void LoadMenu()
        {

            var key = new Menu("Key", "Key");
            {
                key.AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));
                key.AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("escape", "Escape").SetValue(new KeyBind(menu.Item("Flee_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));
                key.AddItem(new MenuItem("insec", "Insec Selected target").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("qeCombo", "Q->E stun Nearest target").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                //add to menu
                menu.AddSubMenu(key);
            }

            //Spell Menu
            var spell = new Menu("Spell", "Spell");
            {

                var qMenu = new Menu("QSpell", "QSpell"); { 
                    qMenu.AddItem(new MenuItem("qOutRange", "Only When Enemy out of Range").SetValue(true));
                    qMenu.AddItem(new MenuItem("qExtend", "Use Extended Q Range").SetValue(true));
                    qMenu.AddItem(new MenuItem("qBehind", "Try to Q Behind target").SetValue(true));
                    qMenu.AddItem(new MenuItem("qMulti", "Q if 2+ Soilder").SetValue(true));
                    qMenu.AddItem(new MenuItem("qHit", "Q HitChance").SetValue(new Slider(3, 1, 3)));
                    spell.AddSubMenu(qMenu);
                }
                //W Menu
                var wMenu = new Menu("WSpell", "WSpell"); {
                    wMenu.AddItem(new MenuItem("wAtk", "Always Atk Enemy").SetValue(true));
                    wMenu.AddItem(new MenuItem("wQ", "Use WQ Poke").SetValue(true));
                    spell.AddSubMenu(wMenu);
                }
                //E Menu
                var eMenu =  new Menu("ESpell", "ESpell");
                {
                    eMenu.AddItem(new MenuItem("eGap", "GapClose if out of Q Range").SetValue(false));
                    eMenu.AddItem(new MenuItem("eKill", "If Killable Combo").SetValue(true));
                    eMenu.AddItem(new MenuItem("eKnock", "Always Knockup/DMG").SetValue(false));
                    eMenu.AddItem(new MenuItem("eHP", "if HP >").SetValue(new Slider(70, 0, 100)));
                    spell.AddSubMenu(eMenu);
                }
                //R Menu
                var rMenu = new Menu("RSpell", "RSpell");{
                    rMenu.AddItem(new MenuItem("rHP", "if HP <").SetValue(new Slider(20, 0, 100)));
                    rMenu.AddItem(new MenuItem("rHit", "If Hit >= Target").SetValue(new Slider(3, 0, 5)));
                    rMenu.AddItem(new MenuItem("rWall", "R Enemy Into Wall").SetValue(true));
                    spell.AddSubMenu(rMenu);
                }
                menu.AddSubMenu(spell);
            }

            //Combo menu:
            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                combo.AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
                combo.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                combo.AddItem(new MenuItem("ignite", "Use Ignite").SetValue(true));
                combo.AddItem(new MenuItem("igniteMode", "Mode").SetValue(new StringList(new[] {"Combo", "KS"}, 0)));
                menu.AddSubMenu(combo);
            }

            //Harass menu:
            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
                harass.AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
                harass.AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
                menu.AddSubMenu(harass);
            }

            //killsteal
            var killSteal = new Menu("KillSteal", "KillSteal");
            {
                killSteal.AddItem(new MenuItem("smartKS", "Use Smart KS System").SetValue(true));
                killSteal.AddItem(new MenuItem("eKS", "Use E KS").SetValue(false));
                killSteal.AddItem(new MenuItem("wqKS", "Use WQ KS").SetValue(true));
                killSteal.AddItem(new MenuItem("qeKS", "Use WQE KS").SetValue(true));
                killSteal.AddItem(new MenuItem("rKS", "Use R KS").SetValue(true));
                menu.AddSubMenu(killSteal);
            }

            //farm menu
            var farm = new Menu("Farm", "Farm");
            {
                farm.AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
                farm.AddItem(new MenuItem("qFarm", "Only Q if > minion").SetValue(new Slider(3, 0, 5)));
                menu.AddSubMenu(farm);
            }

            //Misc Menu:
            var misc = new Menu("Misc", "Misc");
            {
                misc.AddItem(new MenuItem("UseInt", "Use E to Interrupt").SetValue(true));
                menu.SubMenu("Misc").AddItem(new MenuItem("UseGap", "Use E for GapCloser").SetValue(true));
                menu.SubMenu("Misc").AddItem(new MenuItem("fastEscape", "Escape Mode 2").SetValue(true));
                menu.SubMenu("Misc").AddItem(new MenuItem("packet", "Use Packets").SetValue(true));
            }
            //Drawings menu:
            menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("QExtendRange", "Q Extended range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("slaveDmg", "Draw Slave AA Needed").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
        }
    }
}
