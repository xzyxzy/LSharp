using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;

namespace MalzaharSpaceAids
{
    internal class Program
    {
        public const string ChampionName = "Malzahar";

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static Obj_AI_Hero SelectedTarget = null;

        //summoner 
        public static SpellSlot IgniteSlot;

        //Menu
        public static Menu menu;

        private static Obj_AI_Hero Player;

        //mana manager
        public static int[] qMana = { 60, 60, 65, 70, 75, 80 };
        public static int[] wMana = { 70, 70, 80, 90, 100, 110 };
        public static int[] eMana = { 80, 80, 90, 100, 110, 120 };
        public static int[] rMana = { 125, 125, 175, 225 };

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            //check to see if correct champ
            if (Player.BaseSkinName != ChampionName) return;

            //intalize spell
            Q = new Spell(SpellSlot.Q, 850);
            W = new Spell(SpellSlot.W, 800);
            E = new Spell(SpellSlot.E, 650);
            R = new Spell(SpellSlot.R, 700);

            Q.SetSkillshot(.5f, 30, 1600, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.50f, 50, float.MaxValue, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            //Create the menu
            menu = new Menu(ChampionName, ChampionName, true);

            //Orbwalker submenu
            var orbwalkerMenu = new Menu("My Orbwalker", "my_Orbwalker");
            LXOrbwalker.AddToMenu(orbwalkerMenu);
            menu.AddSubMenu(orbwalkerMenu);

            //Target selector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            menu.AddSubMenu(targetSelectorMenu);


            //Keys
            menu.AddSubMenu(new Menu("Keys", "Keys"));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(
                        new KeyBind(menu.Item("Combo_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("HarassActive", "Harass!").SetValue(
                        new KeyBind(menu.Item("LaneClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0],
                        KeyBindType.Toggle)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("lastHit", "Last hit with Q").SetValue(
                        new KeyBind(menu.Item("LastHit_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("LaneClearActive", "Farm!").SetValue(
                        new KeyBind(menu.Item("LaneClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Combo menu:
            menu.AddSubMenu(new Menu("Combo", "Combo"));
            menu.SubMenu("Combo")
                .AddItem(
                    new MenuItem("tsModes", "TS Modes").SetValue(
                        new StringList(new[] { "Orbwalker/LessCast", "Low HP%", "NearMouse", "CurrentHP" }, 0)));
            menu.SubMenu("Combo").AddItem(new MenuItem("selected", "Focus Selected Target").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("ignite", "Use Ignite").SetValue(true));
            menu.SubMenu("Combo")
                .AddItem(new MenuItem("igniteMode", "Mode").SetValue(new StringList(new[] { "Combo", "KS" }, 0)));

            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));

            //Farming menu:
            menu.AddSubMenu(new Menu("Farm", "Farm"));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q Farm").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E Farm").SetValue(false));

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseInt", "Use Q/R to Interrupt").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseGap", "Use R for GapCloser").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("packet", "Use Packets").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("smartKS", "Use Smart KS System").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("printTar", "Print Selected Target").SetValue(true));

            //Damage after combo:
            MenuItem dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
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
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.OnGameSendPacket += Game_OnGameSendPacket;
            GameObject.OnCreate += OnCreate;
            //GameObject.OnDelete += OnDelete;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            double damage = 0d;

            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W) * 3;

            if (E.IsReady() || enemy.HasBuff("AlZaharMaleficVisions"))
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);

            return (float) damage;
        }

        private static void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>(), "Combo");
        }

