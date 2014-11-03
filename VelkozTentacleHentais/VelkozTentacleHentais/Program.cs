using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;
using SharpDX;
using Color = System.Drawing.Color;

namespace VelkozTentacleHentais
{
    internal class Program
    {
        public const string ChampionName = "Velkoz";

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell QSplit;
        public static Spell QDummy;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static Obj_AI_Hero qTarget = null;

        public static Obj_SpellMissile qMissle = null;

        public static Obj_AI_Hero SelectedTarget = null;

        //summoner 
        public static SpellSlot IgniteSlot;

        //Menu
        public static Menu menu;

        private static Obj_AI_Hero Player;

        //mana manager
        public static int[] qMana = {40, 40, 45, 50, 55, 60};
        public static int[] wMana = {50, 50, 55, 60, 65, 70};
        public static int[] eMana = {50, 50, 55, 60, 65, 70};
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
            Q = new Spell(SpellSlot.Q, 1050);
            QSplit = new Spell(SpellSlot.Q, 900);
            QDummy = new Spell(SpellSlot.Q, (float) Math.Sqrt(Math.Pow(Q.Range, 2) + Math.Pow(QSplit.Range, 2)));
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 850);
            R = new Spell(SpellSlot.R, 1500);

            Q.SetSkillshot(0.25f, 50f, 1300f, true, SkillshotType.SkillshotLine);
            QDummy.SetSkillshot(0.25f, 55f, float.MaxValue, false, SkillshotType.SkillshotLine);
            QSplit.SetSkillshot(0.25f, 55f, 2100, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 85f, 1700f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.5f, 80f, 1500f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.3f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);

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
                        new KeyBind(menu.Item("Harass_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0],
                        KeyBindType.Toggle)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("LaneClearActive", "Farm!").SetValue(
                        new KeyBind(menu.Item("LaneClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Spell Menu
            menu.AddSubMenu(new Menu("Spell", "Spell"));
            //Q Menu
            menu.SubMenu("Spell").AddSubMenu(new Menu("QSpell", "QSpell"));
            menu.SubMenu("Spell").SubMenu("QSpell").AddItem(new MenuItem("qSplit", "Auto Split Q").SetValue(true));
            menu.SubMenu("Spell").SubMenu("QSpell").AddItem(new MenuItem("qAngle", "Shoot Q At Angle").SetValue(true));
            //R
            menu.SubMenu("Spell").AddSubMenu(new Menu("RSpell", "RSpell"));
            menu.SubMenu("Spell").SubMenu("RSpell").AddItem(new MenuItem("rAimer", "R Aim").SetValue(
                new StringList(new[] {"Auto", "To Mouse"}, 0)));
            menu.SubMenu("Spell").SubMenu("RSpell").AddSubMenu(new Menu("Dont use R on", "DontUlt"));


            foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                menu.SubMenu("Spell").SubMenu("RSpell")
                    .SubMenu("DontUlt")
                    .AddItem(new MenuItem("DontUlt" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));


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

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseInt", "Use E to Interrupt").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseGap", "Use E for GapCloser").SetValue(true));
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
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.OnGameSendPacket += Game_OnGameSendPacket;
            GameObject.OnCreate += OnCreate;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            double damage = 0d;

            int collisionCount = Q.GetPrediction(enemy).CollisionObjects.Count;
            if (Q.IsReady() && collisionCount < 1)
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += W.Instance.Ammo*Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);

            if (R.IsReady())
                damage += getUltDmg((Obj_AI_Hero) enemy);

            damage += getPassiveDmg();

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
            Obj_AI_Hero qDummyTarget = SimpleTs.GetTarget(QDummy.Range, SimpleTs.DamageType.Magical);

            bool hasmana = manaCheck();
            float dmg = GetComboDamage(target);

            int IgniteMode = menu.Item("igniteMode").GetValue<StringList>().SelectedIndex;

            useR = (menu.Item("DontUlt" + target.BaseSkinName) != null &&
                    menu.Item("DontUlt" + target.BaseSkinName).GetValue<bool>() == false) && useR;

            if (useW && target != null && W.IsReady() && Player.Distance(target) <= W.Range &&
                W.GetPrediction(target).Hitchance >= HitChance.High)
            {
                W.Cast(target);
                return;
            }

            if (useE && target != null && E.IsReady() && Player.Distance(target) < E.Range &&
                E.GetPrediction(target).Hitchance >= HitChance.High)
            {
                E.Cast(target, packets());
                return;
            }

            //Ignite
            if (target != null && menu.Item("ignite").GetValue<bool>() && IgniteSlot != SpellSlot.Unknown &&
                Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && Source == "Combo" && hasmana)
            {
                if (IgniteMode == 0 && dmg > target.Health)
                {
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
                }
            }

            if (useQ && Q.IsReady() && target != null)
            {
                castQ(target, qDummyTarget);
            }

            if (useR && target != null && R.IsReady() && Player.Distance(target) < R.Range)
            {
                if (getUltDmg(target) >= target.Health)
                    R.Cast(target.ServerPosition);
            }
        }

