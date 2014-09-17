using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace YorickMILFDigger
{
    class Program
    {
        public const string ChampionName = "Yorick";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static bool hasGhost = false;
        public static int ghostTimer = 0;

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
            Q = new Spell(SpellSlot.Q, 125);
            W = new Spell(SpellSlot.W, 650);
            E = new Spell(SpellSlot.E, 550);
            R = new Spell(SpellSlot.R, 900);

            W.SetSkillshot(0.25f, 200, float.MaxValue, false, SkillshotType.SkillshotCircle);

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
            menu.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind(menu.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Farm").AddItem(new MenuItem("LastHitE", "Last hit with E").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("useW_Hit", "Use W if hit").SetValue(new Slider(2, 5, 0)));
            menu.SubMenu("Misc").AddItem(new MenuItem("autoE", "Auto E enemy if HP <").SetValue(new Slider(50, 0, 100)));
            menu.SubMenu("Misc").AddItem(new MenuItem("useRHP", "R in combo if my %HP <= ").SetValue(new Slider(50, 0, 100)));
            menu.SubMenu("Misc").AddItem(new MenuItem("useRHPE", "R in combo if Enemy %HP <= ").SetValue(new Slider(50, 0, 100)));
            menu.SubMenu("Misc").AddSubMenu(new Menu("Dont use R on", "DontUlt"));

            foreach (var myTeam in ObjectManager.Get<Obj_AI_Hero>().Where(myTeam => myTeam.Team == Player.Team && myTeam.BaseSkinName != Player.BaseSkinName))
                menu.SubMenu("Misc")
                    .SubMenu("DontUlt")
                    .AddItem(new MenuItem("DontUlt" + myTeam.BaseSkinName, myTeam.BaseSkinName).SetValue(false));
            

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
            menu.AddToMainMenu();

            //Events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.PrintChat(ChampionName + " Loaded! --- By xSalice");
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.Q);

            if (W.IsReady())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.W);

            if (E.IsReady())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.E);

            if (R.IsReady())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.R);

            return (float)damage;
        }

        private static void Combo()
        {
            UseSpells(menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>());
        }

        private static void UseSpells( bool useW, bool useE, bool useR)
        {
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            if (useW && wTarget != null && W.IsReady() && Player.Distance(wTarget) < W.Range && W.GetPrediction(wTarget).Hitchance >= HitChance.High)
            {
                W.Cast(wTarget, true);
                return;
            }

            if (useE && eTarget != null && E.IsReady() && Player.Distance(eTarget) < E.Range)
            {
                E.CastOnUnit(eTarget);
                return;
            }

            if (useR && rTarget != null && R.IsReady() && !hasGhost)
            {
                foreach (Obj_AI_Hero myTeam in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if(Player.Distance(rTarget) <= 1000 && myTeam != Player && myTeam.IsAlly)
                    checkR(myTeam, rTarget);
                }
            }

        }

        public static void checkR(Obj_AI_Hero target, Obj_AI_Hero enemy)
        {
            var HPtoUlt = menu.Item("useRHP").GetValue<Slider>().Value;
            var HPtoUltEnemy = menu.Item("useRHPE").GetValue<Slider>().Value;
            var playerHP = (Player.Health/Player.MaxHealth) * 100;
            var enemyHP = (enemy.Health/enemy.MaxHealth) * 100;
            bool useR = (menu.Item("DontUlt" + target.BaseSkinName) != null && menu.Item("DontUlt" + target.BaseSkinName).GetValue<bool>() == false);

            if (enemyHP <= HPtoUltEnemy)
            {
                if (target != null && enemy != null && target.BaseSkinName != Player.BaseSkinName && Player.Distance(target) < R.Range && useR)
                {
                    R.Cast(target, true);
                    return;
                }
                else if (enemy != null && Player.Distance(enemy) < R.Range)
                {
                    R.Cast(Player, true);
                }

            }

            if (playerHP <= HPtoUlt)
            {
                if (enemy != null)
                {
                    R.Cast(Player, true);
                    return;
                }
            }

        }
        public static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            var useQCombo = menu.Item("UseQCombo").GetValue<bool>();
            var useQHarass = menu.Item("UseQHarass").GetValue<bool>();

            if (unit.IsMe)
            {
                if (menu.Item("ComboActive").GetValue<KeyBind>().Active || menu.Item("HarassActive").GetValue<KeyBind>().Active || menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                {
                    if(Q.IsReady() && target.IsValidTarget())
                        if (useQCombo || useQHarass)
                        {
                            Q.Cast();
                            Packet.C2S.Move.Encoded(new Packet.C2S.Move.Struct(target.ServerPosition.To2D().X, target.ServerPosition.To2D().Y, 7, target.NetworkId, Player.NetworkId)).Send();
                        }
                }
            }
        }

        private static void Harass()
        {
            UseSpells(menu.Item("UseWHarass").GetValue<bool>(), menu.Item("UseEHarass").GetValue<bool>(), false);
        }

        public static void autoE()
        {
            var HPtoE = menu.Item("autoE").GetValue<Slider>().Value;
            var playerHP = (Player.Health / Player.MaxHealth) * 100;
            var Target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);

            if (playerHP < HPtoE && Target != null && Target.IsValidTarget())
                E.Cast(Target);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            autoE();

            WMec();

            int RTimeLeft = Environment.TickCount - ghostTimer;
            if ((RTimeLeft >= 10000))
            {
                hasGhost = false;
            }

            Orbwalker.SetAttacks(true);

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("HarassActive").GetValue<KeyBind>().Active || menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("LastHitE").GetValue<KeyBind>().Active)
                    lastHit();

                if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    Farm();

            }

            //command pet
            var rTarget = SimpleTs.GetTarget(2500, SimpleTs.DamageType.Physical);
            if (hasGhost && rTarget != null)
            {
                Packet.C2S.Move.Encoded(new Packet.C2S.Move.Struct(rTarget.ServerPosition.To2D().X, rTarget.ServerPosition.To2D().Y, 5, rTarget.NetworkId, Player.NetworkId)).Send();
            }
        }

        public static void lastHit()
        {
            if (!Orbwalking.CanMove(40)) return;

            var allMinions = MinionManager.GetMinions(Player.ServerPosition, E.Range);

            if (E.IsReady())
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget() && HealthPrediction.GetHealthPrediction(minion, (int)(Player.Distance(minion) * 1000 / 1400)) < DamageLib.getDmg(minion, DamageLib.SpellType.Q) - 10)
                    {
                        E.Cast(minion, true);
                        return;
                    }
                }
            }
        }

        private static void Farm()
        {
            if (!Orbwalking.CanMove(40)) return;

            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);
            var rangedMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range + W.Width + 50, MinionTypes.All);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useW = menu.Item("UseWFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (useQ && Q.IsReady())
                Q.Cast();

            if (useE && allMinionsE.Count > 0 && E.IsReady())
                E.Cast(allMinionsE[0]);

            if (useW && W.IsReady())
            {
                var wPos = W.GetCircularFarmLocation(rangedMinionsW);
                if (wPos.MinionsHit >= 2)
                {
                    W.Cast(wPos.Position, true);
                }
            }
        }

        public static void WMec()
        {
            var minHit = menu.Item("useW_Hit").GetValue<Slider>().Value;
            if (minHit == 0)
                return;

            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);

            //check if target is in range
            if (!wTarget.IsValidTarget(W.Range) && !(W.GetPrediction(wTarget).Hitchance >= HitChance.High))
                return;

            W.CastIfWillHit(wTarget, minHit, true);
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
            if (attack.SData.Name == "YorickReviveAlly")
            {
                hasGhost = true;
                ghostTimer = Environment.TickCount - 250;
            }
        }

    }
}