        private static void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(),
                menu.Item("UseEHarass").GetValue<bool>(), false, "Harass");
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, string Source)
        {
            Obj_AI_Hero target = getTarget();
            var hasmana = manaCheck();

            int IgniteMode = menu.Item("igniteMode").GetValue<StringList>().SelectedIndex;

            //Q
            if (useQ && Q.IsReady() && Player.Distance(target) <= Q.Range && target != null &&
                     Q.GetPrediction(target).Hitchance >= HitChance.High)
            {
                Q.Cast(target, packets(), true);
                return;
            }

            var dmg = GetComboDamage(target);

            //E
            if (useE && target != null && E.IsReady() && Player.Distance(target) < E.Range)
            {
                E.CastOnUnit(target, packets());
            }

            //Ignite
            if (target != null && menu.Item("ignite").GetValue<bool>() && IgniteSlot != SpellSlot.Unknown &&
                Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && Source == "Combo" && hasmana)
            {
                if (IgniteMode == 0 && GetComboDamage(target) > target.Health)
                {
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
                }
            }

            //W
            if (useW && target != null && W.IsReady() && Player.Distance(target) <= W.Range && W.GetPrediction(target).Hitchance >= HitChance.High && !E.IsReady())
            {
                W.Cast(target, packets());

                //R
                if (useR && R.IsReady() && dmg >= target.Health &&
                    Player.Distance(target) < R.Range)
                {
                    R.CastOnUnit(target, packets());
                }
            }

            /*//R
            if (useR && target != null && R.IsReady() && dmg >= target.Health && !W.IsReady() &&
                Player.Distance(target) < R.Range)
            {
                R.CastOnUnit(target, packets());
            }*/
        }

        public static Obj_AI_Hero getTarget()
        {
            int tsMode = menu.Item("tsModes").GetValue<StringList>().SelectedIndex;
            var focusSelected = menu.Item("selected").GetValue<bool>();

            if (focusSelected && SelectedTarget != null)
            {
                if (Player.Distance(SelectedTarget) < 1200 && !SelectedTarget.IsDead && SelectedTarget.IsVisible &&
                    SelectedTarget.IsEnemy)
                {
                    //Game.PrintChat("focusing selected target");
                    LXOrbwalker.ForcedTarget = SelectedTarget;
                    return SelectedTarget;
                }
                SelectedTarget = null;
            }


            Obj_AI_Hero getTar = SimpleTs.GetTarget(1000, SimpleTs.DamageType.Magical);

            if (tsMode == 0)
                return getTar;

            foreach (
                Obj_AI_Hero target in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            x =>
                                Player.Distance(x) < E.Range && x.IsValidTarget(E.Range) && !x.IsDead && x.IsEnemy &&
                                x.IsVisible))
            {
                if (tsMode == 1)
                {
                    float tar1hp = target.Health / target.MaxHealth * 100;
                    float tar2hp = getTar.Health / getTar.MaxHealth * 100;
                    if (tar1hp < tar2hp)
                        getTar = target;
                }

                if (tsMode == 2)
                {
                    if (target.Distance(Game.CursorPos) < getTar.Distance(Game.CursorPos))
                        getTar = target;
                }

                if (tsMode == 3)
                {
                    if (target.Health < getTar.Health)
                        getTar = target;
                }
            }

            if (getTar != null)
            {
                LXOrbwalker.ForcedTarget = getTar;
                //Game.PrintChat("Focus Mode on: " + getTar.BaseSkinName);
                return getTar;
            }

            return null;
        }

        public static void smartKS()
        {
            if (!menu.Item("smartKS").GetValue<bool>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => Player.Distance(x) < 1300 && x.IsValidTarget() && x.IsEnemy && !x.IsDead))
            {
                if (target != null)
                {
                    //ER
                    if (Player.Distance(target.ServerPosition) <= E.Range &&
                        (Player.GetSpellDamage(target, SpellSlot.R) + Player.GetSpellDamage(target, SpellSlot.E)) > target.Health + 50)
                    {
                        if (R.IsReady() && E.IsReady())
                        {
                            E.CastOnUnit(target, packets());
                            R.CastOnUnit(target, packets());
                            return;
                        }
                    }

                    //WR
                    if (Player.Distance(target.ServerPosition) <= R.Range &&
                        (Player.GetSpellDamage(target, SpellSlot.W) * 3 + Player.GetSpellDamage(target, SpellSlot.R)) > target.Health + 30)
                    {
                        if (W.IsReady() && R.IsReady())
                        {
                            W.Cast(target, packets());
                            return;
                        }
                    }

                    //W
                    if (Player.Distance(target.ServerPosition) <= W.Range &&
                        (Player.GetSpellDamage(target, SpellSlot.W)) > target.Health + 30)
                    {
                        if (W.IsReady())
                        {
                            W.Cast(target, packets());
                            return;
                        }
                    }

                    //Q
                    if (Player.Distance(target.ServerPosition) <= Q.Range &&
                        (Player.GetSpellDamage(target, SpellSlot.Q)) > target.Health + 30)
                    {
                        if (Q.IsReady())
                        {
                            Q.Cast(target, packets());
                            return;
                        }
                    }

                    //E
                    if (Player.Distance(target.ServerPosition) <= E.Range &&
                        (Player.GetSpellDamage(target, SpellSlot.E)) > target.Health + 30)
                    {
                        if (E.IsReady())
                        {
                            E.CastOnUnit(target, packets());
                            return;
                        }
                    }

                    //R
                    if (Player.Distance(target.ServerPosition) <= R.Range &&
                        (Player.GetSpellDamage(target, SpellSlot.R)) > target.Health + 50)
                    {
                        if (R.IsReady())
                        {
                            R.CastOnUnit(target, packets());
                            return;
                        }
                    }

                    //ignite
                    if (target != null && menu.Item("ignite").GetValue<bool>() && IgniteSlot != SpellSlot.Unknown &&
                        Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                        Player.Distance(target.ServerPosition) <= 600)
                    {
                        int IgniteMode = menu.Item("igniteMode").GetValue<StringList>().SelectedIndex;
                        if (Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > target.Health + 20)
                        {
                            Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
                        }
                    }
                }
            }
        }

        public static bool manaCheck()
        {
            int totalMana = qMana[Q.Level] + wMana[W.Level] + eMana[E.Level] + rMana[R.Level];

            if (Player.Mana >= totalMana)
                return true;

            return false;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            if (Player.IsChannelingImportantSpell())
                return;

            smartKS();

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("lastHit").GetValue<KeyBind>().Active)
                    lastHit();

                if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    Farm();

                if (menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();
            }
        }

        public static bool packets()
        {
            return menu.Item("packet").GetValue<bool>();
        }

        public static void lastHit()
        {
            if (!Orbwalking.CanMove(40)) return;

            List<Obj_AI_Base> allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

            if (Q.IsReady())
            {
                foreach (Obj_AI_Base minion in allMinions)
                {
                    if (minion.IsValidTarget() &&
                        HealthPrediction.GetHealthPrediction(minion, (int) (Player.Distance(minion)*1000/1400)) <
                        Player.GetSpellDamage(minion, SpellSlot.Q) - 10)
                    {
                        Q.Cast(minion, packets());
                        return;
                    }
                }
            }
        }

        private static void Farm()
        {
            if (!Orbwalking.CanMove(40)) return;

            List<Obj_AI_Base> rangedMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                Q.Range + Q.Width, MinionTypes.All, MinionTeam.NotAlly);
            List<Obj_AI_Base> allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range,
                MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (useE && allMinionsE.Count > 0 && E.IsReady())
            {
                E.Cast(allMinionsE[0], packets());
            }

            if (useQ && Q.IsReady())
            {
                MinionManager.FarmLocation qPos = Q.GetCircularFarmLocation(rangedMinionsQ);
                if (qPos.MinionsHit >= 3)
                    Q.Cast(qPos.Position, packets());
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (Spell spell in SpellList)
            {
                var menuItem = menu.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs attack)
        {
        }

        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!menu.Item("UseGap").GetValue<bool>()) return;

            if (R.IsReady() && gapcloser.Sender.IsValidTarget(R.Range))
                R.CastOnUnit(gapcloser.Sender, packets());
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!menu.Item("UseInt").GetValue<bool>()) return;

            if (Player.Distance(unit) < Q.Range && unit != null && Q.IsReady())
            {
                Q.Cast(unit, packets());
            }

            if (Player.Distance(unit) < R.Range && unit != null && R.IsReady())
            {
                R.CastOnUnit(unit, packets());
            }
        }
        private static void OnCreate(GameObject obj, EventArgs args)
        {
            //if(Player.Distance(obj.Position) < 300)
               // Game.PrintChat("OBJ: " + obj.Name);
            if (!menu.Item("UseRCombo").GetValue<bool>())
                return;

            if (Player.Distance(obj.Position) < 1500)
            {
                //Q
                if (obj != null && obj.IsValid && obj.Name == "AlzaharNullZoneFlash.troy")
                {
                    //Game.PrintChat("Woot");
                    var target = getTarget();
                    if(GetComboDamage(target) > target.Health && R.IsReady() && target.Distance(obj.Position) < 250)
                        R.CastOnUnit(target, packets());
                }


            }
        }
        private static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            if (args.PacketData[0] != Packet.C2S.SetTarget.Header)
            {
                return;
            }

            var decoded = Packet.C2S.SetTarget.Decoded(args.PacketData);

            if (decoded.NetworkId != 0 && decoded.Unit.IsValid && !decoded.Unit.IsMe && decoded.Unit.IsEnemy)
            {
                SelectedTarget = (Obj_AI_Hero)decoded.Unit;
                if (menu.Item("printTar").GetValue<bool>())
                    Game.PrintChat("Selected Target: " + decoded.Unit.BaseSkinName);
            }
        }
    }
}