        public static float getPassiveDmg()
        {
            double stack = 0;
            double dmg = 25 + (10 * Player.Level);

            if (Q.IsReady())
                stack++;

            if (W.IsReady())
                stack += 2;

            if (E.IsReady())
                stack ++;

            stack = stack/3;

            stack = Math.Floor(stack);

            dmg = dmg*stack;

            //Game.PrintChat("Stacks: " + stack);

            return (float) dmg;
        }
        public static float getUltDmg(Obj_AI_Hero target)
        {
            double dmg = 0;

            float dist = (Player.Distance(target) - 700)/800;
            float div = 8 - dist;

            //Game.PrintChat("ult dmg" + div);

            if (Player.Distance(target) < 700)
                div = 8;

            if (Player.Distance(target) < 1550)
                if (R.IsReady())
                {
                    double ultDmg = Player.GetSpellDamage(target, SpellSlot.R)/8;

                    dmg += ultDmg*div;
                }

            if(div > 2)
                dmg += 25 + (10*Player.Level);

            
            return (float) dmg;
        }

        public static void castQ(Obj_AI_Hero target, Obj_AI_Hero targetExtend)
        {
            PredictionOutput pred = Q.GetPrediction(target);
            int collision = pred.CollisionObjects.Count;

            //cast Q with no collision
            if (Player.Distance(target) < 1050)
            {
                if (collision == 0 && pred.Hitchance >= HitChance.High)
                {
                    Q.Cast(pred.CastPosition, packets());
                    return;
                }
            }

            if (!menu.Item("qAngle").GetValue<bool>())
                return;

            if (qTarget != null)
                targetExtend = qTarget;

            if (Q.Instance.Name == "VelkozQ" && targetExtend != null)
            {
                QDummy.Delay = Q.Delay + Q.Range/Q.Speed*1000 + QSplit.Range/QSplit.Speed*1000;
                pred = QDummy.GetPrediction(targetExtend);

                if (pred.Hitchance >= HitChance.High)
                {
                    //math by esk0r <3
                    for (int i = -1; i < 1; i = i + 2)
                    {
                        const float alpha = 28*(float) Math.PI/180;
                        Vector2 cp = Player.ServerPosition.To2D() +
                                     (pred.CastPosition.To2D() - Player.ServerPosition.To2D()).Rotated(i*alpha);

                        //Utility.DrawCircle(cp.To3D(), 100, Color.Blue, 1, 1);

                        if (Q.GetCollision(Player.ServerPosition.To2D(), new List<Vector2> {cp}).Count == 0 &&
                            QSplit.GetCollision(cp, new List<Vector2> {pred.CastPosition.To2D()}).Count == 0)
                        {
                            Q.Cast(cp, packets());
                            return;
                        }
                    }
                }
            }
        }

        public static void splitMissle()
        {
            //Game.PrintChat("bleh");

            Obj_AI_Hero target = getTarget();

            QSplit.From = qMissle.Position;
            PredictionOutput pred = QSplit.GetPrediction(target);

            Vector2 perpendicular = (qMissle.EndPosition - qMissle.StartPosition).To2D().Normalized().Perpendicular();

            Vector2 lineSegment1End = qMissle.Position.To2D() + perpendicular*QSplit.Range;
            Vector2 lineSegment2End = qMissle.Position.To2D() - perpendicular*QSplit.Range;

            float d1 = pred.UnitPosition.To2D().Distance(qMissle.Position.To2D(), lineSegment1End, true);
            float d2 = pred.UnitPosition.To2D().Distance(qMissle.Position.To2D(), lineSegment2End, true);

            //cast split
            if (pred.CollisionObjects.Count == 0 && d1 < QSplit.Width + target.BoundingRadius ||
                d2 < QSplit.Width + target.BoundingRadius)
            {
                Q.Cast();
                qMissle = null;
                //Game.PrintChat("splitted");
            }
        }

        public static Obj_AI_Hero getTarget()
        {
            int tsMode = menu.Item("tsModes").GetValue<StringList>().SelectedIndex;
            var focusSelected = menu.Item("selected").GetValue<bool>();

            if (focusSelected && SelectedTarget != null)
            {
                if (Player.Distance(SelectedTarget) < 1600 && !SelectedTarget.IsDead && SelectedTarget.IsVisible &&
                    SelectedTarget.IsEnemy)
                {
                    //Game.PrintChat("focusing selected target");
                    LXOrbwalker.ForcedTarget = SelectedTarget;
                    return SelectedTarget;
                }
                SelectedTarget = null;
            }


            Obj_AI_Hero getTar = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Magical);

