using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Vladionic
{
    class Program
    {
        public const string ChampionName = "Vladimir";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Obj_SpellMissile qpos;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        //Menu
        public static Menu menu;

        private static Obj_AI_Hero Player;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            //Thanks to Esk0r
            Player = ObjectManager.Player;

            //check to see if correct champ
            if (Player.BaseSkinName != ChampionName) return;

            //intalize spell
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 575);
            R = new Spell(SpellSlot.R, 700);

            R.SetSkillshot(0.25f, 175, 700, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            //Create the menu
            menu = new Menu(ChampionName, ChampionName, true);

            //Orbwalker submenu
            menu.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            //Target selector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            menu.AddSubMenu(targetSelectorMenu);

            //Orbwalk
            Orbwalker = new Orbwalking.Orbwalker(menu.SubMenu("Orbwalking"));

            //Combo menu:
            menu.AddSubMenu(new Menu("Combo", "Combo"));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(menu.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind(menu.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Harass").AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));

            //Farming menu:
            menu.AddSubMenu(new Menu("Farm", "Farm"));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind(menu.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Farm").AddItem(new MenuItem("LastHitQQ", "Last hit with Q").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseGap", "Use W Against Gap Closer").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("useR_Hit", "Use R if hit").SetValue(new Slider(2, 5, 0)));
            menu.SubMenu("Misc").AddItem(new MenuItem("StackE", "StackE (toggle)!").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));
            menu.SubMenu("Misc").AddItem(new MenuItem("MoveToMouse", "MoveToMouse only").SetValue(new KeyBind("n".ToCharArray()[0], KeyBindType.Toggle)));

            //Damage after combo:
            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            //Drawings menu:
            menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(dmgAfterComboItem);
            menu.AddToMainMenu();

            //Events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.PrintChat(ChampionName + " Loaded! --- By xSalice");
        }

        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (W.IsReady() && gapcloser.Sender.IsValidTarget(W.Range) && menu.Item("UseGap").GetValue<bool>())
                W.Cast(gapcloser.Sender, true);
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);

            return (float)damage;
        }

        private static void Combo()
        {
            Orbwalker.SetAttacks(!(Q.IsReady() || E.IsReady() || menu.Item("MoveToMouse").GetValue<KeyBind>().Active));
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>());
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR)
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            if (useQ && qTarget != null && Q.IsReady() && Player.Distance(qTarget) <= Q.Range)
            {
                Q.Cast(qTarget, true);
                return;
            }

            if (useE && eTarget != null && E.IsReady() && Player.Distance(eTarget) <= E.Range)
            {
                E.Cast();
            }

            if (useR && rTarget != null && R.IsReady() && R.GetPrediction(rTarget).Hitchance >= HitChance.High && GetComboDamage(rTarget) > qTarget.Health + 150 && Player.Distance(rTarget) <= R.Range)
            {
                R.Cast(rTarget, true);
                return;
            }

        }

        private static void Harass()
        {
            Orbwalker.SetAttacks(!(menu.Item("MoveToMouse").GetValue<KeyBind>().Active));
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(),
                menu.Item("UseEHarass").GetValue<bool>(), false);
        }

        public static void RMec()
        {
            var minHit = menu.Item("useR_Hit").GetValue<Slider>().Value;
            if (minHit == 0)
                return;

            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            //check if target is in range
            if (Player.Distance(rTarget) <= R.Range && R.GetPrediction(rTarget).Hitchance >= HitChance.High)
            {
                R.CastIfWillHit(rTarget, minHit, true);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            int wTimeLeft = Environment.TickCount - Charges.lastW;
            if ((wTimeLeft <= 2000) && !W.IsReady())
            {
                Orbwalker.SetAttacks(false);
                return;
            }

            RMec();

            Orbwalker.SetAttacks(true);

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("HarassActive").GetValue<KeyBind>().Active || menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("LastHitQQ").GetValue<KeyBind>().Active)
                    lastHit();

                if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    Farm();

            }

            //Estacking credits to TRUS :D
            if (menu.Item("StackE").GetValue<KeyBind>().Active)
            {
                if (ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.E) == SpellState.Ready)
                {
                    int eTimeLeft = Environment.TickCount - Charges.lastE;
                    if ((eTimeLeft >= 9900) && E.IsReady())
                    {
                        E.Cast();
                    }
                }
            }

        }

        public static void lastHit()
        {
            if (!Orbwalking.CanMove(40)) return;

            var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

            if (Q.IsReady())
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget() && HealthPrediction.GetHealthPrediction(minion, (int)(Player.Distance(minion) * 1000 / 1400)) < DamageLib.getDmg(minion, DamageLib.SpellType.Q) - 10)
                    {
                        Q.CastOnUnit(minion, true);
                        return;
                    }
                }
            }
        }

        private static void Farm()
        {
            if (!Orbwalking.CanMove(40)) return;

            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            var rangedMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width, MinionTypes.All);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (useQ && allMinionsQ.Count > 0 && Q.IsReady())
            {
                Q.Cast(allMinionsQ[0], true);
            }

            if (useE && E.IsReady())
            {
                var ePos = E.GetCircularFarmLocation(rangedMinionsE);
                if (ePos.MinionsHit >= 3)
                    E.Cast(ePos.Position);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = menu.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }
        }
        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs attack)
        {
            if (unit.IsMe && attack.SData.Name == "VladimirTidesofBlood")
            {
                Charges.lastE = Environment.TickCount - 250;
            }

            if (unit.IsMe && attack.SData.Name == "VladimirSanguinePool")
            {
                Charges.lastW = Environment.TickCount - 250;
            }
        }

        public static class Charges
        {
            public static int lastE;
            public static int lastW;
        }

    }
}
