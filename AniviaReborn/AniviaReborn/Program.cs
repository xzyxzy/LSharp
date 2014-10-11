using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace AniviaReborn
{
    class Program
    {
        public const string ChampionName = "Anivia";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static float[] wWidth = { 400f, 500f, 600f, 700f, 800f };

        //Spell Obj
        //Q
        public static GameObject qMissle = null;
        public static bool qFirstCreated = false;
        public static Vector3 qPos;

        //E
        public static bool eCasted = false;

        //R
        public static GameObject rObj = null;
        public static bool rFirstCreated = false;
        public static bool rByMe = false;

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
            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 650);
            R = new Spell(SpellSlot.R, 625);

            Q.SetSkillshot(.25f, 75f, 650f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(.25f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(.25f, 100f, float.MaxValue, false, SkillshotType.SkillshotCircle);

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

            //key menu
            menu.AddSubMenu(new Menu("Key", "Key"));
            menu.SubMenu("Key").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(menu.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Key").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind(menu.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Key").AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));
            menu.SubMenu("Key").AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind(menu.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Key").AddItem(new MenuItem("snipe", "W/Q Snipe").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            menu.SubMenu("Key").AddItem(new MenuItem("escape", "RUN FOR YOUR LIFE!").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            //Combo menu:
            menu.AddSubMenu(new Menu("Combo", "Combo"));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));

            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseRHarass", "Use R").SetValue(true));

            //Farming menu:
            menu.AddSubMenu(new Menu("Farm", "Farm"));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseRFarm", "Use R").SetValue(false));
            
            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseInt", "Use Spells to Interrupt").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseGap", "Use W for GapCloser").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("packet", "Use Packets").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("checkR", "Auto turn off R").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("detonateQ", "Auto Detonate Q").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("detonateQ2", "Pop Q Behind Enemy").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("wallKill", "Wall Enemy on killable").SetValue(true));

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
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (E.IsReady() & (Q.IsReady() || R.IsReady()))
                damage += Player.GetSpellDamage(enemy, SpellSlot.E) * 2;
            else if(E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R) * 3;

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
                menu.Item("UseEHarass").GetValue<bool>(), menu.Item("UseRHarass").GetValue<bool>(), "Harass");
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, string Source)
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            if (useE && eTarget != null && E.IsReady() && Player.Distance(eTarget) < E.Range && shouldE(eTarget, Source))
            {
                E.CastOnUnit(eTarget, packets());
            }

            if (useQ && Q.IsReady() && Player.Distance(qTarget) <= Q.Range && qTarget != null && Q.GetPrediction(qTarget).Hitchance >= HitChance.High && shouldQ(qTarget))
            {
                var qPos2 = Q.GetPrediction(qTarget).CastPosition;
                var vec = new Vector3(qPos2.X - Player.ServerPosition.X, 0, qPos2.Z - Player.ServerPosition.Z);
                var CastBehind = qPos2 + Vector3.Normalize(vec) * 100;

                qPos = CastBehind;
                Q.Cast(qTarget, packets());
                qFirstCreated = true;
            }

            if (useW && wTarget != null && W.IsReady() && Player.Distance(wTarget) <= W.Range && shouldUseW(qTarget))
            {
                castW(wTarget);
            }

            if (useR && rTarget != null && R.IsReady() && Player.Distance(rTarget) < R.Range && shouldR(rTarget, Source) && R.GetPrediction(rTarget).Hitchance >= HitChance.High)
            {
                R.Cast(R.GetPrediction(rTarget).CastPosition, packets());
                rFirstCreated = true;
                rByMe = true;
            }

        }

        public static bool shouldQ(Obj_AI_Hero target)
        {
            if (qFirstCreated)
                return false;

            return true;
        }

        public static bool shouldR(Obj_AI_Hero target, string source)
        {
            if (rObj != null && rFirstCreated)
                return false;

            if (rByMe)
                return false;

            if (eCasted)
                return true;

            if (source == "Combo")
                return true;

            return false;
        }

        public static bool shouldE(Obj_AI_Hero target, string source)
        {
            if (checkChilled(target))
                return true;

            if (Player.GetSpellDamage(target, SpellSlot.E) > target.Health)
                return true;

            if (R.IsReady() && Player.Distance(target) <= R.Range - 25 && Player.Distance(target.ServerPosition) > 250)
                return true;
                
            return false;
        }
        public static bool shouldUseW(Obj_AI_Hero target)
        {
            if (GetComboDamage(target) >= target.Health - 20 && menu.Item("wallKill").GetValue<bool>())
                return true;

            if(rFirstCreated && rObj != null)
            {
                if (rObj.Position.Distance(target.ServerPosition) > 300)
                {
                    return true;
                }
            }

            return false;
        }

        public static void castW(Obj_AI_Hero target)
        {
            var pred = W.GetPrediction(target);
            var vec = new Vector3(pred.CastPosition.X - Player.ServerPosition.X, 0, pred.CastPosition.Z - Player.ServerPosition.Z);
            var CastBehind = pred.CastPosition + Vector3.Normalize(vec) * 125;

            W.Cast(CastBehind, packets());
        }

        /*public static void castWBetween()
        {
            var enemy = (from champ in ObjectManager.Get<Obj_AI_Hero>() where Player.Distance(champ.ServerPosition) < W.Range && champ.IsEnemy && champ.IsValid select champ).ToList();
            enemy.OrderBy(x => rObj.Position.Distance(x.ServerPosition));

            castW(enemy.FirstOrDefault());
        }*/

        public static void castWEscape(Obj_AI_Hero target)
        {
            var pred = W.GetPrediction(target);
            var vec = new Vector3(pred.CastPosition.X - Player.ServerPosition.X, 0, pred.CastPosition.Z - Player.ServerPosition.Z);
            var CastBehind = pred.CastPosition - Vector3.Normalize(vec) * 125;

            W.Cast(CastBehind, packets());
        }

        public static bool checkChilled(Obj_AI_Hero target)
        {
            return target.HasBuff("Chilled");
        }

        public static void detonateQ()
        {
            var Q2 = menu.Item("detonateQ2").GetValue<bool>();

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget()))
            {
                if (qMissle != null && enemy.ServerPosition.Distance(qMissle.Position) < 110 && enemy != null && Q.IsReady())
                {
                    //check if user wnat chill to behind target
                    if (Q2)
                    {
                        if (checkChilled(enemy))
                            Q.Cast();
                        else
                            return;
                    }
                    else
                        Q.Cast();

                    return;
                }
            }
        }

        public static bool shouldDetonate(Obj_AI_Hero target)
        {
            if (target.ServerPosition.Distance(qMissle.Position) < 110)
                return true;

            if (qMissle.Position.Distance(qPos) < 50)
                return true;

            return false;
        }
        public static void snipe()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if(W.IsReady() && Q.IsReady() && Player.Distance(qTarget.ServerPosition) < W.Range)
                castW(qTarget);

            if (!W.IsReady() && Q.IsReady() && Player.Distance(qTarget.ServerPosition) < Q.Range && Q.GetPrediction(qTarget).Hitchance >= HitChance.High && !qFirstCreated)
            {
                Q.Cast(Q.GetPrediction(qTarget).CastPosition, packets());
                qFirstCreated = true;
            }
        }

        public static void checkR()
        {
            int hit = 0;
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget()))
            {
                if (rObj != null && enemy != null && R.IsReady() && rObj.Position.Distance(enemy.ServerPosition) < 450 + 50)
                {
                    hit++;
                }
            }

            if (hit < 1 && R.IsReady() && rObj != null && rFirstCreated)
            {
                R.Cast();
            }
        }

        public static void escape()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            var enemy = (from champ in ObjectManager.Get<Obj_AI_Hero>() where Player.Distance(champ.ServerPosition) < 2500 && champ.IsEnemy && champ.IsValid select champ).ToList();
            enemy.OrderBy(x => rObj.Position.Distance(x.ServerPosition));

            if (Q.IsReady() && Player.Distance(enemy.FirstOrDefault()) <= Q.Range && enemy != null && Q.GetPrediction(enemy.FirstOrDefault()).Hitchance >= HitChance.High && !qFirstCreated)
            {
                Q.Cast(enemy.FirstOrDefault(), packets());
            }

            if (enemy != null && W.IsReady() && Player.Distance(enemy.FirstOrDefault()) <= W.Range)
            {
                castWEscape(enemy.FirstOrDefault());
            }

        }

        private static void Farm()
        {
            if (!Orbwalking.CanMove(40)) return;

            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            var allMinionsR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All, MinionTeam.NotAlly);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();
            var useR = menu.Item("UseRFarm").GetValue<bool>();

            int hit = 0;

            if (useQ && Q.IsReady() && !qFirstCreated)
            {
                var qPos = Q.GetLineFarmLocation(allMinionsQ);
                if (qPos.MinionsHit >= 3)
                {
                    Q.Cast(qPos.Position, packets());
                }
            }

            if (useR & R.IsReady() && !rFirstCreated)
            {
                var rPos = R.GetCircularFarmLocation(allMinionsR);
                if (Player.Distance(rPos.Position) < R.Range)
                    R.Cast(rPos.Position, packets());
            }

            if (qFirstCreated)
            {
                if (useQ && Q.IsReady())
                {
                    foreach (var enemy in allMinionsQ)
                    {
                        if (enemy.Distance(qMissle.Position) < 75)
                            hit++;
                    }
                }

                if (hit > 2)
                    Q.Cast();
            }

            if (rFirstCreated)
            {
                foreach (var enemy in allMinionsR)
                {
                    if (enemy.Distance(rObj.Position) < 400)
                        hit++;
                }

                if (hit < 2)
                    R.Cast();
            }

            if (useE && allMinionsE.Count > 0 && E.IsReady())
                E.Cast(allMinionsE[0]);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead)
            {
                //reset on death
                qMissle = null;
                qFirstCreated = false;
                eCasted = false;
                //rObj = null;
                //rFirstCreated = false;
                return;
            }

            Orbwalker.SetAttacks(true);

            //detonate Q check
            var detQ = menu.Item("detonateQ").GetValue<bool>();
            if (detQ && qFirstCreated)
                detonateQ();

            //checkR
            var rCheck = menu.Item("checkR").GetValue<bool>();
            if (rCheck && rFirstCreated && !menu.Item("LaneClearActive").GetValue<KeyBind>().Active && rByMe)
                checkR();

            if (menu.Item("escape").GetValue<KeyBind>().Active)
                escape();

            if (menu.Item("snipe").GetValue<KeyBind>().Active)
                snipe();

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
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs attack)
        {
            if (unit.IsMe)
            {
                if (attack.SData.Name == "Frostbite")
                {
                    eCasted = true;
                }

                if (attack.SData.Name == "FlashFrost" && !qFirstCreated)
                {
                    //Game.PrintChat("woot");
                    qFirstCreated = true;
                }
            }
        }

        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!menu.Item("UseGap").GetValue<bool>()) return;

            if (W.IsReady() && gapcloser.Sender.IsValidTarget(W.Range))
            {
                var vec = Player.ServerPosition - Vector3.Normalize(Player.ServerPosition - gapcloser.Sender.ServerPosition) * 1;
                    W.Cast(vec, packets());
            }
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!menu.Item("UseInt").GetValue<bool>()) return;

            if (Player.Distance(unit) < Q.Range && unit != null)
            {
                if (Q.GetPrediction(unit).Hitchance >= HitChance.High)
                    Q.Cast(unit, packets());
            }

            if (Player.Distance(unit) < W.Range && unit != null)
            {
                W.Cast(unit, packets());
            }
        }

        private static void OnCreate(GameObject obj, EventArgs args)
        {
            //if(Player.Distance(obj.Position) < 1500)
                //Game.PrintChat("OBJ: " + obj.Name);

            if (Player.Distance(obj.Position) < 1500)
            {
                //Q
                if (obj != null && obj.IsValid && obj.Name == "cryo_FlashFrost_Player_mis.troy")
                {
                    qMissle = obj;
                    qFirstCreated = true;
                }

                //R
                if (obj != null && obj.IsValid && obj.Name.Contains("cryo_storm"))
                {
                    rObj = obj;
                    rFirstCreated = true;
                    return;
                }
            }
        }

        private static void OnDelete(GameObject obj, EventArgs args)
        {
            //if (Player.Distance(obj.Position) < 300)
                //Game.PrintChat("OBJ2: " + obj.Name);

            if (Player.Distance(obj.Position) < 1500)
            {
                //Q
                if (Player.Distance(obj.Position) < 1500)
                {
                    if (obj != null && obj.IsValid && obj.Name == "cryo_FlashFrost_Player_mis.troy")
                    {
                        qMissle = null;
                        qFirstCreated = false;
                    }

                    if (obj != null && obj.IsValid && obj.Name == "cryo_FrostBite_tar.troy")
                    {
                        eCasted = false;
                    }

                    //R
                    if (obj != null && obj.IsValid && obj.Name.Contains("cryo_storm"))
                    {
                        rObj = null;
                        rFirstCreated = false;
                        rByMe = false;
                    }
                }
            }
        }


    }
}
