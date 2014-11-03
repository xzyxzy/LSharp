using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;
using SharpDX;
using Color = System.Drawing.Color;

namespace YorickMILFDigger
{
    internal class Program
    {
        public const string ChampionName = "Yorick";

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
        public static int[] qMana = {40, 40, 40, 40, 40, 40};
        public static int[] wMana = {55, 55, 60, 65, 70, 75};
        public static int[] eMana = {55, 55, 60, 65, 70, 75};
        public static int[] rMana = {100, 100, 100, 100};

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
            Q = new Spell(SpellSlot.Q, 125 + Player.BoundingRadius);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 550);
            R = new Spell(SpellSlot.R, 900);

            W.SetSkillshot(0.50f, 200, float.MaxValue, false, SkillshotType.SkillshotCircle);

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
                    new MenuItem("LaneClearActive", "Farm!").SetValue(
                        new KeyBind(menu.Item("LaneClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("LastHitE", "Last hit with E").SetValue(new KeyBind("A".ToCharArray()[0],
                        KeyBindType.Press)));

            //Spell Menu
            menu.AddSubMenu(new Menu("Spell", "Spell"));

            //Q Menu
            menu.SubMenu("Spell").AddSubMenu(new Menu("QSpell", "QSpell"));
            menu.SubMenu("Spell").SubMenu("QSpell").AddItem(new MenuItem("qReset", "Use Q as AA Reset").SetValue(true));
            //W Menu
            menu.SubMenu("Spell").AddSubMenu(new Menu("WSpell", "WSpell"));
            menu.SubMenu("Spell")
                .SubMenu("WSpell")
                .AddItem(new MenuItem("useW_Hit", "Use W if hit").SetValue(new Slider(2, 5, 0)));
            //E menu
            menu.SubMenu("Spell").AddSubMenu(new Menu("ESpell", "ESpell"));
            menu.SubMenu("Spell")
                .SubMenu("ESpell")
                .AddItem(new MenuItem("autoE", "Auto E enemy if HP <").SetValue(new Slider(50, 0, 100)));
            //R
            menu.SubMenu("Spell").AddSubMenu(new Menu("RSpell", "RSpell"));
            menu.SubMenu("Spell")
                .SubMenu("RSpell")
                .AddItem(new MenuItem("useRHP", "R in combo if my %HP <= ").SetValue(new Slider(50, 0, 100)));
            menu.SubMenu("Spell")
                .SubMenu("RSpell")
                .AddItem(new MenuItem("useRHPE", "R in combo if Enemy %HP <= ").SetValue(new Slider(50, 0, 100)));
            menu.SubMenu("Spell").SubMenu("RSpell").AddItem(new MenuItem("ultAllies", "Ult Allies").SetValue(true));

            menu.SubMenu("Spell").SubMenu("RSpell").AddSubMenu(new Menu("Dont use R on", "DontUlt"));
            foreach (
                Obj_AI_Hero myTeam in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(myTeam => myTeam.Team == Player.Team && myTeam.BaseSkinName != Player.BaseSkinName))
                menu.SubMenu("Spell").SubMenu("RSpell")
                    .SubMenu("DontUlt")
                    .AddItem(new MenuItem("DontUlt" + myTeam.BaseSkinName, myTeam.BaseSkinName).SetValue(false));

