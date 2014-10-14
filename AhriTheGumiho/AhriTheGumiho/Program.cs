using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace AhriTheGumiho
{
    class Program
    {
        public const string ChampionName = "Ahri";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static bool rOn;
        public static int rTimer;
        public static int rTimeLeft;

        //mana 
        public static int[] qMana = { 55, 55, 60, 65, 70, 75 };
        public static int[] wMana = { 50, 50, 50, 50, 50, 50 };
        public static int[] eMana = { 85, 85, 85, 85, 85, 85 };
        public static int[] rMana = { 100, 100, 100, 100, 100, 100};
        //items
        public static Items.Item DFG;

        //Menu
        public static Menu menu;

        private static Obj_AI_Hero Player;
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
            Q = new Spell(SpellSlot.Q, 900);
            W = new Spell(SpellSlot.W, 800);
            E = new Spell(SpellSlot.E, 825);
            R = new Spell(SpellSlot.R, 850);

            Q.SetSkillshot(0.25f, 65, 1600, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 40, 1300, true, SkillshotType.SkillshotLine);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            DFG = Utility.Map.GetMap()._MapType == Utility.Map.MapType.TwistedTreeline ? new Items.Item(3188, 750) : new Items.Item(3128, 750);

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

            //key
            menu.AddSubMenu(new Menu("Key", "Key"));
            menu.SubMenu("Key").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(menu.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Key").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind(menu.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Key").AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));
            menu.SubMenu("Key").AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind(menu.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Key").AddItem(new MenuItem("charmCombo", "Q if Charmed in Combo").SetValue(new KeyBind("I".ToCharArray()[0], KeyBindType.Toggle)));

            //Combo menu:
            menu.AddSubMenu(new Menu("Combo", "Combo"));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("qHit", "Q/E HitChance").SetValue(new Slider(3, 1, 4)));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("rSpeed", "Use All R fast Duel").SetValue(true));
            
            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("qHit2", "Q/E HitChance").SetValue(new Slider(3, 1, 4)));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("longQ", "Cast Long range Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("charmHarass", "Only Q if Charmed").SetValue(true));

            //Farming menu:
            menu.AddSubMenu(new Menu("Farm", "Farm"));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(false));

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseInt", "Use E to Interrupt").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseGap", "Use E for GapCloser").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("packet", "Use Packets").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("mana", "Mana check before use R").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("dfgCharm", "Require Charmed to DFG").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("EQ", "Use Q onTop of E").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("smartKS", "Smart KS").SetValue(true));

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
                .AddItem(new MenuItem("cursor", "Draw R Dash Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(dmgAfterComboItem);
            menu.AddToMainMenu();

            //Events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q, 1);
            }

            if (DFG.IsReady())
                damage += Player.GetItemDamage(enemy, Damage.DamageItems.Dfg) / 1.2;

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R) * 2;//* Player.Spellbook.GetSpell(SpellSlot.R).Ammo;

            if (DFG.IsReady() && E.IsReady())
                damage = damage * 1.44;
            else if (DFG.IsReady() && enemy.HasBuffOfType(BuffType.Charm))
                damage = damage * 1.44;
            else if (E.IsReady())
                damage = damage * 1.2;
            else if (DFG.IsReady())
                damage = damage * 1.2;
            else if (enemy.HasBuffOfType(BuffType.Charm))
                damage = damage * 1.2;
            
            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            return (float)damage;
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
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
            var rETarget = SimpleTs.GetTarget(450 + E.Range, SimpleTs.DamageType.Magical);

            var hitC = HitChance.High;
            var qHit = menu.Item("qHit").GetValue<Slider>().Value;
            var harassQHit = menu.Item("qHit2").GetValue<Slider>().Value;

            // HitChance.Low = 3, Medium , High .... etc..
            if (Source == "Combo")
            {
                switch (qHit)
                {
                    case 1:
                        hitC = HitChance.Low;
                        break;
                    case 2:
                        hitC = HitChance.Medium;
                        break;
                    case 3:
                        hitC = HitChance.High;
                        break;
                    case 4:
                        hitC = HitChance.VeryHigh;
                        break;
                }
            }
            else if (Source == "Harass")
            {
                switch (harassQHit)
                {
                    case 1:
                        hitC = HitChance.Low;
                        break;
                    case 2:
                        hitC = HitChance.Medium;
                        break;
                    case 3:
                        hitC = HitChance.High;
                        break;
                    case 4:
                        hitC = HitChance.VeryHigh;
                        break;
                }
            }

            if (useE && eTarget != null && E.IsReady() && Player.Distance(eTarget) < E.Range && E.GetPrediction(eTarget).Hitchance >= hitC)
            {
                E.Cast(eTarget, packets());
                if (menu.Item("EQ").GetValue<bool>() && Q.IsReady())
                {
                    Q.Cast(eTarget, packets());
                    return;
                }
            }

            if (eTarget != null && GetComboDamage(eTarget) > eTarget.Health && DFG.IsReady() && (eTarget.HasBuffOfType(BuffType.Charm) || !menu.Item("dfgCharm").GetValue<bool>()))
            {
                DFG.Cast(eTarget);
            }

            if (useW && eTarget != null && W.IsReady() && Player.Distance(eTarget) <= W.Range && shouldW(eTarget, Source))
            {
                W.Cast(Player.ServerPosition, packets());
            }
            if (Source == "Harass" && menu.Item("longQ").GetValue<bool>())
            {
                if (useQ && Q.IsReady() && Player.Distance(eTarget) <= Q.Range && eTarget != null && shouldQ(eTarget, Source) && Player.Distance(eTarget) > 600)
                {
                    if (Q.GetPrediction(eTarget).Hitchance >= hitC)
                    {
                        Q.Cast(eTarget, packets(), true);
                        return;
                    }
                }
            }else if (useQ && Q.IsReady() && Player.Distance(eTarget) <= Q.Range && eTarget != null && shouldQ(eTarget, Source))
            {
                if (Q.GetPrediction(eTarget).Hitchance >= hitC)
                {
                    Q.Cast(eTarget, packets(), true);
                    return;
                }
            }

            if (useR && eTarget != null && R.IsReady() && Player.Distance(eTarget) < R.Range)
            {
                if (E.IsReady())
                {
                    if (checkREQ(rETarget))
                        E.Cast(rETarget, packets());
                    return;
                }
                else if (shouldR(eTarget))
                {
                    R.Cast(Game.CursorPos, packets());
                    rTimer = Environment.TickCount - 250;
                    return;
                }
                else if (rTimeLeft > 9500 && rOn)
                {
                    R.Cast(Game.CursorPos, packets());
                    rTimer = Environment.TickCount - 250;
                    return;
                }
            }

        }

        public static bool shouldQ(Obj_AI_Hero target, string Source)
        {
            if (Source == "Combo")
            {
                if ((Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.Q, 1)) > target.Health)
                    return true;

                if (rOn)
                    return true;

                if (!menu.Item("charmCombo").GetValue<KeyBind>().Active)
                    return true;

                if (target.HasBuffOfType(BuffType.Charm))
                    return true;

            }

            if (Source == "Harass")
            {
                if ((Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.Q, 1)) > target.Health)
                    return true;

                if (rOn)
                    return true;

                if (!menu.Item("charmHarass").GetValue<bool>())
                    return true;

                if (target.HasBuffOfType(BuffType.Charm))
                    return true;
            }

            return false;
        }

        public static bool shouldW(Obj_AI_Hero target, string Source)
        {
            if (Source == "Combo")
            {
                if (Player.GetSpellDamage(target, SpellSlot.W) > target.Health)
                    return true;

                if (rOn)
                    return true;

                if (!menu.Item("charmCombo").GetValue<KeyBind>().Active)
                    return true;

                if (target.HasBuffOfType(BuffType.Charm))
                    return true;

            }
            if (Source == "Harass")
            {
                if (Player.GetSpellDamage(target, SpellSlot.W) > target.Health)
                    return true;

                if (rOn)
                    return true;

                if (!menu.Item("charmHarass").GetValue<bool>())
                    return true;

                if (target.HasBuffOfType(BuffType.Charm))
                    return true;
            }

            return false;
        }

        public static bool shouldR(Obj_AI_Hero target)
        {
            if (!manaCheck())
                return false;

            if (target.Distance(Game.CursorPos) > 800 && rOn)
                return true;

            var dashVector = Player.Position + Vector3.Normalize(Game.CursorPos - Player.Position) * 425;
            if (Player.Distance(Game.CursorPos) < 75 && target.Distance(dashVector) > 425)
                return false;

            if (GetComboDamage(target) > target.Health && !rOn)
            {
                if (target.HasBuffOfType(BuffType.Charm))
                    return true;
            }

            if (countAlliesNearPosition(Game.CursorPos, 1000) > 2 && rTimeLeft > 3500)
                return true;

            if (menu.Item("rSpeed").GetValue<bool>() && countEnemiesNearPosition(Game.CursorPos, 1500) < 2 )
                return true;

            if (Player.GetSpellDamage(target, SpellSlot.R) * 2 > target.Health)
                return true;

            if (rOn && rTimeLeft > 9500)
                return true;

            return false;
        }

        public static bool checkREQ(Obj_AI_Hero target)
        {
            
            if (Player.Distance(Game.CursorPos) < 75)
                return false;

            if (GetComboDamage(target) > target.Health && !rOn && countEnemiesNearPosition(Game.CursorPos, 1500) < 3)
            {
                if (target.Distance(Game.CursorPos) <= E.Range && E.IsReady())
                {
                    var dashVector = Player.Position + Vector3.Normalize(Game.CursorPos - Player.Position) * 425;
                    var addedDelay = Player.Distance(dashVector) / 2200;

                    //Game.PrintChat("added delay: " + addedDelay);

                    var pred = GetP(Game.CursorPos, E, target, addedDelay, false);
                    if (pred.Hitchance >= HitChance.High)
                    {
                        R.Cast(Game.CursorPos, packets());
                        rTimer = Environment.TickCount - 250;
                        return true;
                    }
                }
            }

            return false;
        }

        public static int countEnemiesNearPosition(Vector3 pos, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>().Count(
                    hero => hero.IsEnemy && !hero.IsDead && hero.IsValid && hero.Distance(pos) <= range);
        }

        public static int countAlliesNearPosition(Vector3 pos, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>().Count(
                    hero => hero.IsAlly && !hero.IsDead && hero.IsValid && hero.Distance(pos) <= range);
        }

        public static void checkKS()
        {
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (target != null && !target.IsDead && target.IsEnemy && Player.Distance(target.ServerPosition) <= 1200)
                {
                    if (Player.Distance(target.ServerPosition) <= Q.Range && (Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.Q, 1)) > target.Health)
                    {
                        Q.Cast(target, packets());
                        return;
                    }

                    if (Player.Distance(target.ServerPosition) <= E.Range && (Player.GetSpellDamage(target, SpellSlot.E)) > target.Health)
                    {
                        E.Cast(target, packets());
                        return;
                    }

                    if (Player.Distance(target.ServerPosition) <= W.Range && (Player.GetSpellDamage(target, SpellSlot.W)) > target.Health)
                    {
                        W.Cast(Player.ServerPosition, packets());
                        return;
                    }
                }
            }
        }

        public static void mecQ()
        {
            
        }

        /// <summary>
        ///     gets minions and champs in a spells path.
        /// </summary>
        /// <param name="player"> the player </param>
        /// <param name="target"> the target </param>
        /// <param name="spell"> the spell to do the calculations for </param>
        /// <returns>
        ///     if a target is killable with given spell, taking into account damage reduction from minions and champs it
        ///     passes through also takes into account health regeneration rate, returns true / false.
        /// </returns>
        /// Credits Princer007
        public static int getUnitsInPath(Obj_AI_Hero player, Obj_AI_Hero target, Spell spell)
        {
            float distance = player.Distance(target);
            List<Obj_AI_Base> minionList = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spell.Range,
                MinionTypes.All, MinionTeam.NotAlly);
            int numberOfMinions = (from Obj_AI_Minion minion in minionList
                                   let skillshotPosition =
                                       V2E(player.Position,
                                           V2E(player.Position, target.Position,
                                               Vector3.Distance(player.Position, target.Position) - spell.Width + 1).To3D(),
                                           Vector3.Distance(player.Position, minion.Position))
                                   where skillshotPosition.Distance(minion) < spell.Width
                                   select minion).Count();
            int numberOfChamps = (from minion in ObjectManager.Get<Obj_AI_Hero>()
                                  let skillshotPosition =
                                      V2E(player.Position,
                                          V2E(player.Position, target.Position,
                                              Vector3.Distance(player.Position, target.Position) - spell.Width + 1).To3D(),
                                          Vector3.Distance(player.Position, minion.Position))
                                  where skillshotPosition.Distance(minion) < spell.Width && minion.IsEnemy
                                  select minion).Count();
            int total = numberOfChamps + numberOfMinions - 1;
            // total number of champions and minions the projectile will pass through.
            if (total == -1) return 0;

            return total;
        }

        public static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }

        public static bool manaCheck()
        {
            var totalMana = qMana[Q.Level] + wMana[W.Level] + eMana[E.Level] + rMana[R.Level];
            var checkMana = menu.Item("mana").GetValue<bool>();

            if (Player.Mana >= totalMana || checkMana)
                return true;
            
            return false;
        }

        public static bool isRActive()
        {
            return Player.HasBuff("AhriTumble", true);
        }

        public static PredictionOutput GetP(Vector3 pos, Spell spell, Obj_AI_Base target, float delay, bool aoe)
        {

            return Prediction.GetPrediction(new PredictionInput
            {
                Unit = target,
                Delay = spell.Delay + delay,
                Radius = spell.Width,
                Speed = spell.Speed,
                From = pos,
                Range = spell.Range,
                Collision = spell.Collision,
                Type = spell.Type,
                RangeCheckFrom = Player.ServerPosition,
                Aoe = aoe,
            });
        }

        private static void Farm()
        {
            if (!Orbwalking.CanMove(40)) return;

            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            var allMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useW = menu.Item("UseWFarm").GetValue<bool>();

            if (useQ && Q.IsReady())
            {
                var qPos = Q.GetLineFarmLocation(allMinionsQ);
                if (qPos.MinionsHit >= 3)
                {
                    Q.Cast(qPos.Position, packets());
                }
            }

            if (useW && allMinionsW.Count > 0 && W.IsReady())
                W.Cast(Player.ServerPosition, packets());
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            Orbwalker.SetAttacks(true);

            rOn = isRActive();

            if(rOn)
                rTimeLeft = Environment.TickCount - rTimer;

            //ks check
            if (menu.Item("smartKS").GetValue<bool>())
                checkKS();

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

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = menu.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            if (menu.Item("cursor").GetValue<Circle>().Active)
                Utility.DrawCircle(Player.Position, 475, Color.Aquamarine);
        }

        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!menu.Item("UseGap").GetValue<bool>()) return;

            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
                E.Cast();
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!menu.Item("UseInt").GetValue<bool>()) return;

            if (Player.Distance(unit) < E.Range && unit != null)
            {
                if (E.GetPrediction(unit).Hitchance >= HitChance.High)
                    E.Cast(unit, packets());
            }


        }


    }
}
