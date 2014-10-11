using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace BlitzcrankGrabDAT
{
    class Program
    {
        public const string ChampionName = "Blitzcrank";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
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
            Player = ObjectManager.Player;

            //check to see if correct champ
            if (Player.BaseSkinName != ChampionName) return;

            //intalize spell
            Q = new Spell(SpellSlot.Q, 950);
            W = new Spell(SpellSlot.W, float.MaxValue);
            E = new Spell(SpellSlot.E, 140);
            R = new Spell(SpellSlot.R, 600);

            Q.SetSkillshot(0.22f, 70f, 1800, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.25f, 600, float.MaxValue, false, SkillshotType.SkillshotCircle);

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
            menu.SubMenu("Combo").AddItem(new MenuItem("qHit", "Q HitChance").SetValue(new Slider(3, 1, 4)));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("QE", "Use E on Grab").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("useRQ", "Use R After Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(menu.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("qHit2", "Q HitChance").SetValue(new Slider(3, 1, 4)));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind(menu.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Harass").AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseInt", "Use R to Interrupt").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("packet", "Use Packets").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("qRange2", "Q Min Range Slider").SetValue(new Slider(300, 1, 950)));
            menu.SubMenu("Misc").AddItem(new MenuItem("qRange", "Q Max Range Slider").SetValue(new Slider(900, 1, 950)));
            menu.SubMenu("Misc").AddItem(new MenuItem("qSlow", "Auto Q Slow").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("qImmobile", "Auto Q Immobile").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("qDashing", "Auto Q Dashing").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("resetE", "Use E AA reset Only").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("autoR", "Use R if hit").SetValue(new Slider(3, 0, 5)));
            menu.SubMenu("Misc").AddItem(new MenuItem("panic", "Panic Key(no spell)").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Toggle)));

            menu.SubMenu("Misc").AddSubMenu(new Menu("Don't use Q on", "intR"));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                menu.SubMenu("Misc")
                    .SubMenu("intR")
                    .AddItem(new MenuItem("intR" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));

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
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
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
            Orbwalker.SetAttacks(!(Q.IsReady()));
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

            var hitC = HitChance.High;
            var qHit = menu.Item("qHit").GetValue<Slider>().Value;
            var harassQHit = menu.Item("qHit2").GetValue<Slider>().Value;
            var qRange = menu.Item("qRange").GetValue<Slider>().Value;

            var RQ = menu.Item("useRQ").GetValue<bool>();
            var QE = menu.Item("QE").GetValue<bool>();

            Q.Range = qRange;

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

            if (useW && wTarget != null && W.IsReady() && Player.Distance(wTarget) <= 1200)
            {
                W.Cast();
            }

            if (useQ && Q.IsReady() && Player.Distance(qTarget) <= Q.Range && qTarget != null && (Q.GetPrediction(qTarget).Hitchance >= hitC || shouldUseQ(qTarget)) && useQonEnemy(qTarget))
            {
                Q.Cast(qTarget, packets());

                if (QE && useE && E.IsReady())
                    E.Cast();
            }

            if (useE && eTarget != null && E.IsReady() && Player.Distance(eTarget) < 300 && !menu.Item("resetE").GetValue<bool>() && !Q.IsReady())
            {
                E.Cast();
            }

            if (useR && rTarget != null && R.IsReady() && Player.Distance(rTarget) < R.Range)
            {
                if (RQ && Q.IsReady())
                    return;

                R.Cast();
            }

        }

        public static bool shouldUseQ(Obj_AI_Hero target)
        {
            var slow = menu.Item("qSlow").GetValue<bool>();
            var immobile = menu.Item("qImmobile").GetValue<bool>();
            var dashing = menu.Item("qDashing").GetValue<bool>();

            if (Q.GetPrediction(target).Hitchance == HitChance.Dashing && dashing)
                return true;

            if (Q.GetPrediction(target).Hitchance == HitChance.Immobile && immobile)
                return true;

            if (target.HasBuffOfType(BuffType.Slow) && slow && Q.GetPrediction(target).Hitchance >= HitChance.High)
                return true;

            return false;
        }

        public static void autoQ()
        {
            var nearChamps = (from champ in ObjectManager.Get<Obj_AI_Hero>() where Player.Distance(champ.ServerPosition) < Q.Range && champ.IsEnemy select champ).ToList();
            nearChamps.OrderBy(x => x.Health);

            if (shouldUseQ(nearChamps.First()) && useQonEnemy(nearChamps.First()))
                Q.Cast(nearChamps.First().ServerPosition, menu.Item("packet").GetValue<bool>());
        }

        public static PredictionOutput GetPCircle(Vector3 pos, Spell spell, Obj_AI_Base target, bool aoe)
        {

            return Prediction.GetPrediction(new PredictionInput
            {
                Unit = target,
                Delay = spell.Delay,
                Radius = 1,
                Speed = spell.Speed,
                From = pos,
                Range = float.MaxValue,
                Collision = spell.Collision,
                Type = spell.Type,
                RangeCheckFrom = Player.ServerPosition,
                Aoe = aoe,
            });
        }

        public static void checkRMec()
        {
            int hit = 0;
            var minHit = menu.Item("autoR").GetValue<Slider>().Value;
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                if (enemy != null && !enemy.IsDead)
                {
                    var prediction = GetPCircle(Player.ServerPosition, R, enemy, true);

                    if (R.IsReady() && enemy.Distance(Player.ServerPosition) <= R.Width && prediction.CastPosition.Distance(Player.ServerPosition) <= R.Width && prediction.Hitchance >= HitChance.High)
                    {
                        hit++;
                    }
                }
            }

            if (hit >= minHit)
                R.Cast();
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            Orbwalker.SetAttacks(true);

            if (menu.Item("panic").GetValue<KeyBind>().Active)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                return;
            }

            //Q grab on immobile
            autoQ();

            //Rmec
            checkRMec();

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();

                if(menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();
            }
        }

        public static bool useQonEnemy(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.SpellImmunity))
            {
                //Game.PrintChat("Spell immune");
                return false;
            }

            var qRangeMin = menu.Item("qRange2").GetValue<Slider>().Value;
            if (Player.Distance(target.ServerPosition) < qRangeMin)
                return false;

            if (menu.Item("intR" + target.BaseSkinName) != null)
                if (menu.Item("intR" + target.BaseSkinName).GetValue<bool>() == true)
                return false;

            return true;
        }

        public static bool packets()
        {
            return menu.Item("packet").GetValue<bool>();
        }

        public static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            var useECombo = menu.Item("UseECombo").GetValue<bool>();
            var useEHarass = menu.Item("UseEHarass").GetValue<bool>();

            if (unit.IsMe)
            {
                if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
                {
                    if (useECombo && E.IsReady())
                    {
                        Orbwalking.ResetAutoAttackTimer();
                        E.Cast();
                    }
                }

                if (menu.Item("HarassActive").GetValue<KeyBind>().Active || menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                {
                    if (useEHarass && E.IsReady())
                    {
                        Orbwalking.ResetAutoAttackTimer();
                        E.Cast();
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = menu.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                {
                    if (spell == Q) {
                        var qRange = menu.Item("qRange").GetValue<Slider>().Value;

                        Q.Range = qRange;
                    }
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
                }
            }

        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs attack)
        {

        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!menu.Item("UseInt").GetValue<bool>()) return;

            if (Player.Distance(unit) < Q.Range && unit != null)
            {
                if (Q.GetPrediction(unit).Hitchance >= HitChance.High)
                    Q.Cast(unit, packets());
            }

            if (Player.Distance(unit) < R.Range && unit != null)
            {
                R.CastOnUnit(unit, packets());
            }
        }
    }
}