            //Combo menu:
            menu.AddSubMenu(new Menu("Combo", "Combo"));
            menu.SubMenu("Combo")
                .AddItem(
                    new MenuItem("tsModes", "TS Modes").SetValue(
                        new StringList(new[] {"Orbwalker/LessCast", "Low HP%", "NearMouse", "CurrentHP"}, 0)));
            menu.SubMenu("Combo").AddItem(new MenuItem("selected", "Focus Selected Target").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("ignite", "Use Ignite").SetValue(true));
            menu.SubMenu("Combo")
                .AddItem(new MenuItem("igniteMode", "Mode").SetValue(new StringList(new[] {"Combo", "KS"}, 0)));

            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));

            //Farming menu:
            menu.AddSubMenu(new Menu("Farm", "Farm"));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(false));

            //intiator list:
            menu.AddSubMenu((new Menu("Spellblocker", "Spellblocker")));

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                foreach (var spell in SpellCollision.Spells)
                {
                    if (spell.HeroName == hero.BaseSkinName)
                    {
                        menu.SubMenu("Spellblocker").AddItem(new MenuItem(spell.SpellName, spell.SpellName)).SetValue(false);
                    }
                }
            }

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
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
            Game.OnGameSendPacket += Game_OnGameSendPacket;
            LXOrbwalker.AfterAttack += OnAfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (!unit.IsEnemy || Player.Distance(unit.ServerPosition) > 1300) return;

            foreach (var spell in SpellCollision.Spells)
            {
                if (args.SData.Name == spell.SDataName)
                {
                    if (menu.Item(spell.SpellName).GetValue<bool>())
                    {
                        if (W.IsReady() && Player.Distance(args.End) < 300)
                        {
                            var vec = Player.ServerPosition + Vector3.Normalize(args.Start - Player.ServerPosition)*200;
                            Game.PrintChat("blocking");
                            W.Cast(vec);

                        }
                    }
                }
            }
        }

        private static void OnAfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe)
            {
                var useQCombo = menu.Item("UseQCombo").GetValue<bool>();
                var useQHarass = menu.Item("UseQHarass").GetValue<bool>();

                if (unit.IsMe)
                {
                    if (menu.Item("ComboActive").GetValue<KeyBind>().Active ||
                        menu.Item("HarassActive").GetValue<KeyBind>().Active ||
                        menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    {
                        if (Q.IsReady())
                        {
                            if (useQCombo || useQHarass)
                            {
                                Q.Cast();
                                LXOrbwalker.ResetAutoAttackTimer();
                                LXOrbwalker.Orbwalk(Game.CursorPos, target);
                            }
                        }
                    }
                }
            }
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            double damage = 0d;

            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);

            damage += Player.GetAutoAttackDamage(enemy)*2;

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
            bool hasmana = manaCheck();

            int IgniteMode = menu.Item("igniteMode").GetValue<StringList>().SelectedIndex;
            var qReset = menu.Item("qReset").GetValue<bool>();

            //Q
            if (useQ && Q.IsReady() && Player.Distance(target) <= 300 && !qReset)
            {
                Q.Cast();
            }

            //E
            if (useE && target != null && E.IsReady() && Player.Distance(target) < E.Range)
            {
                E.Cast(target);
                return;
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
            if (useW && target != null && W.IsReady() && Player.Distance(target) <= W.Range &&
                W.GetPrediction(target).Hitchance >= HitChance.High)
            {
                W.Cast(target, packets());
            }

            //R
            if (useR && target != null && R.IsReady())
            {
                if (!Player.HasBuff("yorickreviveallyguide", true))
                {
                    foreach (
                        Obj_AI_Hero ally in
                            ObjectManager.Get<Obj_AI_Hero>()
                                .Where(x => Player.Distance(x) < R.Range && x.IsAlly && !x.IsDead))
                    {
                        castR(target, ally);
                    }
                }
            }
        }

        public static void castR(Obj_AI_Hero enemy, Obj_AI_Hero target)
        {
            int HPtoUlt = menu.Item("useRHP").GetValue<Slider>().Value;
            int HPtoUltEnemy = menu.Item("useRHPE").GetValue<Slider>().Value;

            float playerHP = (Player.Health/Player.MaxHealth)*100;
            float enemyHP = (enemy.Health/enemy.MaxHealth)*100;

            bool useR = (menu.Item("DontUlt" + target.BaseSkinName) != null &&
                         menu.Item("DontUlt" + target.BaseSkinName).GetValue<bool>() == false);
            var useOnAlly = menu.Item("ultAllies").GetValue<bool>();

            if (enemyHP <= HPtoUltEnemy)
            {
                if (target != null && enemy != null && target.BaseSkinName != Player.BaseSkinName && useR && useOnAlly)
                {
                    R.Cast(target, packets());
                    return;
                }
                if (enemy != null && Player.Distance(enemy) < R.Range && R.IsReady())
                {
                    R.Cast(Player, packets());
                    return;
                }
            }

            if (playerHP <= HPtoUlt && Player.Distance(enemy) < 700)
            {
                if (enemy != null && R.IsReady())
                {
                    R.Cast(Player, packets());
                }
            }
        }

        public static void autoE()
        {
            int HPtoE = menu.Item("autoE").GetValue<Slider>().Value;
            float playerHP = (Player.Health/Player.MaxHealth)*100;
            Obj_AI_Hero Target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);

            if (Target == null)
                return;

            if (playerHP < HPtoE && Player.Distance(Target) <= E.Range && E.IsReady())
                E.Cast(Target);
        }

        private static void ExploitE()
        {
            if (E.IsReady())
                E.Cast(Player.ServerPosition, true);
        }

        public static void mecW()
        {
            int minHit = menu.Item("useW_Hit").GetValue<Slider>().Value;
            if (minHit == 0)
                return;

            foreach (
                Obj_AI_Hero target in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(x => Player.Distance(x) < W.Range && x.IsValidTarget() && x.IsEnemy && !x.IsDead))
            {
                if (target != null && target.Distance(Player.ServerPosition) <= W.Range && W.IsReady())
                {
                    PredictionOutput pred = W.GetPrediction(target);
                    if (pred.AoeTargetsHitCount > minHit)
                    {
                        W.Cast(pred.CastPosition);
                    }
                }
            }
        }

        public static void autoR()
        {
            if (!Player.HasBuff("yorickreviveallyguide", true))
                return;

            foreach (
                Obj_AI_Hero target in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(x => Player.Distance(x) < 1500 && x.IsEnemy && !x.IsDead).OrderBy(x => x.Health))
            {
                int lastR = Environment.TickCount - R.LastCastAttemptT;
                if (target != null && lastR <= 500)
                {
                    R.Cast(target);
                    R.LastCastAttemptT = Environment.TickCount - 250;
                }
            }
            
        }

        public static Obj_AI_Hero getTarget()
        {
            int tsMode = menu.Item("tsModes").GetValue<StringList>().SelectedIndex;
            var focusSelected = menu.Item("selected").GetValue<bool>();

            var range = 700f;

            Obj_AI_Hero getTar = SimpleTs.GetTarget(range, SimpleTs.DamageType.Magical);

            if (focusSelected && SelectedTarget != null)
            {
                if (Player.Distance(SelectedTarget) < 1000 && !SelectedTarget.IsDead && SelectedTarget.IsVisible &&
                    SelectedTarget.IsValidTarget() &&
                    SelectedTarget.IsEnemy)
                {
                    //Game.PrintChat("focusing selected target");
                    LXOrbwalker.ForcedTarget = SelectedTarget;
                    return SelectedTarget;
                }

                SelectedTarget = null;
                return getTar;
            }

            if (tsMode == 0)
                return getTar;

            foreach (
                Obj_AI_Hero target in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            x =>
                                Player.Distance(x) < range && x.IsValidTarget(range) && !x.IsDead && x.IsEnemy &&
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


            LXOrbwalker.ForcedTarget = getTar;
            return getTar;
        }

        public static void smartKS()
        {
            if (!menu.Item("smartKS").GetValue<bool>())
                return;

            List<Obj_AI_Hero> nearChamps = (from champ in ObjectManager.Get<Obj_AI_Hero>()
                where Player.Distance(champ.ServerPosition) <= 900 && champ.IsEnemy
                select champ).ToList();
            nearChamps.OrderBy(x => x.Health);

            foreach (Obj_AI_Hero target in nearChamps)
            {
                //EW
                if (Player.Distance(target.ServerPosition) <= E.Range &&
                    (Player.GetSpellDamage(target, SpellSlot.E) + Player.GetSpellDamage(target, SpellSlot.W)) >
                    target.Health + 30)
                {
                    if (W.IsReady() && E.IsReady())
                    {
                        E.Cast(target);
                        W.Cast(target, packets());
                        return;
                    }
                }

                //E
                if (Player.Distance(target.ServerPosition) <= E.Range &&
                    (Player.GetSpellDamage(target, SpellSlot.E)) > target.Health + 30)
                {
                    if (E.IsReady())
                    {
                        E.CastOnUnit(target);
                        return;
                    }
                }

                //W
                if (Player.Distance(target.ServerPosition) <= W.Range &&
                    (Player.GetSpellDamage(target, SpellSlot.W)) > target.Health + 50)
                {
                    if (W.IsReady())
                    {
                        W.Cast(target, packets());
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

            autoE();

            mecW();

            autoR();

            smartKS();

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("LastHitE").GetValue<KeyBind>().Active)
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

            List<Obj_AI_Base> allMinions = MinionManager.GetMinions(Player.ServerPosition, E.Range);

            if (E.IsReady())
            {
                foreach (Obj_AI_Base minion in allMinions)
                {
                    if (minion.IsValidTarget() &&
                        HealthPrediction.GetHealthPrediction(minion, (int) (Player.Distance(minion)*1000/1400)) <
                        Player.GetSpellDamage(minion, SpellSlot.E) - 10)
                    {
                        E.Cast(minion);
                        return;
                    }
                }
            }
        }

        private static void Farm()
        {
            if (!Orbwalking.CanMove(40)) return;

            List<Obj_AI_Base> allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range,
                MinionTypes.All, MinionTeam.NotAlly);
            List<Obj_AI_Base> rangedMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                W.Range + W.Width + 50, MinionTypes.All);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useW = menu.Item("UseWFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (useQ && Q.IsReady())
                Q.Cast();

            if (useE && allMinionsE.Count > 0 && E.IsReady())
                E.Cast(allMinionsE[0]);

            if (useW && W.IsReady())
            {
                MinionManager.FarmLocation wPos = W.GetCircularFarmLocation(rangedMinionsW);
                if (wPos.MinionsHit >= 2)
                {
                    W.Cast(wPos.Position, packets());
                }
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

        private static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            if (args.PacketData[0] != Packet.C2S.SetTarget.Header)
            {
                return;
            }

            Packet.C2S.SetTarget.Struct decoded = Packet.C2S.SetTarget.Decoded(args.PacketData);

            if (decoded.NetworkId != 0 && decoded.Unit.IsValid && !decoded.Unit.IsMe)
            {
                SelectedTarget = (Obj_AI_Hero) decoded.Unit;
                if (menu.Item("printTar").GetValue<bool>())
                    Game.PrintChat("Selected Target: " + decoded.Unit.BaseSkinName);
            }
        }
    }
}