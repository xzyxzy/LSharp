using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;


namespace LissIceBladeDancer
{
    class Program
    {
        public const string ChampionName = "Lissandra";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell Q2;
        public static Spell W;
        public static Spell E;
        public static Obj_SpellMissile epos;
        public static bool ecreated = false;
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
            Q = new Spell(SpellSlot.Q, 725);
            Q2 = new Spell(SpellSlot.Q, 825);
            W = new Spell(SpellSlot.W, 450);
            E = new Spell(SpellSlot.E, 1050);
            R = new Spell(SpellSlot.R, 700);

            Q.SetSkillshot(0.50f, 100, 1300, false, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.50f, 100, 1300, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.50f, 110, 850, false, SkillshotType.SkillshotLine);

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
            menu.SubMenu("Farm").AddItem(new MenuItem("LastHitQQ", "Last hit with Q").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
            menu.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind(menu.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseInt", "Use R to Interrupt").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseGap", "Use W for GapCloser").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseHAM", "Ham With E").SetValue(false));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseEGap", "Use E to Gap Close").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("gapD", "Min Distance").SetValue(new Slider(600, 300, 1050)));
            menu.SubMenu("Misc").AddItem(new MenuItem("MoveToMouse", "MoveToMouse only").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));

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
            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            //if(Q.IsReady())
            damage += DamageLib.getDmg(enemy, DamageLib.SpellType.Q);

            //if (W.IsReady())
            damage += DamageLib.getDmg(enemy, DamageLib.SpellType.W);

            if (E.IsReady())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.E);

            if (R.IsReady())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.R);

            return (float)damage;
        }

        private static void Combo()
        {
            Orbwalker.SetAttacks(!(Q.IsReady() || menu.Item("MoveToMouse").GetValue<KeyBind>().Active));
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>());
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR)
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var q2Target = SimpleTs.GetTarget(Q2.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            if (useW && wTarget != null && W.IsReady() && Player.Distance(wTarget) <= W.Range)
            {
                W.Cast();
                return;
            }

            if (useQ && Q.IsReady())
            {
                if (Player.Distance(qTarget) <= Q.Range && qTarget != null && Q.GetPrediction(qTarget).Hitchance >= HitChance.Medium)
                {
                    Q.Cast(qTarget, true, true);
                    return;
                }
                else if (q2Target != null && Player.Distance(qTarget) <= Q2.Range)
                {
                    var qCollision = Q2.GetPrediction(q2Target).CollisionObjects;
                    foreach (var qCollisionChar in qCollision.Where(qCollisionChar => qCollisionChar.IsValidTarget(Q.Range)))
                    {
                        Q2.Cast(qCollisionChar, true, true);
                        return;
                    }
                }
            }

            if (useE && eTarget != null && E.IsReady() && Player.Distance(eTarget) < E.Range && (GetComboDamage(rTarget) >= qTarget.Health || menu.Item("UseHAM").GetValue<bool>() ||
                menu.Item("HarassActive").GetValue<KeyBind>().Active || menu.Item("HarassActiveT").GetValue<KeyBind>().Active) && !ecreated)
            {
                E.Cast(eTarget, true);
                return;
            }

            if (useR && rTarget != null && R.IsReady() && GetComboDamage(rTarget) >= rTarget.Health && Player.Distance(rTarget) < R.Range)
            {
                R.CastOnUnit(rTarget);
                return;
            }

        }

        private static void Harass()
        {
            Orbwalker.SetAttacks(!(menu.Item("MoveToMouse").GetValue<KeyBind>().Active));
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(),
                menu.Item("UseEHarass").GetValue<bool>(), false);
        }

        public static void detonateE()
        {
            var enemy = SimpleTs.GetTarget(2000, SimpleTs.DamageType.Magical);
            var distanceToEnemy = enemy.Distance(epos.Position);
            var endpos = epos.EndPosition;
            var endposToEnemy = enemy.Distance(endpos);

            if (epos != null && enemy.ServerPosition.Distance(epos.Position) < 110 && enemy != null && ecreated && menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                E.Cast();
                return;
            }
            else if (epos != null && enemy != null && ecreated && menu.Item("ComboActive").GetValue<KeyBind>().Active && menu.Item("UseEGap").GetValue<bool>()
                && Player.Distance(enemy) > distanceToEnemy)
            {
                if (endpos.Distance(epos.Position) < 400 && distanceToEnemy < endposToEnemy)
                    E.Cast();
                else if (epos.Position == endpos)
                    E.Cast();
            }

        }

        public static void gapClose()
        {
            var Target = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Magical);
            var distance = menu.Item("gapD").GetValue<Slider>().Value;

            if (Player.Distance(Target.ServerPosition) >= distance && Target.IsValidTarget(E.Range) && !ecreated && E.GetPrediction(Target).Hitchance >= HitChance.High)
            {
                E.Cast(Target, true);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            detonateE();

            Orbwalker.SetAttacks(true);

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                if (menu.Item("UseEGap").GetValue<bool>())
                    gapClose();

                Combo();
            }
            else
            {
                if (menu.Item("HarassActive").GetValue<KeyBind>().Active || menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                {
                    Farm();
                }

                if (menu.Item("LastHitQQ").GetValue<KeyBind>().Active)
                {
                    lastHit();
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
                        Q.Cast(minion, true);
                        return;
                    }
                }
            }
        }

        private static void Farm()
        {
            //if (!Orbwalking.CanMove(40)) return;

            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All);
            var allMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width, MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useW = menu.Item("UseWFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (useE && E.IsReady() && !ecreated)
            {
                var ePos = E.GetLineFarmLocation(allMinionsE);
                if (ePos.MinionsHit >= 3)
                    E.Cast(ePos.Position, true);
            }

            if (useQ && Q.IsReady())
            {
                var qPos = Q.GetLineFarmLocation(allMinionsQ);
                if (qPos.MinionsHit >= 2)
                    Q.Cast(qPos.Position, true);
            }

            if (useW && W.IsReady())
            {
                var wPos = W.GetLineFarmLocation(allMinionsW);
                if (wPos.MinionsHit >= 2)
                    W.Cast();
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

        }

        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!menu.Item("UseGap").GetValue<bool>()) return;

            if (W.IsReady() && gapcloser.Sender.IsValidTarget(W.Range))
                W.Cast();
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!menu.Item("UseInt").GetValue<bool>()) return;

            if (Player.Distance(unit) < R.Range && unit != null)
            {
                R.CastOnUnit(unit);
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            var spell = (Obj_SpellMissile)sender;
            var unit = spell.SpellCaster.Name;
            var name = spell.SData.Name;

            if (unit == ObjectManager.Player.Name && name == "LissandraEMissile")
            {
                epos = spell;
                ecreated = true;
                return;
            }
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            var spell = (Obj_SpellMissile)sender;
            var unit = spell.SpellCaster.Name;
            var name = spell.SData.Name;

            if (unit == ObjectManager.Player.Name && name == "LissandraEMissile")
            {
                epos = null;
                ecreated = false;
                return;
            }
        }

    }
}
