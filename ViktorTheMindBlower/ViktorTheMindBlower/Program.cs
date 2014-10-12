using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace ViktorTheMindBlower
{
    class Program
    {
        public const string ChampionName = "Viktor";

        //Orbwalker instance
        public static Orbwalkerz.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static bool chargeQ;
        public static Spell W;
        public static Spell E;
        public static Spell E2;
        public static Spell R;
        public static GameObject rObj = null;
        public static bool activeR = false;
        public static int lastR;

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
            Q = new Spell(SpellSlot.Q, 700);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 540);
            E2 = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 700);

            //Q.SetTargetted(0.25f, 2000);
            W.SetSkillshot(1.5f, 300, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.0f, 90, 1000, false, SkillshotType.SkillshotLine);
            E2.SetSkillshot(0.0f, 90, 1000, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.25f, 250, float.MaxValue, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(E2);
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
            Orbwalker = new Orbwalkerz.Orbwalker(menu.SubMenu("Orbwalking"));

            //Combo menu:
            menu.AddSubMenu(new Menu("Combo", "Combo"));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("wSlow", "Auto W Slow").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("wImmobile", "Auto W Immobile").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("wDashing", "Auto W Dashing").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("wMulti", "W only Multitarget").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("QAARange", "Q only if in AA Range").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("eHit", "E HitChance").SetValue(new Slider(3, 1, 4)));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(menu.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("eHit2", "E HitChance").SetValue(new Slider(4, 1, 4)));
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
            menu.SubMenu("Misc").AddItem(new MenuItem("UseInt", "Use R to Interrupt").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("autoAtk", "AA after Q in Range").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("useW_Hit", "Auto W if hit In Combo").SetValue(new Slider(2, 5, 0)));
            menu.SubMenu("Misc").AddItem(new MenuItem("rAlways", "Ult Always Combo").SetValue(new KeyBind("K".ToCharArray()[0], KeyBindType.Toggle)));
            menu.SubMenu("Misc").AddItem(new MenuItem("useR_Hit", "Auto R if hit In Combo").SetValue(new Slider(2, 5, 0)));
            menu.SubMenu("Misc").AddItem(new MenuItem("packet", "Use Packets to cast").SetValue(true));
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
                .AddItem(new MenuItem("E2Range", "Extended range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));

            menu.SubMenu("Drawings")
                .AddItem(dmgAfterComboItem);
            menu.AddToMainMenu();

            //Events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Orbwalking.AfterAttack += AfterAttack;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            SpellSlot castedSlot = ObjectManager.Player.GetSpellSlot(args.SData.Name, false);

            if (!unit.IsMe) return;

            Game.PrintChat("Spell: " + args.SData.Name);

        }

        private static void OnCreate(GameObject obj, EventArgs args)
        {
            //if (Player.Distance(obj.Position) < 400 && obj.Name != "missile")
                //Game.PrintChat("Obj: " + obj.Name);

            if (Player.Distance(obj.Position) < 3000)
            {
                if (obj != null && obj.IsValid && obj.Name.Contains("Viktor_Base_R"))
                {
                    //Game.PrintChat("woot");
                    activeR = true;
                    rObj = obj;
                }
            }
            
        }

        private static void OnDelete(GameObject obj, EventArgs args)
        {
            //if (Player.Distance(obj.Position) < 400 && obj.Name != "missile")
                //Game.PrintChat("Obj2: " + obj.Name);
            if (Player.Distance(obj.Position) < 3000)
            {
                if (obj != null && obj.IsValid && obj.Name.Contains("Viktor_Base_R"))
                {
                    //Game.PrintChat("woot2");
                    activeR = false;
                    rObj = null;
                }
            }
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!menu.Item("UseInt").GetValue<bool>()) return;

            if (Player.Distance(unit) < R.Range)
            {
                R.Cast(unit.ServerPosition, menu.Item("packet").GetValue<bool>());
            }
        }

        public static void AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe && chargeQ)
            {
                Orbwalker.SetMovement(true);
                chargeQ = false;
            }

            chargeQ = false;
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);
                damage += 5 * Player.GetSpellDamage(enemy, SpellSlot.R, 1);
            }

            return (float)damage;
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, String Source)
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var eTarget2 = SimpleTs.GetTarget(1200, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            var immobile = menu.Item("wSlow").GetValue<bool>();
            var slow = menu.Item("wImmobile").GetValue<bool>();
            var dashing = menu.Item("wImmobile").GetValue<bool>();

            if (useW && wTarget != null && W.IsReady() && Player.Distance(wTarget) <= W.Range
                && (!(menu.Item("wMulti").GetValue<bool>()) || GetComboDamage(wTarget) >= wTarget.Health || W.GetPrediction(wTarget).Hitchance == HitChance.Immobile && immobile ||
                wTarget.HasBuffOfType(BuffType.Slow) && slow || W.GetPrediction(wTarget).Hitchance == HitChance.Dashing && dashing || Player.Distance(wTarget.ServerPosition) < 300))
            {
                W.Cast(wTarget, menu.Item("packet").GetValue<bool>());
            }
            if(menu.Item("QAARange").GetValue<bool>()){
                if (useQ && qTarget != null && Q.IsReady() && Player.Distance(qTarget) <= Player.AttackRange) // Q only in AA range for guaranteed AutoAttack
                {
                    Q.CastOnUnit(qTarget, menu.Item("packet").GetValue<bool>());
                       Player.IssueOrder(GameObjectOrder.AttackUnit, qTarget);
                    return;
                }
            }
            else{
            if (useQ && qTarget != null && Q.IsReady() && Player.Distance(qTarget) <= Q.Range)
            {
                Q.CastOnUnit(qTarget, menu.Item("packet").GetValue<bool>());

                //force auto
                if (menu.Item("autoAtk").GetValue<bool>())
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, qTarget);
                }
                return;
            }
            }

            if (useR && rTarget != null && R.IsReady() && (GetComboDamage(rTarget) > qTarget.Health || menu.Item("rAlways").GetValue<KeyBind>().Active) && Player.Distance(rTarget) <= R.Range && !activeR)
            {
                R.Cast(rTarget.ServerPosition, menu.Item("packet").GetValue<bool>());
            }

            if (useE && E.IsReady())
            {

                if (Player.Distance(eTarget2.ServerPosition) <= 1200 && Player.Distance(eTarget2.ServerPosition) > E.Range && eTarget2 != null)
                {
                    eCalc2(Source);
                    return;
                }
                else if (eTarget.Distance(Player.ServerPosition) <= E.Range && eTarget != null)
                {
                    eCalc(eTarget, Source);
                    return;
                }
            }
        }

        private static void Combo()
        {
            Orbwalker.SetAttacks(!(Q.IsReady()));
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>(), "Combo");
        }

        private static void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), false,
                menu.Item("UseEHarass").GetValue<bool>(), false, "Harass");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            Orbwalker.SetAttacks(true);

            int rTimeLeft = Environment.TickCount - lastR;
            if ((rTimeLeft <= 750))
            {
                autoR();
                lastR = Environment.TickCount - 250;
            }
            
            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                mecR();
                mecW();
                Combo();
            }
            else
            {
                if (menu.Item("LastHitQQ").GetValue<KeyBind>().Active)
                    lastHit();

                if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    Farm();

                if (menu.Item("HarassActive").GetValue<KeyBind>().Active || menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();

            }
            
        }

        public static void autoR()
        {
            if (activeR)
            {
                var nearChamps = (from champ in ObjectManager.Get<Obj_AI_Hero>() where rObj.Position.Distance(champ.ServerPosition) < 2500 && champ.IsEnemy select champ).ToList();
                nearChamps.OrderBy(x => rObj.Position.Distance(x.ServerPosition));
                R.Cast(nearChamps.First().ServerPosition, menu.Item("packet").GetValue<bool>());
            }
        }

        public static PredictionOutput GetP(Vector3 pos, Spell spell, Obj_AI_Base target, bool aoe)
        {

            return Prediction.GetPrediction(new PredictionInput
            {
                Unit = target,
                Delay = spell.Delay,
                Radius = spell.Width,
                Speed = spell.Speed,
                From = pos,
                Range = spell.Range,
                Collision = spell.Collision,
                Type = spell.Type,
                RangeCheckFrom = pos,
                Aoe = aoe,
            });
        }

        private static void castE(Vector3 source, Vector3 destination)
        {
            castE(source.To2D(), destination.To2D());
        }

        private static void castE(Vector2 source, Vector2 destination)
        {
            Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, SpellSlot.E, -1, source.X, source.Y, destination.X, destination.Y)).Send();
        }

        public static void eCalc(Obj_AI_Hero eTarget, String Source)
        {
            var hitC = HitChance.High;
            var qHit = menu.Item("eHit").GetValue<Slider>().Value;
            var harassQHit = menu.Item("eHit2").GetValue<Slider>().Value;

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

            //case where target is in range of E
            if (Player.Distance(eTarget.ServerPosition) <= E.Range && eTarget != null)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (enemy != null && !enemy.IsDead && eTarget.BaseSkinName != enemy.BaseSkinName && enemy.IsEnemy)
                    {
                        if (eTarget.Distance(enemy) <= E2.Range) //check if another target is in range
                        {
                            var pred = GetP(eTarget.ServerPosition, E2, enemy, true);

                            if (pred.Hitchance >= hitC)
                            {
                                castE(eTarget.ServerPosition, pred.CastPosition);
                                return;
                            }
                        }
                    }
                }

                var midPos = new Vector3((Player.ServerPosition.X + eTarget.ServerPosition.X) / 2, (Player.ServerPosition.Y + eTarget.ServerPosition.Y) / 2,
                    (Player.ServerPosition.Z + eTarget.ServerPosition.Z) / 2);

                var pred2 = GetP(midPos, E2, eTarget, true);

                if (pred2.Hitchance >= hitC)
                {
                    castE(midPos, pred2.CastPosition);
                    return;
                }
            }
        }

        public static void eCalc2(String Source)
        {
            var hitC = HitChance.High;
            var qHit = menu.Item("eHit").GetValue<Slider>().Value;
            var harassQHit = menu.Item("eHit2").GetValue<Slider>().Value;

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

            var eTarget2 = SimpleTs.GetTarget(1200, SimpleTs.DamageType.Magical);

            if (eTarget2 != null && Player.Distance(eTarget2) <= 1200)
            {   
                // Get initial start point at the border of cast radius
                Vector3 startPos = Player.Position + Vector3.Normalize(eTarget2.ServerPosition - Player.Position) * (E.Range - 50);
                var pred = GetP(startPos, E2, eTarget2, true);

                if (pred.Hitchance >= hitC)
                {
                    castE(startPos, pred.CastPosition);
                    return;
                }
            }
        }

        public static void mecR()
        {
            var minHit = menu.Item("useR_Hit").GetValue<Slider>().Value;
            if (minHit == 0)
                return;
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            if(rTarget != null && rTarget.Distance(Player.ServerPosition) <= R.Range && R.IsReady())
                if (R.GetPrediction(rTarget).Hitchance >= HitChance.High)
                {
                    R.CastIfWillHit(rTarget, 2);
                }
        }

        public static void mecW()
        {
            var minHit = menu.Item("useW_Hit").GetValue<Slider>().Value;
            if (minHit == 0)
                return;

            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);

            if (wTarget != null && wTarget.Distance(Player.ServerPosition) <= W.Range && W.IsReady())
                if (W.GetPrediction(wTarget).Hitchance >= HitChance.High)
                {
                    W.CastIfWillHit(wTarget, 2);
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
                    if (minion.IsValidTarget() && HealthPrediction.GetHealthPrediction(minion, (int)(Player.Distance(minion) * 1000 / 1400)) < Player.GetSpellDamage(minion, SpellSlot.Q) - 10)
                    {
                        Q.Cast(minion, menu.Item("packet").GetValue<bool>());
                        return;
                    }
                }
            }
        }

        private static void Farm()
        {
            if (!Orbwalking.CanMove(40)) return;

            var MinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width, MinionTypes.All, MinionTeam.NotAlly);
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (useQ && allMinionsQ.Count > 0 && Q.IsReady())
            {
                Q.Cast(allMinionsQ[0], menu.Item("packet").GetValue<bool>());
            }

            if (useE && E.IsReady())
            {
                var minion = MinionsE[0];
                foreach (var enemy in MinionsE)
                {
                    if (Player.Distance(enemy) >= Player.Distance(minion))
                        minion = enemy;
                }

                castE(MinionsE[0].ServerPosition, minion.ServerPosition);
            }

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = menu.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);

                var menu2 = menu.Item("E2Range").GetValue<Circle>();
                if (menu2.Active)
                    Utility.DrawCircle(Player.Position, 1200, menuItem.Color);
            }

        }
    }
}
