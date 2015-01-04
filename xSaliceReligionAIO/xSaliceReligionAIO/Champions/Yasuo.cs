using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace xSaliceReligionAIO.Champions
{
    class Yasuo : Champion
    {
        public Yasuo()
        {
            SetSpells();
            LoadMenu();
        }

        private void SetSpells()
        {
            Q = new Spell(SpellSlot.Q, 475);
            Q.SetSkillshot(0.25f, 120f, float.MaxValue, false, SkillshotType.SkillshotLine);
            
            Q2 = new Spell(SpellSlot.Q, 900);
            Q2.SetSkillshot(0.25f, 120f, 1200f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 400);

            E = new Spell(SpellSlot.E, 475);

            R = new Spell(SpellSlot.R, 1200);
        }

        private void LoadMenu()
        {
            var key = new Menu("Key", "Key");
            {
                key.AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));
                key.AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("LastHitQ", "Last hit with Q!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
                //add to menu
                menu.AddSubMenu(key);
            }

            var spellMenu = new Menu("SpellMenu", "SpellMenu");
            {
                var qMenu = new Menu("QMenu", "QMenu");
                {
                    qMenu.AddItem(new MenuItem("Q_Auto", "Auto Q Toggle")).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle));
                    qMenu.AddItem(new MenuItem("Q_Auto_third", "Use 3rd Q in Auto Q")).SetValue(true);
                    qMenu.AddItem(new MenuItem("Q_UnderTower", "Auto Q under Tower")).SetValue(false);
                    qMenu.AddItem(new MenuItem("Q_Stack", "Auto 3rd Q stack Toggle")).SetValue(new KeyBind("I".ToCharArray()[0], KeyBindType.Toggle));
                    qMenu.AddItem(new MenuItem("Q_thirdE", "Priortize E->3rd Q over Single Q")).SetValue(true);
                    spellMenu.AddSubMenu(qMenu);
                }

                var wMenu = new Menu("WMenu", "WMenu");
                {
                    //wind wall
                    var dangerous = new Menu("Dodge Dangerous", "Dodge Dangerous");
                    {
                        foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy))
                        {
                            dangerous.AddSubMenu(new Menu(hero.ChampionName, hero.ChampionName));
                            dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.Q).Name + "W_Wall", hero.Spellbook.GetSpell(SpellSlot.Q).Name).SetValue(false));
                            dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.W).Name + "W_Wall", hero.Spellbook.GetSpell(SpellSlot.W).Name).SetValue(false));
                            dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.E).Name + "W_Wall", hero.Spellbook.GetSpell(SpellSlot.E).Name).SetValue(false));
                            dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.R).Name + "W_Wall", hero.Spellbook.GetSpell(SpellSlot.R).Name).SetValue(false));
                        }
                        wMenu.AddSubMenu(dangerous);
                    }
                    spellMenu.AddSubMenu(wMenu);
                }

                var eMenu = new Menu("EMenu", "EMenu");
                {
                    eMenu.AddItem(new MenuItem("E_Min_Dist", "Min Distance to use E").SetValue(new Slider(250, 0, 475)));
                    eMenu.AddItem(new MenuItem("E_Into_Enemy", "Do not E if there are >= Enemies").SetValue(new Slider(3, 1, 5)));
                    eMenu.AddItem(new MenuItem("E_GapClose", "Use E to gapclose").SetValue(true));
                    spellMenu.AddSubMenu(eMenu);
                }

                var rMenu = new Menu("RMenu", "RMenu");
                {
                    rMenu.AddItem(new MenuItem("R_If_Killable", "R If Enemy Is killable").SetValue(true));
                    rMenu.AddItem(new MenuItem("R_MEC", "Auto R if >= Enemies, 0 = off")).SetValue(new Slider(3, 0, 5));
                    spellMenu.AddSubMenu(rMenu);
                }
                //add to menu
                menu.AddSubMenu(spellMenu);
            }

            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("selected", "Focus Selected Target").SetValue(true));
                combo.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                combo.AddItem(new MenuItem("qHit", "Q3 HitChance").SetValue(new Slider(3, 1, 3)));
                combo.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                combo.AddItem(new MenuItem("ComboR_MEC", "R if >= Enemies")).SetValue(new Slider(3, 1, 5));
                //add to menu
                menu.AddSubMenu(combo);
            }
            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
                harass.AddItem(new MenuItem("qHit2", "Q3 HitChance").SetValue(new Slider(3, 1, 3)));
                harass.AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
                //add to menu
                menu.AddSubMenu(harass);
            }
            var farm = new Menu("LaneClear", "LaneClear");
            {
                farm.AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
                farm.AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
                farm.AddItem(new MenuItem("LaneClear_useQ_minHit", "Use Q if min. hit").SetValue(new Slider(2, 1, 6)));
                //add to menu
                menu.AddSubMenu(farm);
            }
            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
                drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_Q2", "Draw Q Extended").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));

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
                comboDamage += Player.GetSpellDamage(target, SpellSlot.Q)*2;

            if (E.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.E) * 1.5;

            if (R.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.R);

            comboDamage = ActiveItems.CalcDamage(target, comboDamage);

            return (float)(comboDamage + Player.GetAutoAttackDamage(target) * 3);
        }

        private void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), false,
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>(), "Combo");
        }

        private void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), false,
                menu.Item("UseEHarass").GetValue<bool>(), false, "Harass");
        }

        private void UseSpells(bool useQ, bool useW, bool useE, bool useR, string source)
        {
            var itemTarget = TargetSelector.GetTarget(750, TargetSelector.DamageType.Physical);
            var dmg = GetComboDamage(itemTarget);

            if (useE)
                Cast_E();

            if(useQ)
                Cast_Q(source);

            if(useR)
                Cast_R();

            //items
            if (source == "Combo")
            {
                if (itemTarget != null)
                {
                    ActiveItems.Target = itemTarget;

                    //see if killable
                    if (dmg > itemTarget.Health - 50)
                        ActiveItems.KillableTarget = true;

                    ActiveItems.UseTargetted = true;
                }
            }
        }

        private void Cast_Q(string source)
        {
            if (!Q.IsReady() || Environment.TickCount - E.LastCastAttemptT < 250)
                return;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
           
            if(target != null)
                if (menu.Item("Q_thirdE").GetValue<bool>() && E.IsReady() && CanCastE(target) && Player.Distance(target) < E.Range)
                    return;

            if (!ThirdQ() && target != null)
            {
                Q.Cast(target.Position, packets());
            }
            else
            {
                CastBasicSkillShot(Q2, Q2.Range, TargetSelector.DamageType.Physical, GetHitchance(source));
            }
        }

        private void Cast_E()
        {
            var target = TargetSelector.GetTarget(E.Range*2, TargetSelector.DamageType.Physical);

            if (target == null || !E.IsReady() || !CanCastE(target))
                return;

            if (E.IsKillable(target) && Player.Distance(target) < E.Range + target.BoundingRadius)
                E.Cast(target, packets());

            //EQ3
            if (ThirdQ() && Player.ServerPosition.To2D().Distance(target.ServerPosition.To2D()) < E.Range + target.BoundingRadius)
            {
                E.Cast(target);
                Utility.DelayAction.Add(200, () => Q.Cast(target.Position, packets()));
                return;
            }

            if (Player.ServerPosition.To2D().Distance(target.ServerPosition.To2D()) <= menu.Item("E_Min_Dist").GetValue<Slider>().Value)
                return;
            
            //gapclose
            var allMinionQ = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);

            Obj_AI_Base bestMinion = allMinionQ[0];
            Vector3 bestVec = Player.ServerPosition + Vector3.Normalize(bestMinion.ServerPosition - Player.ServerPosition)*475;

            foreach (var minion in allMinionQ.Where(CanCastE))
            {
                var dashVec = Player.ServerPosition + Vector3.Normalize(minion.ServerPosition - Player.ServerPosition)*475;

                if (Player.Distance(target) > target.Distance(dashVec) && target.Distance(bestVec) > target.Distance(dashVec))
                {
                    bestMinion = minion;
                    bestVec = dashVec;
                }
            }

            if (Player.Distance(target) > target.Distance(bestVec) && bestMinion != null)
            {
                E.Cast(bestMinion, packets());
                return;
            }

            if (Q.IsReady() && Player.Distance(target) > menu.Item("E_Min_Dist").GetValue<Slider>().Value &&
                Player.Distance(target) < E.Range)
            {
                E.Cast(target, packets());
                Utility.DelayAction.Add(200, () => Q.Cast(target.Position, packets()));
                return;
            }

            if (Player.ServerPosition.To2D().Distance(target.ServerPosition.To2D()) > menu.Item("E_Min_Dist").GetValue<Slider>().Value && Player.Distance(target) < E.Range + target.BoundingRadius)
                E.Cast(target, packets());
        }

        private void Cast_R()
        {
            int hit = 0;
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(x => Player.IsValidTarget(R.Range) && (x.HasBuff("yasuoq3mis") || x.HasBuffOfType(BuffType.Knockup) || x.HasBuffOfType(BuffType.Knockback))))
            {
                hit = 1;
                if (GetComboDamage(target) > target.Health && menu.Item("R_If_Killable").GetValue<bool>())
                    R.Cast(packets());

                hit += ObjectManager.Get<Obj_AI_Hero>().Count(x => target.Distance(x) < 400 && (x.HasBuffOfType(BuffType.Knockup) || x.HasBuffOfType(BuffType.Knockback)));
            }

            if (hit >= menu.Item("ComboR_MEC").GetValue<Slider>().Value)
                R.Cast();
        }

        private void Cast_MecR()
        {
            if (menu.Item("R_MEC").GetValue<Slider>().Value == 0)
                return;

            int hit = 1 + ObjectManager.Get<Obj_AI_Hero>().Where(x => Player.IsValidTarget(R.Range) && (x.HasBuffOfType(BuffType.Knockup) || x.HasBuffOfType(BuffType.Knockback))).Sum(target => ObjectManager.Get<Obj_AI_Hero>().Count(x => target.Distance(x) < 400 && (x.HasBuffOfType(BuffType.Knockup) || x.HasBuffOfType(BuffType.Knockback))));

            if (hit >= menu.Item("R_MEC").GetValue<Slider>().Value)
                R.Cast();
        }

        private void AutoQ()
        {
            if (!Q.IsReady() || !menu.Item("Q_Auto").GetValue<KeyBind>().Active || Environment.TickCount - E.LastCastAttemptT < 250 + Game.Ping)
                return;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (!ThirdQ() && target != null)
            {
                Q.Cast(target.Position, packets());
            }
            else if (menu.Item("Q_Auto_third").GetValue<bool>())
            {
                CastBasicSkillShot(Q2, Q2.Range, TargetSelector.DamageType.Physical, GetHitchance("Harass"), menu.Item("Q_UnderTower").GetValue<bool>());
            }
        }

        private void StackQ()
        {
            if (!Q.IsReady() || !menu.Item("Q_Stack").GetValue<KeyBind>().Active || ThirdQ())
                return;

            var minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            var enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Q.Range) && !x.UnderTurret(true)).ToList();

            if (minions.Count > 0)
                Q.Cast(minions[0], packets());

            if(enemies.Count > 0)
                Q.Cast(enemies[0], packets());
        }

        private void LastHit()
        {
            
        }

        private void Farm()
        {

        }

        private int CheckEnemieKnockedUp()
        {
            int knocked = 0;

            return knocked;
        }

        private bool ThirdQ()
        {
            return Player.HasBuff("YasuoQ3W");
        }

        private bool CanCastE(Obj_AI_Base target)
        {
            return !target.HasBuff("YasuoDashWrapper");
        }

        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (!unit.IsMe)
                return;

            SpellSlot castedSlot = ObjectManager.Player.GetSpellSlot(args.SData.Name, false);

            if (castedSlot == SpellSlot.E)
            {
                E.LastCastAttemptT = Environment.TickCount;
            }
        }
        public override void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            //auto Q harass
            AutoQ();

            //rmec
            Cast_MecR();

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    Farm();

                if (menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();
            }

            //stack Q
            StackQ();
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            if (menu.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (menu.Item("Draw_Q").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_Q2").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(Player.Position, Q2.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_R").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
        }

    }
}
