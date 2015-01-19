using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace xSaliceReligionAIO.Champions
{
    class Fizz : Champion
    {
        public Fizz()
        {
            LoadSpells();
            LoadMenu();
        }

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 400);
            E2 = new Spell(SpellSlot.E, 400);
            R = new Spell(SpellSlot.R, 1300);

            E.SetSkillshot(0.5f, 270f, 1300, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 120f, 1350f, false, SkillshotType.SkillshotLine);
        }

        private void LoadMenu()
        {
            var key = new Menu("Key", "Key");
            {
                key.AddItem(new MenuItem("ComboActive", "Combo!", true).SetValue(new KeyBind(32, KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActive", "Harass!", true).SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!", true).SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));
                key.AddItem(new MenuItem("LaneClearActive", "Farm!", true).SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("Flee", "Escape with E", true).SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
                //add to menu
                menu.AddSubMenu(key);
            }

            var spellMenu = new Menu("SpellMenu", "SpellMenu");
            {
                var qMenu = new Menu("QMenu", "QMenu");
                {
                    qMenu.AddItem(new MenuItem("Q_Min_Dist", "Min Distance to use E", true).SetValue(new Slider(250, 1, 475)));
                    spellMenu.AddSubMenu(qMenu);
                }

                var eMenu = new Menu("EMenu", "EMenu");
                {
                    eMenu.AddItem(new MenuItem("E_Min_Dist", "Min Distance to use E", true).SetValue(new Slider(250, 1, 475)));
                    spellMenu.AddSubMenu(eMenu);
                }

                var rMenu = new Menu("RMenu", "RMenu");
                {   
                    rMenu.AddItem(new MenuItem("rBestTarget", "Shoot R to Best Target", true).SetValue(new KeyBind("R".ToCharArray()[0], KeyBindType.Press)));
                    rMenu.AddItem(new MenuItem("R_Max_Dist", "R Max Distance", true).SetValue(new Slider(1000, 200, 1300)));
                    spellMenu.AddSubMenu(rMenu);
                }
                //add to menu
                menu.AddSubMenu(spellMenu);
            }

            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("Combo_mode", "Combo Mode", true).SetValue(new StringList(new[] { "R-W-Q-E (Normal)", "W-Q-R-E(R During Q)", "R-E-W-Q (R->E gap)" }, 1)));
                combo.AddItem(new MenuItem("Combo_Switch", "Switch mode Key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                combo.AddItem(new MenuItem("UseQCombo", "Use Q", true).SetValue(true));
                combo.AddItem(new MenuItem("UseWCombo", "Use W", true).SetValue(true));
                combo.AddItem(new MenuItem("UseECombo", "Use E", true).SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R", true).SetValue(true));
                //add to menu
                menu.AddSubMenu(combo);
            }
            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q", true).SetValue(true));
                harass.AddItem(new MenuItem("UseWHarass", "Use W", true).SetValue(true));
                harass.AddItem(new MenuItem("UseEHarass", "Use E", true).SetValue(true));
                //add to menu
                menu.AddSubMenu(harass);
            }
            var farm = new Menu("Farming", "Farming");
            {
                farm.AddItem(new MenuItem("UseQFarm", "Use Q Farm", true).SetValue(true));
                farm.AddItem(new MenuItem("UseWFarm", "Use W Farm", true).SetValue(true));
                farm.AddItem(new MenuItem("UseEFarm", "Use E Farm", true).SetValue(true));
                farm.AddItem(new MenuItem("LaneClear_useE_minHit", "Use E if min. hit", true).SetValue(new Slider(2, 1, 6)));
                //add to menu
                menu.AddSubMenu(farm);
            }

            var misc = new Menu("Misc", "Misc");
            {
                misc.AddItem(new MenuItem("smartKS", "Use Smart KS System", true).SetValue(true));
                menu.AddSubMenu(misc);
            }

            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All", true).SetValue(false));
                drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q", true).SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_E", "Draw E", true).SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R", "Draw R", true).SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_Mode", "Draw Modes", true).SetValue(true));

                MenuItem drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage", true).SetValue(true);
                MenuItem drawFill = new MenuItem("Draw_Fill", "Draw Combo Damage Fill", true).SetValue(new Circle(true, Color.FromArgb(90, 255, 169, 4)));
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
            }
            //add to menu
            menu.AddSubMenu(drawMenu);
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            if (enemy == null)
                return 0;

            double damage = 0d;

            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);

            
            damage += Player.GetAutoAttackDamage(enemy)*2;

            damage = ActiveItems.CalcDamage(enemy, damage);

            return (float)damage;
        }

        private void Combo()
        {
            UseSpells(menu.Item("UseQCombo", true).GetValue<bool>(), menu.Item("UseWCombo", true).GetValue<bool>(),
                menu.Item("UseECombo", true).GetValue<bool>(), menu.Item("UseRCombo", true).GetValue<bool>(), "Combo");
        }

        private void Harass()
        {
            UseSpells(menu.Item("UseQHarass", true).GetValue<bool>(), menu.Item("UseWHarass", true).GetValue<bool>(),
                menu.Item("UseEHarass", true).GetValue<bool>(), false, "Harass");
        }

        private void UseSpells(bool useQ, bool useW, bool useE, bool useR, string source)
        {
            int mode = menu.Item("Combo_mode", true).GetValue<StringList>().SelectedIndex;
            var target = TargetSelector.GetTarget(R.IsReady() || E.IsReady() ? R.Range : Q.Range, TargetSelector.DamageType.Magical);

            if (target == null)
                return;

            var dmg = GetComboDamage(target);

            if (source == "Combo" && Q.IsInRange(target))
            {
                ActiveItems.Target = target;

                //see if killable
                if (dmg > target.Health - 50)
                    ActiveItems.KillableTarget = true;

                ActiveItems.UseTargetted = true;
            }

            switch (mode)
            {
                case 0://R-W-Q-E
                    if (useR && R.IsReady())
                    {
                        if (ShouldCastR(target, dmg))
                        {
                            if (R.GetPrediction(target).Hitchance >= HitChance.High)
                            {
                                CastR(R.GetPrediction(target).CastPosition);
                            }
                        }
                    }

                    if (!R.IsReady() || target.HasBuff("FizzMarinerDoom") || dmg < target.Health || !useR)
                    {
                        if (useW && W.IsReady())
                        {
                            if(ShouldCastW(target))
                                W.Cast(packets());
                        }

                        if (useQ && Q.IsReady())
                        {
                            if(ShouldCastQ(target))
                                Q.CastOnUnit(target, packets());
                        }

                        if (useE && E.IsReady())
                        {
                            if (ShouldCatE(target))
                            {
                                CastE(target);
                            }
                        }
                    }
                    break;

                case 1://W-Q-R-E
                    if (useW && W.IsReady())
                    {
                        if (ShouldCastW(target))
                            W.Cast(packets());
                    }

                    if (useQ && Q.IsReady())
                    {
                        if (ShouldCastQ(target))
                        {
                            Q.CastOnUnit(target, packets());

                            if (R.IsReady() && dmg > target.Health - 75)
                            { 
                                qDelay = (int)Player.Distance(target) / 2;
                                qVec = Player.ServerPosition + Vector3.Normalize(target.ServerPosition - Player.ServerPosition) * Q.Range;
                                Q.LastCastAttemptT = Environment.TickCount;
                            }
                        }
                    }
                    if (useE && E.IsReady() && !Q.IsReady())
                    {
                        if (ShouldCatE(target, true))
                        {
                            CastE(target);
                        }
                    }
                    break;
                case 2://R-E-W-Q (Gap)
                    if (useR && R.IsReady())
                    {
                        if (ShouldCastR(target, dmg))
                        {
                            if (R.GetPrediction(target).Hitchance >= HitChance.High)
                            {
                                CastR(R.GetPrediction(target).CastPosition);
                            }
                        }
                    }

                    if (!R.IsReady() || target.HasBuff("FizzMarinerDoom") || dmg < target.Health || !useR)
                    {
                        if (useE && E.IsReady())
                        {
                            if (ShouldCatE(target, true))
                            {
                                CastE(target);
                                return;
                            }
                        }

                        if (useW && W.IsReady())
                        {
                            if(ShouldCastW(target))
                                W.Cast(packets());
                        }

                        if (useQ && Q.IsReady())
                        {
                            if(ShouldCastQ(target))
                                Q.CastOnUnit(target, packets());
                        }
                    }
                    break;
            }
        }

        private void CastR(Vector3 pos)
        {
            var vec = Player.ServerPosition + Vector3.Normalize(pos - Player.ServerPosition)*1200;

            R.Cast(vec, packets());

        }
        private void CastE(Obj_AI_Hero target)
        {
            if (Player.Spellbook.GetSpell(SpellSlot.E).Name == "FizzJump")
                E.Cast(target, packets());

            if (Player.Spellbook.GetSpell(SpellSlot.E).Name == "fizzjumptwo" && Environment.TickCount - E.LastCastAttemptT > 100)
                E.Cast(target.ServerPosition, packets());
        }

        private bool ShouldCastQ(Obj_AI_Hero target)
        {
            if (Player.Distance(target) > menu.Item("Q_Min_Dist", true).GetValue<Slider>().Value && Player.Distance(target) < Q.Range)
                return true;

            return false;
        }

        private bool ShouldCastW(Obj_AI_Hero target)
        {
            if (Player.Distance(target) < Q.Range + 100 && Q.IsReady())
                return true;

            if (Player.Distance(target) < 250)
                return true;

            return false;
        }

        private bool ShouldCatE(Obj_AI_Hero target, bool gap = false)
        {
            if (Player.Spellbook.GetSpell(SpellSlot.E).Name == "fizzjumptwo")
                return true;

            if (Player.Distance(target) > menu.Item("E_Min_Dist", true).GetValue<Slider>().Value && Player.Distance(target) < E.Range)
                return true;

            if (gap && Player.Distance(target) < 1000)
                return true;

            return false;
        }

        private bool ShouldCastR(Obj_AI_Hero target, float dmg)
        {
            if (dmg > target.Health)
                return true;

            return false;
        }

        private void Farm()
        {
            
        }

        private Vector3 qVec;
        private int qDelay;
        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (unit.IsMe)
            {
                SpellSlot castedSlot = ObjectManager.Player.GetSpellSlot(args.SData.Name);

                if (castedSlot == SpellSlot.Q)
                {
                    if (R.IsReady() && Environment.TickCount - Q.LastCastAttemptT < 250)
                    {
                        var vec = qVec + Vector3.Normalize(Prediction.GetPrediction((Obj_AI_Hero)args.Target, qDelay).CastPosition - qVec) * 600;
                        R.Cast(vec);
                    }
                }
                if (castedSlot == SpellSlot.E)
                {
                    E.LastCastAttemptT = Environment.TickCount;
                }
            }
        }

        private int _lasttick;

        private void ModeSwitch()
        {
            int mode = menu.Item("Combo_mode", true).GetValue<StringList>().SelectedIndex;
            int lasttime = Environment.TickCount - _lasttick;

            if (menu.Item("Combo_Switch", true).GetValue<KeyBind>().Active && lasttime > Game.Ping)
            {
                if (mode == 0)
                {
                    menu.Item("Combo_mode", true).SetValue(new StringList(new[] { "R-W-Q-E (Normal)", "W-Q-R-E(R During Q)", "R-E-W-Q (R->E gap)" }, 1));
                    _lasttick = Environment.TickCount + 300;
                }
                else if (mode == 1)
                {
                    menu.Item("Combo_mode", true).SetValue(new StringList(new[] { "R-W-Q-E (Normal)", "W-Q-R-E(R During Q)", "R-E-W-Q (R->E gap)" }, 2));
                    _lasttick = Environment.TickCount + 300;
                }
                else
                {
                    menu.Item("Combo_mode", true).SetValue(new StringList(new[] { "R-W-Q-E (Normal)", "W-Q-R-E(R During Q)", "R-E-W-Q (R->E gap)" }));
                    _lasttick = Environment.TickCount + 300;
                }
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            ModeSwitch();

            if (menu.Item("ComboActive", true).GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("LaneClearActive", true).GetValue<KeyBind>().Active)
                    Farm();

                if (menu.Item("HarassActiveT", true).GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("HarassActive", true).GetValue<KeyBind>().Active)
                    Harass();
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            if (menu.Item("Draw_Disabled", true).GetValue<bool>())
                return;

            if (menu.Item("Draw_Q", true).GetValue<bool>())
                if (Q.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_E", true).GetValue<bool>())
                if (E.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_R", true).GetValue<bool>())
                if (R.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_Mode", true).GetValue<bool>())
            {
                Vector2 wts = Drawing.WorldToScreen(Player.Position);
                int mode = menu.Item("Combo_mode", true).GetValue<StringList>().SelectedIndex;
                if (mode == 0)
                    Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "R-W-Q-E (Normal)");
                else if (mode == 1)
                    Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "W-Q-R-E(R During Q)");
                else if (mode == 2)
                    Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "R-E-W-Q (R->E gap)");
            }
        }
    }
}