            if (tsMode == 0)
                return getTar;

            foreach (
                Obj_AI_Hero target in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            x =>
                                Player.Distance(x) < 1500 && x.IsValidTarget(1500) && !x.IsDead && x.IsEnemy &&
                                x.IsVisible))
            {
                if (tsMode == 1)
                {
                    float tar1hp = target.Health/target.MaxHealth*100;
                    float tar2hp = getTar.Health/getTar.MaxHealth*100;
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

            List<Obj_AI_Hero> nearChamps = (from champ in ObjectManager.Get<Obj_AI_Hero>()
                where Player.Distance(champ.ServerPosition) <= Q.Range && champ.IsEnemy
                select champ).ToList();
            nearChamps.OrderBy(x => x.Health);

            foreach (Obj_AI_Hero target in nearChamps)
            {
                //Q
                if (Player.Distance(target.ServerPosition) <= Q.Range &&
                    (Player.GetSpellDamage(target, SpellSlot.Q)) > target.Health + 30)
                {
                    if (Q.IsReady())
                    {
                        castQ(target, target);
                        return;
                    }
                }

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
                        E.CastOnUnit(target, packets());
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

            if (Player.IsChannelingImportantSpell())
            {
                Obj_AI_Hero target = getTarget();
                int aimMode = menu.Item("rAimer").GetValue<StringList>().SelectedIndex;

                if (target != null && aimMode == 0)
                    Packet.C2S.ChargedCast.Encoded(new Packet.C2S.ChargedCast.Struct(SpellSlot.R,
                        target.ServerPosition.X, target.ServerPosition.Z, target.ServerPosition.Y)).Send();
                else
                    Packet.C2S.ChargedCast.Encoded(new Packet.C2S.ChargedCast.Struct(SpellSlot.R, Game.CursorPos.X,
                        Game.CursorPos.Z, Game.CursorPos.Y)).Send();

                return;
            }

            if (qMissle != null && qMissle.IsValid && menu.Item("qSplit").GetValue<bool>())
                splitMissle();

            smartKS();

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
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

        private static void Farm()
        {
            List<Obj_AI_Base> allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                Q.Range + Q.Width, MinionTypes.All, MinionTeam.NotAlly);
            List<Obj_AI_Base> allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range,
                MinionTypes.Ranged, MinionTeam.NotAlly);
            List<Obj_AI_Base> allMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range,
                MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useW = menu.Item("UseWFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (useW && W.IsReady() && allMinionsW.Count > 0)
            {
                MinionManager.FarmLocation wPos = Q.GetLineFarmLocation(allMinionsW);

                if (wPos.MinionsHit > 2)
                    W.Cast(wPos.Position, packets());
            }

            if (useE && allMinionsE.Count > 0 && E.IsReady())
            {
                MinionManager.FarmLocation ePos = E.GetCircularFarmLocation(allMinionsE);

                if (ePos.MinionsHit > 2)
                    E.Cast(ePos.Position, packets());
            }

            if (useQ && Q.IsReady() && allMinionsQ.Count > 0)
            {
                MinionManager.FarmLocation qPos = Q.GetLineFarmLocation(allMinionsQ);

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


        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!menu.Item("UseGap").GetValue<bool>()) return;

            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
                E.Cast(gapcloser.Sender, packets());
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!menu.Item("UseInt").GetValue<bool>()) return;

            if (Player.Distance(unit) < E.Range && unit != null && E.IsReady())
            {
                E.Cast(unit, packets());
            }
        }

        private static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            //Disable action on Ult
            if (args.PacketData[0] == Packet.C2S.ChargedCast.Header)
            {
                var decodedPacket = Packet.C2S.ChargedCast.Decoded(args.PacketData);

                if (decodedPacket.SourceNetworkId == Player.NetworkId)
                {
                    args.Process = !(menu.Item("ComboActive").GetValue<KeyBind>().Active && menu.Item("UseRCombo").GetValue<bool>() && menu.Item("smartKS").GetValue<bool>());
                }
            }

            /*if (args.PacketData[0] == 0xFE)
            {
                var p = new GamePacket(args.PacketData);
                if (p.ReadInteger(1) == ObjectManager.Player.NetworkId && p.Size() > 9)
                {
                    args.Process = false;
                }
            }*/

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

        private static void OnCreate(GameObject obj, EventArgs args)
        {
            // return if its not a missle
            if (!(obj is Obj_SpellMissile))
                return;

            var spell = (Obj_SpellMissile) obj;

            if (Player.Distance(obj.Position) < 1500)
            {
                //Q
                if (spell != null && spell.IsValid && spell.SData.Name == "VelkozQMissile")
                {
                    //Game.PrintChat("Woot");
                    qMissle = spell;
                }
            }
        }
    }
}