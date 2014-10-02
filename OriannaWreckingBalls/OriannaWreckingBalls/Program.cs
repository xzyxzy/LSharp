using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace OriannaWreckingBalls
{
    class Program
    {
        public const string ChampionName = "Orianna";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        //ball manager
        public static GameObject qpos;
        public static bool IsBallMoving = false;
        public static Vector3 CurrentBallPosition;
        public static int ballStatus = 0;

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
            Q = new Spell(SpellSlot.Q, 825);
            W = new Spell(SpellSlot.W, 220);
            E = new Spell(SpellSlot.E, 1095);
            R = new Spell(SpellSlot.R, 300);

            Q.SetSkillshot(0f, 135, 1150, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 245, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 145, 1700, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.60f, 350, float.MaxValue, false, SkillshotType.SkillshotCircle);

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
            menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(menu.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("qHit2", "Q HitChance").SetValue(new Slider(3, 1, 4)));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind(menu.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Harass").AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));

            //Farming menu:
            menu.AddSubMenu(new Menu("Farm", "Farm"));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("qFarm", "Only Q/W if > minion").SetValue(new Slider(3, 0, 5)));
            menu.SubMenu("Farm").AddItem(new MenuItem("LastHitQQ", "Last hit with Q").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
            menu.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind(menu.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            //menu.SubMenu("Misc").AddItem(new MenuItem("UseInt", "Use R to Interrupt").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("autoW", "Use W if hit").SetValue(new Slider(2, 0, 5)));
            menu.SubMenu("Misc").AddItem(new MenuItem("killR", "R Multi Only Toggle").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));
            menu.SubMenu("Misc").AddItem(new MenuItem("autoR", "Use R if hit").SetValue(new Slider(3, 0, 5)));
            menu.SubMenu("Misc").AddItem(new MenuItem("autoE", "E If HP < %").SetValue(new Slider(40, 0, 100)));
            menu.SubMenu("Misc").AddItem(new MenuItem("blockR", "Block R if no enemy").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("overK", "OverKill Check").SetValue(true));
            //menu.SubMenu("Combo").AddItem(new MenuItem("autoEDmg", "E If HP < %").SetValue(new Slider(2, 0, 5)));

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
            //Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Game.OnGameSendPacket += Game_OnSendPacket;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
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
                RangeCheckFrom = Player.ServerPosition,
                Aoe = aoe,
            });
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

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            //if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);

            return (float)damage;
        }

        private static void Combo()
        {
            //Orbwalker.SetAttacks(!(Q.IsReady()));
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>(), "Combo");
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, String source)
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(2000, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Magical);

            if (useE && eTarget != null && E.IsReady())
            {
                castE(eTarget);
            }

            if (useW && wTarget != null && W.IsReady())
            {
                castW(wTarget);
            }

            if (useQ && Q.IsReady())
            {
                castQ(qTarget, source);
                return;
            }

            if (useR && rTarget != null && R.IsReady())
            {
                if (!(menu.Item("killR").GetValue<KeyBind>().Active))
                {
                    if (menu.Item("overK").GetValue<bool>())
                    {
                        if ((Player.GetSpellDamage(rTarget, SpellSlot.R)) >= rTarget.Health)
                            return;
                    }
                    else
                    {
                        if (GetComboDamage(rTarget) >= rTarget.Health - 100)
                            castR(rTarget);
                    }
                }
            }

        }

        public static void castW(Obj_AI_Base target)
        {
            if (IsBallMoving) return;

            switch (ballStatus) { 
                //on self
                case 0:
                    var prediction = GetPCircle(Player.ServerPosition, W, target, true);

                    if (W.IsReady() && target.Distance(Player.ServerPosition) <= W.Width && prediction.CastPosition.Distance(Player.ServerPosition) <= W.Width && prediction.Hitchance >= HitChance.Medium)
                    {
                        W.Cast(prediction.CastPosition, true);
                    }
                    break;
                //on map
                case 1:
                    var prediction2 = GetPCircle(qpos.Position, W, target, true);

                    if (W.IsReady() && target.Distance(qpos.Position) <= W.Width && prediction2.CastPosition.Distance(qpos.Position) <= W.Width && prediction2.Hitchance >= HitChance.Medium)
                    {
                        W.Cast(prediction2.CastPosition, true);
                    }
                    break;
                //on ally
                case 2:
                    var prediction3 = GetPCircle(qpos.Position, W, target, true);

                    if (W.IsReady() && target.Distance(qpos.Position) <= W.Width && prediction3.CastPosition.Distance(qpos.Position) <= W.Width && prediction3.Hitchance >= HitChance.Medium)
                    {
                        W.Cast(prediction3.CastPosition, true);
                    }
                    break;
            }
        }

        public static void castR(Obj_AI_Base target)
        {
            if (IsBallMoving) return;

            switch (ballStatus)
            {
                //on self
                case 0:
                    var prediction = GetPCircle(Player.ServerPosition, R, target, true);

                    if (R.IsReady() && target.Distance(Player.ServerPosition) <= R.Width && prediction.CastPosition.Distance(Player.ServerPosition) <= R.Width && prediction.Hitchance >= HitChance.High)
                    {
                        R.Cast(prediction.CastPosition, true);
                    }
                    break;
                //on map
                case 1:
                    var prediction2 = GetPCircle(qpos.Position, R, target, true);

                    if (R.IsReady() && target.Distance(qpos.Position) <= R.Width && prediction2.CastPosition.Distance(qpos.Position) <= R.Width && prediction2.Hitchance >= HitChance.High)
                    {
                        R.Cast(prediction2.CastPosition, true);
                    }
                    break;
                //on ally
                case 2:
                    var prediction3 = GetPCircle(qpos.Position, R, target, true);

                    if (R.IsReady() && target.Distance(qpos.Position) <= R.Width && prediction3.CastPosition.Distance(qpos.Position) <= R.Width && prediction3.Hitchance >= HitChance.High)
                    {
                        R.Cast(prediction3.CastPosition, true);
                    }
                    break;
            }
        }

        public static void castE(Obj_AI_Base target)
        {
            if (IsBallMoving) return;

            //hp sheild
            var hp = menu.Item("autoE").GetValue<Slider>().Value;
            var hpPercent = Player.Health / Player.MaxHealth * 100;

            if (hpPercent <= hp)
            {
                E.CastOnUnit(Player, true);
                return;
            }


            var etarget = Player;

            switch (ballStatus)
            {

                case 0:
                    if (target != null)
                    {
                        var TravelTime = target.Distance(Player.ServerPosition) / Q.Speed;
                        var MinTravelTime = 10000f;

                        foreach (var ally in ObjectManager.Get<Obj_AI_Hero>())
                        {
                            if (!ally.IsMe && ally.IsAlly && Player.Distance(ally.ServerPosition) <= E.Range && ally != null)
                            {
                                var allyRange = target.Distance(ally.ServerPosition) / Q.Speed + ally.Distance(Player.ServerPosition) / E.Speed;
                                if (allyRange < MinTravelTime)
                                {
                                    etarget = ally;
                                    MinTravelTime = allyRange;
                                }
                            }
                        }

                        if (MinTravelTime < TravelTime && Player.Distance(etarget.ServerPosition) <= E.Range)
                        {
                            E.CastOnUnit(etarget, true);
                            return;
                        }
                    }
                    break;
                case 1:
                    if (qpos != null)
                    {
                        var TravelTime2 = target.Distance(qpos.Position) / Q.Speed;
                        var MinTravelTime2 = target.Distance(Player.ServerPosition) / Q.Speed + Player.Distance(qpos.Position) / E.Speed;

                         if (MinTravelTime2 < TravelTime2 && target.Distance(Player.ServerPosition) <= Q.Range + Q.Width)
                        {
                            E.CastOnUnit(Player, true);
                        }
                    }
                    break;
                case 2:
                    if (qpos != null)
                    {
                        var TravelTime3 = target.Distance(qpos.Position) / Q.Speed;
                        var MinTravelTime3 = 10000f;

                        foreach (var ally in ObjectManager.Get<Obj_AI_Hero>())
                        {

                            if (ally.IsMe && ally.IsAlly && Player.Distance(ally.ServerPosition) <= E.Range && ally != null)
                            {
                                var allyRange2 = target.Distance(ally.ServerPosition) / Q.Speed + ally.Distance(qpos.Position) / E.Speed;

                                if (allyRange2 < MinTravelTime3)
                                {
                                    etarget = ally;
                                    MinTravelTime3 = allyRange2;
                                }
                            }
                        }

                        if (MinTravelTime3 < TravelTime3 && Player.Distance(etarget.ServerPosition) <= E.Range)
                        {
                            E.CastOnUnit(etarget, true);
                            return;
                        }
                    }
                    break;
            }
        }

        public static void castQ(Obj_AI_Base target, String Source)
        {
            if (IsBallMoving) return;

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

            switch (ballStatus)
            {
                //on self
                case 0:
                    if (Q.IsReady() && Q.GetPrediction(target).Hitchance >= hitC && Player.Distance(target) <= Q.Range + Q.Width)
                    {
                        Q.Cast(target, true);
                    }
                    break;
                //on map
                case 1:
                    if (qpos != null)
                    {
                        var prediction = GetP(qpos.Position, Q, target, true);

                        if (Q.IsReady() && prediction.Hitchance >= hitC && Player.Distance(target) <= Q.Range + Q.Width)
                        {
                            Q.Cast(prediction.CastPosition, true);
                        }
                    }
                    else
                    {
                        ballStatus = 0;
                    }
                    break;
                //on ally
                case 2:
                    if (qpos != null)
                    {
                        var prediction2 = GetP(qpos.Position, Q, target, true);

                        if (Q.IsReady() && prediction2.Hitchance >= hitC && Player.Distance(target) <= Q.Range + Q.Width)
                        {
                            Q.Cast(prediction2.CastPosition, true);
                        }
                    }
                    else
                    {
                        ballStatus = 0;
                    }
                    break;
            }
        }

        public static void checkWMec()
        {
            int hit = 0;
            var minHit = menu.Item("autoW").GetValue<Slider>().Value;

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                if (enemy != null && !enemy.IsDead)
                {
                    if (ballStatus == 0)
                    {
                        var prediction = GetPCircle(Player.ServerPosition, W, enemy, true);

                        if (W.IsReady() && enemy.Distance(Player.ServerPosition) <= W.Width && prediction.CastPosition.Distance(Player.ServerPosition) <= W.Width && prediction.Hitchance >= HitChance.Medium)
                        {
                            hit++;
                        }
                    }
                    else if (ballStatus == 1 || ballStatus == 2)
                    {
                        var prediction2 = GetPCircle(qpos.Position, W, enemy, true);

                        if (W.IsReady() && enemy.Distance(qpos.Position) <= W.Width && prediction2.CastPosition.Distance(qpos.Position) <= W.Width && prediction2.Hitchance >= HitChance.Medium)
                        {
                            hit++;
                        }
                    }
                }
            }

                    
                    
            if (hit >= minHit)
                W.Cast();
        }

        public static void checkRMec()
        {
            int hit = 0;
            var minHit = menu.Item("autoR").GetValue<Slider>().Value;
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                if (enemy != null && !enemy.IsDead)
                {
                    if (ballStatus == 0)
                    {
                        var prediction = GetPCircle(Player.ServerPosition, R, enemy, true);

                        if (R.IsReady() && enemy.Distance(Player.ServerPosition) <= R.Width && prediction.CastPosition.Distance(Player.ServerPosition) <= R.Width && prediction.Hitchance >= HitChance.Medium)
                        {
                            hit++;
                        }
                    }
                    else if (ballStatus == 1 || ballStatus == 2)
                    {
                        var prediction2 = GetPCircle(qpos.Position, R, enemy, true);

                        if(R.IsReady() && enemy.Distance(qpos.Position) <= R.Width && prediction2.CastPosition.Distance(qpos.Position) <= R.Width && prediction2.Hitchance >= HitChance.Medium)
                        {
                            hit++;
                        }
                    }
                }
            }

            if (hit >= minHit)
                R.Cast();
        }

        public static int countR()
        {
            int hit = 0;
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                if (enemy != null && !enemy.IsDead)
                {
                    if (ballStatus == 0)
                    {
                        var prediction = GetPCircle(Player.ServerPosition, R, enemy, true);

                        if (R.IsReady() && enemy.Distance(Player.ServerPosition) <= R.Width && prediction.CastPosition.Distance(Player.ServerPosition) <= R.Width && prediction.Hitchance >= HitChance.Medium)
                        {
                            hit++;
                        }
                    }
                    else if (ballStatus == 1 || ballStatus == 2)
                    {
                        var prediction2 = GetPCircle(qpos.Position, R, enemy, true);

                        if (R.IsReady() && enemy.Distance(qpos.Position) <= R.Width && prediction2.CastPosition.Distance(qpos.Position) <= R.Width && prediction2.Hitchance >= HitChance.Medium)
                        {
                            hit++;
                        }
                    }
                }
            }

            return hit;
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
                        if (ballStatus == 0)
                        {
                            var qPos = Q.GetLineFarmLocation(allMinions);

                            if (qPos.MinionsHit >= 2)
                            Q.Cast(qPos.Position, true);
                        }
                        else if (ballStatus == 1 || ballStatus == 2)
                        {
                            var prediction = GetP(qpos.Position, Q, minion, true);

                            if(prediction.Hitchance >= HitChance.High)
                            Q.Cast(prediction.CastPosition, true);
                        }
                    }
                }
            }
        }

        private static void Farm()
        {
            if (!Orbwalking.CanMove(40)) return;

            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All);
            var allMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useW = menu.Item("UseWFarm").GetValue<bool>();
            var min = menu.Item("qFarm").GetValue<Slider>().Value;

            int hit = 0;

            if (useQ && Q.IsReady())
            {
                foreach (var enemy in allMinionsW)
                {
                    if (ballStatus == 0)
                    {
                        var prediction = GetP(Player.ServerPosition, Q, enemy, true);

                        if (Q.IsReady() && Player.Distance(enemy) <= Q.Range)
                        {
                            foreach (var enemy2 in allMinionsW)
                                {
                                    if (enemy2.Distance(prediction.CastPosition) < Q.Width)
                                        hit++;
                                }

                                if (hit >= min)
                                {
                                    if (prediction.Hitchance >= HitChance.High)
                                        Q.Cast(prediction.CastPosition, true);

                                }
                        }
                    }
                    else if (ballStatus == 1 || ballStatus == 2)
                    {
                        var prediction = GetP(qpos.Position, Q, enemy, true);

                        if (Q.IsReady() && Player.Distance(enemy) <= Q.Range)
                        {
                            foreach (var enemy2 in allMinionsW)
                            {
                                if (enemy2.Distance(prediction.CastPosition) < Q.Width)
                                    hit++;
                                    
                            }
                            if (hit >= min)
                            {
                                if (prediction.Hitchance >= HitChance.High)
                                Q.Cast(prediction.CastPosition, true);
                            }
                        }
                    }
                }
            }

            hit = 0;
            if (useW && W.IsReady())
            {
                foreach (var enemy in allMinionsW)
                {
                    if (ballStatus == 0)
                    {
                        if (enemy.Distance(Player.ServerPosition) < W.Range)
                            hit++;
                    }
                    else if (ballStatus == 1 || ballStatus == 2)
                    {
                        if (enemy.Distance(qpos.Position) < W.Range)
                            hit++;
                    }
                }

                if (hit >= min)
                    W.Cast();
            }
        }

        private static void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(),
                menu.Item("UseEHarass").GetValue<bool>(), false, "Harass");
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

        public static void onGainBuff(){
            if (Player.HasBuff("OrianaGhostSelf")){
                ballStatus = 0;
                IsBallMoving = false;
                return;
            }
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            SpellSlot castedSlot = ObjectManager.Player.GetSpellSlot(args.SData.Name, false);

            if (!unit.IsMe) return;

            if (castedSlot == SpellSlot.Q)
            {
                IsBallMoving = true;
                Utility.DelayAction.Add((int)Math.Max(1, 1000 * (args.End.Distance(CurrentBallPosition) - Game.Ping - 0.1) / Q.Speed), () =>
                {
                    CurrentBallPosition = args.End;
                    ballStatus = 1;
                    IsBallMoving = false;
                });
            }

            if (castedSlot == SpellSlot.E)
            {
                if (!args.Target.IsMe && args.Target.IsAlly)
                {
                    IsBallMoving = true;
                    ballStatus = 2;
                }
                if (args.Target.IsMe && CurrentBallPosition != ObjectManager.Player.ServerPosition)
                {
                    IsBallMoving = true;
                    ballStatus = 0;
                }
            }

        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!menu.Item("UseInt").GetValue<bool>()) return;

            if (Player.Distance(unit) < R.Range && unit != null)
            {
                //R.CastOnUnit(unit);
            }
        }

        private static void OnCreate(GameObject obj, EventArgs args)
        {
            //if(Player.Distance(obj.Position) < 300)
                //Game.PrintChat("OBJ: " + obj.Name);
            if (Player.Distance(obj.Position) < 1000)
            {
                if (obj != null && obj.IsValid && obj.Name.Contains("yomu_ring_"))
                {
                    qpos = obj;
                    IsBallMoving = false;
                    ballStatus = 1;
                    return;
                }
                if (obj != null && obj.IsValid && obj.Name.Contains("Orianna_Ball_Flash_"))
                {
                    qpos = obj;
                    ballStatus = 0;
                    IsBallMoving = false;
                    return;
                }
                if (obj != null && obj.IsValid && obj.Name.Contains("OriannaEAlly"))
                {
                    //Game.PrintChat("onALYY woot");
                    qpos = obj;
                    ballStatus = 2;
                    IsBallMoving = false;
                    return;
                }
            }
        }

        private static void OnDelete(GameObject obj, EventArgs args)
        {
            //if (Player.Distance(obj.Position) < 300)
                //Game.PrintChat("OBJ2: " + obj.Name);

            if (Player.Distance(obj.Position) < 1000)
            {
                if (obj != null && obj.IsValid && obj.Name.Contains("yomu_ring_"))
                {
                    qpos = null;
                    IsBallMoving = false;
                    ballStatus = 0;
                    return;
                }
                if (obj != null && obj.IsValid && obj.Name.Contains("Orianna_Ball_Flash_"))
                {
                    qpos = null;
                    ballStatus = 0;
                    IsBallMoving = false;
                    return;
                }
                if (obj != null && obj.IsValid && obj.Name.Contains("OriannaEAlly"))
                {
                    qpos = null;
                    ballStatus = 0;
                    IsBallMoving = false;
                }
            }
        }

        private static void Game_OnSendPacket(GamePacketEventArgs args)
        {

            if (args.PacketData[0] == Packet.C2S.Cast.Header)
            {
                var decodedPacket = Packet.C2S.Cast.Decoded(args.PacketData);
                if (decodedPacket.Slot == SpellSlot.R)
                {
                    if (countR() == 0 && menu.Item("blockR").GetValue<bool>())
                    {
                        //Block packet if enemies hit is 0
                        args.Process = false;
                    }
                    
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            onGainBuff();

            Orbwalker.SetAttacks(true);

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                checkRMec();
                checkWMec();
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

    }
}
