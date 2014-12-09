using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace xSaliceReligionAIO.Champions
{
    class Lucian : Champion
    {
        public Lucian()
        {
            LoadSpells();
            LoadMenu();
        }

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 675);
            Q.SetTargetted(0.25f, float.MaxValue);

            QExtend = new Spell(SpellSlot.Q, 1100);
            QExtend.SetSkillshot(0.25f, 5f, float.MaxValue, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 1000);
            W.SetSkillshot(0.3f, 80, 1600, true, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 425);
            E.SetSkillshot(.25f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 1400);
            R.SetSkillshot(.1f, 110, 2800, true, SkillshotType.SkillshotLine);
        }

        private void LoadMenu()
        {
            //key
            var key = new Menu("Key", "Key");
            {
                key.AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                //add to menu
                menu.AddSubMenu(key);
            }

            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                combo.AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
                combo.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                combo.AddItem(new MenuItem("Botrk", "Use BOTRK/Bilge").SetValue(true));
                //add to menu
                menu.AddSubMenu(combo);
            }

            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
                harass.AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
                harass.AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
                AddManaManagertoMenu(harass, "Harass", 30);
                //add to menu
                menu.AddSubMenu(harass);
            }

            var farm = new Menu("LaneClear", "LaneClear");
            {
                farm.AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
                farm.AddItem(new MenuItem("UseWFarm", "Use W").SetValue(true));
                AddManaManagertoMenu(farm, "LaneClear", 30);
                //add to menu
                menu.AddSubMenu(farm);
            }

            var misc = new Menu("Misc", "Misc");
            {
                misc.AddItem(new MenuItem("CheckPassive", "Smart Passive").SetValue(true));
                misc.AddItem(new MenuItem("smartKS", "Use Smart KS System").SetValue(true));
                misc.AddItem(new MenuItem("E_If_HP", "Do not E If HP <=").SetValue(new Slider(20)));
                //add to menu
                menu.AddSubMenu(misc);
            }

            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
                drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R_Killable", "Draw R Mark on Killable").SetValue(true));

                MenuItem drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage").SetValue(true);
                MenuItem drawFill = new MenuItem("Draw_Fill", "Draw Combo Damage Fill").SetValue(new Circle(true, Color.FromArgb(90, 255, 169, 4)));
                drawMenu.AddItem(drawComboDamageMenu);
                drawMenu.AddItem(drawFill);
                DamageIndicator.DamageToUnit = GetComboDamage;
                DamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
                DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
                DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;
                drawComboDamageMenu.ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs eventArgs)
                    {
                        DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                    };
                drawFill.ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs eventArgs)
                    {
                        DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
                        DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
                    };
                //add to menu
                menu.AddSubMenu(drawMenu);
            }
        }

        private float GetComboDamage(Obj_AI_Base target)
        {
            double comboDamage = 0;

            if (Q.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.Q) * 2;

            if (W.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.W);

            if (R.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.R) * GetShots();

            if (Items.CanUseItem(Bilge.Id))
                comboDamage += Player.GetItemDamage(target, Damage.DamageItems.Bilgewater);

            if (Items.CanUseItem(Botrk.Id))
                comboDamage += Player.GetItemDamage(target, Damage.DamageItems.Botrk);

            return (float)(comboDamage + Player.GetAutoAttackDamage(target) * 2);
        }

        private double GetShots()
        {
            double shots = 0;

            if (R.Level == 1)
                shots = 7.5 + 7.5 * (Player.AttackSpeedMod - .6);
            if (R.Level == 2)
                shots = 7.5 + 9 * (Player.AttackSpeedMod - .6);
            if(R.Level == 3 )
                shots = 7.5 + 10.5 * (Player.AttackSpeedMod - .6);

            return shots/1.4;
        }
        private void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(), menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>(), "Combo");
        }

        private void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(), menu.Item("UseEHarass").GetValue<bool>(), false, "Harass");
        }

        private void UseSpells(bool useQ, bool useW, bool useE, bool useR, string source)
        {
            if (source == "Harass" && !HasMana("Harass"))
                return;

            var target = SimpleTs.GetTarget(450, SimpleTs.DamageType.Physical);

            if (target != null)
            {
                if (menu.Item("Botrk").GetValue<bool>())
                {
                    if ((GetComboDamage(target) > target.Health || GetHealthPercent(Player) < 25) &&
                        !target.HasBuffOfType(BuffType.Slow))
                    {
                        Use_Bilge(target);
                        Use_Botrk(target);
                    }
                }
            }

            if(useQ)
                Cast_Q();
            if(useW)
                Cast_W();
            if(useE)
                Cast_E();
            if(useR)
                Cast_R();
        }

        private void Cast_Q(Obj_AI_Hero forceTarget = null)
        {
            if (!Q.IsReady() || !PassiveCheck())
                return;

            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (forceTarget != null)
                target = forceTarget;

            if (target != null && target.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(target, packets());
                Utility.DelayAction.Add(25, Orbwalking.ResetAutoAttackTimer);
                Utility.DelayAction.Add(25, xSLxOrbwalker.ResetAutoAttackTimer);
                Utility.DelayAction.Add(50, () => Player.IssueOrder(GameObjectOrder.AttackTo, target.ServerPosition));
                return;
            }

            target = SimpleTs.GetTarget(QExtend.Range, SimpleTs.DamageType.Physical);

            if (forceTarget != null)
                target = forceTarget;

            if (target == null)
                return;

            var pred = QExtend.GetPrediction(target, true);
            var collisions = pred.CollisionObjects;

            if (collisions.Count == 0)
                return;

            Q.CastOnUnit(collisions[0], packets());
        }

        private void Cast_W()
        {
            if (!W.IsReady() || !PassiveCheck())
                return;

            CastBasicSkillShot(W, W.Range, SimpleTs.DamageType.Magical, HitChance.Medium);
        }

        private void Cast_E()
        {
            if (!E.IsReady() || !PassiveCheck())
                return;

            var target = SimpleTs.GetTarget(E.Range + Player.AttackRange, SimpleTs.DamageType.Physical);

            if (target == null)
                return;

            Vector3 vec = Player.ServerPosition + Vector3.Normalize(Game.CursorPos - Player.ServerPosition) * E.Range;

            if (Player.Distance(Game.CursorPos) < E.Range & Player.Distance(Game.CursorPos) > 150)
                vec = Game.CursorPos;

            if (countEnemiesNearPosition(vec, 500) >= 3 && countAlliesNearPosition(vec, 400) < 3)
                return;

            if (GetHealthPercent(Player) <= menu.Item("E_If_HP").GetValue<Slider>().Value)
                return;

            if (vec.Distance(target.ServerPosition) < Player.AttackRange)
            {
                E.Cast(vec, packets());
                Utility.DelayAction.Add(50, () => Player.IssueOrder(GameObjectOrder.AttackTo, target.ServerPosition));
            }
        }

        private void Cast_R()
        {
            if (!R.IsReady())
                return;

            var target = SimpleTs.GetTarget(E.Range + Player.AttackRange, SimpleTs.DamageType.Physical);

            if (target == null)
                return;

            if (Player.GetSpellDamage(target, SpellSlot.R) * GetShots() > target.Health)
                R.Cast(target);
        }

        private bool PassiveCheck()
        {
            if (!menu.Item("CheckPassive").GetValue<bool>())
                return true;

            if (Environment.TickCount - Q.LastCastAttemptT < 250)
                return false;

            if (Environment.TickCount - W.LastCastAttemptT < 250)
                return false;

            if (Environment.TickCount - E.LastCastAttemptT < 250)
                return false;

            if (Player.HasBuff("LucianPassiveBuff"))
                return false;
                
            return true;
        }

        private void SmartKs()
        {
            if (!menu.Item("smartKS").GetValue<bool>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(QExtend.Range) && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability)))
            {
                //Q
                if (Q.IsKillable(target) && Player.Distance(target) < QExtend.Range && Q.IsReady())
                {
                    Cast_Q(target);
                }
                //E
                if (W.IsKillable(target) && Player.Distance(target) < W.Range && W.IsReady())
                {
                    W.Cast(target);
                }
            }
        }

        private void Farm()
        {
            if (!HasMana("LaneClear"))
                return;

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useW = menu.Item("UseWFarm").GetValue<bool>();

            if (useQ)
            {
                var allMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
                var minion = allMinions.FirstOrDefault(minionn => minionn.Distance(Player) <= Q.Range && HealthPrediction.LaneClearHealthPrediction(minionn, 500) > 0);
                if (minion == null)
                    return;

                Q.CastOnUnit(minion);
            }
            if (useW)
            {
                var allMinionE= MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.NotAlly);

                if (allMinionE.Count > 1)
                {
                    var pred = W.GetCircularFarmLocation(allMinionE);

                    W.Cast(pred.Position);
                }
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            if (Player.IsChannelingImportantSpell())
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                return;
            }

            SmartKs();

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

        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (!unit.IsMe)
                return;

            SpellSlot castedSlot = ObjectManager.Player.GetSpellSlot(args.SData.Name, false);

            if (castedSlot == SpellSlot.Q)
            {
                Q.LastCastAttemptT = Environment.TickCount;
            }
            if (castedSlot == SpellSlot.W)
            {
                W.LastCastAttemptT = Environment.TickCount;
            }
            if (castedSlot == SpellSlot.E)
            {
                E.LastCastAttemptT = Environment.TickCount;
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            if (menu.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (menu.Item("Draw_Q").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_W").GetValue<bool>())
                if (W.Level > 0)
                    Utility.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_R").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
        }
    }
}
