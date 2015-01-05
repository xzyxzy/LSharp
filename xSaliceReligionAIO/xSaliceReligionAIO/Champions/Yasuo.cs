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
            Q.SetSkillshot(0.35f, 50f, float.MaxValue, false, SkillshotType.SkillshotLine);

            Q2 = new Spell(SpellSlot.Q, 900);
            Q2.SetSkillshot(0.4f, 90f, 1500f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 400);

            E = new Spell(SpellSlot.E, 475);
            E.SetSkillshot(.1f, 350f, float.MaxValue, false, SkillshotType.SkillshotCircle);

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
                key.AddItem(new MenuItem("LastHit", "Last hit").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
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
                        SpellDatabase.CreateSpellDatabase();
                        foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy))
                        {
                            dangerous.AddSubMenu(new Menu(hero.ChampionName, hero.ChampionName));

                            var q = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.Q);
                            if (q != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(q.MissileSpellName + "W_Wall", q.MissileSpellName).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.Q).Name + "W_Wall", hero.Spellbook.GetSpell(SpellSlot.Q).Name).SetValue(false));
                            
                            var w = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.W);
                            if (w != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(w.MissileSpellName + "W_Wall", w.MissileSpellName).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.W).Name + "W_Wall", hero.Spellbook.GetSpell(SpellSlot.W).Name).SetValue(false));

                            var e = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.E);
                            if (e != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(e.MissileSpellName + "W_Wall", e.MissileSpellName).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.E).Name + "W_Wall", hero.Spellbook.GetSpell(SpellSlot.E).Name).SetValue(false));

                            var r = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.R);
                            if (r != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(r.MissileSpellName + "W_Wall", r.MissileSpellName).SetValue(false));
                            else
                                 dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.R).Name + "W_Wall", hero.Spellbook.GetSpell(SpellSlot.R).Name).SetValue(false));

                        }
                        wMenu.AddSubMenu(dangerous);
                    }
                    spellMenu.AddSubMenu(wMenu);
                }

                var eMenu = new Menu("EMenu", "EMenu");
                {
                    eMenu.AddItem(new MenuItem("E_Min_Dist", "Min Distance to use E").SetValue(new Slider(250, 1, 475)));
                    //e Evade
                    var dangerous = new Menu("Dodge Spells", "Dodge Spells");
                    {
                        foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy))
                        {
                            dangerous.AddSubMenu(new Menu(hero.ChampionName, hero.ChampionName));

                            var q = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.Q);
                            if (q != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(q.MissileSpellName + "E", q.MissileSpellName).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.Q).Name + "E", hero.Spellbook.GetSpell(SpellSlot.Q).Name).SetValue(false));

                            var w = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.W);
                            if (w != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(w.MissileSpellName + "E", w.MissileSpellName).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.W).Name + "E", hero.Spellbook.GetSpell(SpellSlot.W).Name).SetValue(false));

                            var e = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.E);
                            if (e != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(e.MissileSpellName + "E", e.MissileSpellName).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.E).Name + "E", hero.Spellbook.GetSpell(SpellSlot.E).Name).SetValue(false));

                            var r = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.R);
                            if (r != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(r.MissileSpellName + "E", r.MissileSpellName).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.R).Name + "E", hero.Spellbook.GetSpell(SpellSlot.R).Name).SetValue(false));
                        }
                        eMenu.AddSubMenu(dangerous);
                    }
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
                combo.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                combo.AddItem(new MenuItem("qHit", "Q3 HitChance").SetValue(new Slider(2, 1, 3)));
                combo.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                combo.AddItem(new MenuItem("ComboR_MEC", "R if >= Enemies")).SetValue(new Slider(3, 1, 5));
                //add to menu
                menu.AddSubMenu(combo);
            }
            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
                harass.AddItem(new MenuItem("qHit2", "Q3 HitChance").SetValue(new Slider(2, 1, 3)));
                harass.AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
                //add to menu
                menu.AddSubMenu(harass);
            }
            var farm = new Menu("Farming", "Farming");
            {
                farm.AddItem(new MenuItem("UseQLast", "Use Q Last hit").SetValue(true));
                farm.AddItem(new MenuItem("UseELast", "Use E Last hit").SetValue(true));
                farm.AddItem(new MenuItem("UseQFarm", "Use Q Farm").SetValue(true));
                farm.AddItem(new MenuItem("UseQ3Farm", "Use Q3 Farm").SetValue(true));
                farm.AddItem(new MenuItem("UseEFarm", "Use E Farm").SetValue(true));
                farm.AddItem(new MenuItem("E_UnderTower_Farm", "E under Tower")).SetValue(false);
                farm.AddItem(new MenuItem("LaneClear_useQ_minHit", "Use Q if min. hit").SetValue(new Slider(2, 1, 6)));
                //add to menu
                menu.AddSubMenu(farm);
            }

            var misc = new Menu("Misc", "Misc");
            {
                misc.AddItem(new MenuItem("smartKS", "Use Smart KS System").SetValue(true));
                misc.AddItem(new MenuItem("Interrupt", "Interrupt Spells").SetValue(true));
                menu.AddSubMenu(misc);
            }

            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
                drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_Q2", "Draw Q Extended").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_AutoQ", "Draw Auto Q Enable").SetValue(true));

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
            var itemTarget = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);
            var dmg = GetComboDamage(itemTarget);

            if (useE)
                Cast_E();

            if (useQ)
                Cast_Q(source);

            if (useR)
                Cast_R(dmg);

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

            if (target != null)
                if (menu.Item("Q_thirdE").GetValue<bool>() && E.IsReady() && CanCastE(target))
                    return;

            if (!ThirdQ() && target != null && target.IsValidTarget(Q.Range))
            {
                if (Player.Distance(target) < 100)
                    Q.Cast(target.ServerPosition, packets());
                else
                    Q.Cast(target, packets());
            }
            else if(ThirdQ())
            {
                CastBasicSkillShot(Q2, Q2.Range, TargetSelector.DamageType.Physical, GetHitchance(source));
            }
        }

        private void Cast_E()
        {
            var target = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);

            if (target == null || !E.IsReady() || !CanCastE(target))
                return;

            if (E.IsKillable(target) && Player.Distance(target) < E.Range + target.BoundingRadius)
                E.CastOnUnit(target, packets());

            //EQ3
            if (ThirdQ() && Player.ServerPosition.To2D().Distance(target.ServerPosition.To2D()) < E.Range)
            {
                E.CastOnUnit(target);
                Utility.DelayAction.Add(200, () => Q.Cast(target, packets()));
                return;
            }

            if (Player.ServerPosition.To2D().Distance(target.ServerPosition.To2D()) <= menu.Item("E_Min_Dist").GetValue<Slider>().Value)
                return;

            //gapclose
            if (menu.Item("E_GapClose").GetValue<bool>()) { 
                var allMinionQ = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);

                if (allMinionQ.Count > 0)
                {
                    Obj_AI_Base bestMinion = allMinionQ[0];
                    Vector3 bestVec = Player.ServerPosition + Vector3.Normalize(bestMinion.ServerPosition - Player.ServerPosition) * 475;

                    foreach (var minion in allMinionQ.Where(CanCastE))
                    {
                        var dashVec = Player.ServerPosition + Vector3.Normalize(minion.ServerPosition - Player.ServerPosition) * 475;

                        if (target.Distance(Player) > target.Distance(bestVec) - 50 && target.Distance(bestVec) > target.Distance(dashVec))
                        {
                            bestMinion = minion;
                            bestVec = dashVec;
                        }
                    }
                    if (target.Distance(Player) > target.Distance(bestVec) - 50 && bestMinion != null)
                    {
                        E.CastOnUnit(bestMinion, packets());
                        return;
                    }
                }
            }

            if (Q.IsReady() && Player.Distance(target) > menu.Item("E_Min_Dist").GetValue<Slider>().Value &&
                Player.Distance(target) < E.Range)
            {
                E.CastOnUnit(target, packets());
                Utility.DelayAction.Add(200, () => Q.Cast(target, packets()));
                return;
            }

            if (Player.ServerPosition.To2D().Distance(target.ServerPosition.To2D()) > menu.Item("E_Min_Dist").GetValue<Slider>().Value && Player.Distance(target) < E.Range + target.BoundingRadius)
                E.CastOnUnit(target, packets());
        }

        private void Cast_R(float dmg)
        {
            int hit = 0;
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(R.Range) && isKnockedUp(x)))
            {
                hit = 1;
                if (dmg > target.Health && menu.Item("R_If_Killable").GetValue<bool>())
                    R.Cast(packets());

                hit += ObjectManager.Get<Obj_AI_Hero>().Count(x => x.ChampionName != target.ChampionName && target.Distance(x) < 400 && isKnockedUp(x));
            }

            if (hit >= menu.Item("ComboR_MEC").GetValue<Slider>().Value)
                R.Cast();
        }

        private void Cast_MecR()
        {
            if (menu.Item("R_MEC").GetValue<Slider>().Value == 0)
                return;

            int hit = 1;
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(R.Range) && isKnockedUp(x)))
            {
                hit += ObjectManager.Get<Obj_AI_Hero>().Count(x => x.ChampionName != target.ChampionName && target.Distance(x) < 400 && isKnockedUp(x));
            }       

            if (hit >= menu.Item("R_MEC").GetValue<Slider>().Value)
                R.Cast();
        }

        private bool isKnockedUp(Obj_AI_Hero x)
        {
            return (x.HasBuffOfType(BuffType.Knockup) || x.HasBuffOfType(BuffType.Knockback) || x.HasBuff("yasuoq3mis"));
        }

        private void AutoQ()
        {
            if (!Q.IsReady() || !menu.Item("Q_Auto").GetValue<KeyBind>().Active || Environment.TickCount - E.LastCastAttemptT < 250 + Game.Ping)
                return;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (!ThirdQ() && target != null && target.IsValidTarget(Q.Range))
            {
                if (menu.Item("Q_UnderTower").GetValue<bool>() && target.UnderTurret(true))
                    return;

                if (Player.Distance(target) < 100)
                    Q.Cast(target.ServerPosition, packets());
                else
                    Q.Cast(target, packets());
            }
            else if (menu.Item("Q_Auto_third").GetValue<bool>() && ThirdQ())
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

            if (enemies.Count > 0)
                Q.Cast(enemies[0], packets());
        }


        private void SmartKs()
        {
            
            if (!menu.Item("smartKS").GetValue<bool>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Q2.Range) && !x.HasBuffOfType(BuffType.Invulnerability)).OrderByDescending(GetComboDamage))
            {
                if (target != null)
                {
                    //E + Q
                    if (Player.Distance(target.ServerPosition) <= E.Range && (Player.GetSpellDamage(target, SpellSlot.E) + Player.GetSpellDamage(target, SpellSlot.Q)) >
                        target.Health + 20)
                    {
                        if (E.IsReady() && Q.IsReady())
                        {
                            E.Cast(target, packets());
                            Obj_AI_Hero target1 = target;
                            Utility.DelayAction.Add(200, () => Q.Cast(target1.Position));
                            return;
                        }
                    }

                    //Q
                    if ((Player.GetSpellDamage(target, SpellSlot.Q)) > target.Health + 20)
                    {
                        if (!ThirdQ() && target.IsValidTarget(Q.Range))
                        {
                            Q.Cast(target, packets());
                        }
                        else if(ThirdQ() && target.IsValidTarget(Q2.Range))
                        {
                            Q.Cast(target, packets());
                        }
                    }

                    //E
                    if (Player.Distance(target.ServerPosition) <= E.Range && (Player.GetSpellDamage(target, SpellSlot.E)) > target.Health + 20)
                    {
                        if (E.IsReady())
                        {
                            E.Cast(target, packets());
                            return;
                        }
                    }
                }
            }
        }

        private void LastHit()
        {
            var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            var allMinionsQ2 = MinionManager.GetMinions(Player.ServerPosition, Q2.Range, MinionTypes.All, MinionTeam.NotAlly);
            var allMinionsE = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQLast").GetValue<bool>();
            var useE = menu.Item("UseELast").GetValue<bool>();


            if (useQ && Q.IsReady())
            {
                if (!ThirdQ())
                {
                    foreach (var minion in allMinionsQ)
                    {
                        if (Q.IsKillable(minion))
                            Q.Cast(minion, packets());
                    }
                }
                else
                {
                    foreach (var minion in allMinionsQ2)
                    {
                        if (Q.IsKillable(minion))
                            Q.Cast(minion, packets());
                    }
                }
            }

            if (useE && E.IsReady())
            {
                foreach (var minion in allMinionsE.Where(CanCastE))
                {
                    var dashVec = Player.ServerPosition + Vector3.Normalize(minion.ServerPosition - Player.ServerPosition) * 475;
                    if (!menu.Item("E_UnderTower_Farm").GetValue<bool>() && dashVec.UnderTurret(true))
                        continue;

                    var predHealth = HealthPrediction.GetHealthPrediction(minion, (int)(Player.Distance(minion) * 1000 / 2000));

                    if (predHealth <= Player.GetSpellDamage(minion, SpellSlot.E))
                        E.CastOnUnit(minion, packets());
                }
            }

        }

        private void Farm()
        {
            var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            var allMinionsQ2 = MinionManager.GetMinions(Player.ServerPosition, Q2.Range, MinionTypes.All, MinionTeam.NotAlly);
            var allMinionsE = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();
            var useQ3 = menu.Item("UseQ3Farm").GetValue<bool>();

            var min = menu.Item("LaneClear_useQ_minHit").GetValue<Slider>().Value;

            if (useQ && useE && Q.IsReady() && E.IsReady())
            {
                foreach (var minion in allMinionsE.Where(CanCastE))
                {
                    var dashVec = Player.ServerPosition + Vector3.Normalize(minion.ServerPosition - Player.ServerPosition) * 475;
                    var count = MinionManager.GetMinions(dashVec, 300, MinionTypes.All, MinionTeam.NotAlly).Count - 1;
                    
                    if (!menu.Item("E_UnderTower_Farm").GetValue<bool>() && dashVec.UnderTurret(true))
                        continue;

                    if (count >= min)
                    {
                        E.CastOnUnit(minion, packets());
                        Obj_AI_Base minion1 = minion;
                        Utility.DelayAction.Add(200, () => Q.Cast(minion1.ServerPosition, packets()));
                    }
                }
            }

            if (useQ && Q.IsReady())
            {
                if (!ThirdQ())
                {
                    var pred = Q.GetLineFarmLocation(allMinionsQ);

                    if (pred.MinionsHit >= min)
                        Q.Cast(pred.Position, packets());
                }
                else if(useQ3)
                {
                    var pred = Q.GetLineFarmLocation(allMinionsQ2);

                    if (pred.MinionsHit >= min)
                        Q.Cast(pred.Position, packets());
                }
            }

            if (useE && E.IsReady())
            {
                foreach (var minion in allMinionsE.Where(CanCastE))
                {
                    var dashVec = Player.ServerPosition + Vector3.Normalize(minion.ServerPosition - Player.ServerPosition) * 475;
                    if (!menu.Item("E_UnderTower_Farm").GetValue<bool>() && dashVec.UnderTurret(true))
                        continue;

                    var predHealth = HealthPrediction.GetHealthPrediction(minion, (int)(Player.Distance(minion) * 1000 / 2000));

                    if (predHealth <= Player.GetSpellDamage(minion, SpellSlot.E))
                        E.CastOnUnit(minion, packets());
                }
            }
        }

        private bool ThirdQ()
        {
            return Player.HasBuff("YasuoQ3W");
        }

        private bool CanCastE(Obj_AI_Base target)
        {
            return !target.HasBuff("YasuoDashWrapper");
        }

        public override void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (spell.DangerLevel < InterruptableDangerLevel.Medium || unit.IsAlly || !Q.IsReady() || !ThirdQ() || !menu.Item("Interrupt").GetValue<bool>())
                return;

            if (unit.IsValidTarget(E.Range))
            {
                E.CastOnUnit(unit, packets());
                Utility.DelayAction.Add(200, () => Q.Cast(unit.Position));
            }
            else if(unit.IsValidTarget(Q2.Range))
            {
                Q2.Cast(unit);
            }
        }

        private Obj_SpellMissile _windWall = null;
        private Obj_SpellMissile _eSlide = null;

        public override void GameObject_OnCreate(GameObject sender, EventArgs args2)
        {
            if (!(sender is Obj_SpellMissile) || !sender.IsValid)
                return;
            var args = (Obj_SpellMissile)sender;

            if (sender.Name != "missile")
            {
                if (menu.Item(args.SData.Name + "E").GetValue<bool>() && E.IsReady())
                {
                    //Game.PrintChat("RAWR1");
                    _eSlide = args;
                }

                //Game.PrintChat(args.SData.Name);
                if (menu.Item(args.SData.Name + "W_Wall").GetValue<bool>() && W.IsReady())
                {
                    //Game.PrintChat("RAWR1");
                    _windWall = args;

                    if (_windWall != null && W.IsReady())
                    {
                        if (Player.Distance(_windWall.Position) < 400)
                        {
                            W.Cast(_windWall.Position, packets());

                            var vec = Player.ServerPosition - (_windWall.Position - Player.ServerPosition) * 50;
                            
                            Player.IssueOrder(GameObjectOrder.MoveTo, vec);
                            _windWall = null;
                        }
                    }
                }

            }
        }

        public override void GameObject_OnDelete(GameObject sender, EventArgs args2)
        {
            if (!(sender is Obj_SpellMissile) || !sender.IsValid)
                return;
            var args = (Obj_SpellMissile)sender;

            if (sender.Name != "missile")
            {
                if (menu.Item(args.SData.Name + "W_Wall").GetValue<bool>())
                {
                    _windWall = null;
                }

                if (menu.Item(args.SData.Name + "E").GetValue<bool>())
                {
                    //Game.PrintChat("RAWR1");
                    _eSlide = null;
                }
            }
        }
        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (unit.IsEnemy && (unit is Obj_AI_Hero))
            {
                if (Player.Distance(args.End) > W.Range)
                    return;


                if (menu.Item(args.SData.Name + "E").GetValue<bool>())
                {
                    var minion = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);

                    foreach (var m in minion.Where(CanCastE))
                    {
                        var dashVec = Player.ServerPosition + Vector3.Normalize(m.ServerPosition - Player.ServerPosition) * 475;
                        Object[] obj = VectorPointProjectionOnLineSegment(dashVec.To2D(), args.Start.To2D(), args.End.To2D());
                        var isOnseg = (bool)obj[2];
                        var pointLine = (Vector2)obj[1];

                        if (!isOnseg && !dashVec.UnderTurret(true) && m.Distance(pointLine.To3D()) > args.SData.LineWidth)
                        {
                            
                            E.CastOnUnit(m, packets());
                            E.LastCastAttemptT = Environment.TickCount;
                            return;
                        }
                    }

                }

                if (menu.Item(args.SData.Name + "W_Wall").GetValue<bool>() && W.IsReady() && (Player.Distance(args.Start) < 1000 || Player.Distance(args.End) < 1000))
                {
                    W.Cast(args.Start, packets());

                    var vec = Player.ServerPosition - (args.Start - Player.ServerPosition)*50;

                    Player.IssueOrder(GameObjectOrder.MoveTo, vec);
                    return;
                }

            }

            if (unit.IsMe)
            {
                SpellSlot castedSlot = ObjectManager.Player.GetSpellSlot(args.SData.Name, false);

                if (castedSlot == SpellSlot.E)
                {
                    E.LastCastAttemptT = Environment.TickCount;
                }
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

            //smart ks
            SmartKs();

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("LastHit").GetValue<KeyBind>().Active)
                    LastHit();

                if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    Farm();

                if (menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();
            }

            if (_eSlide != null)
            {
                var minion = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);

                foreach (var m in minion.Where(CanCastE))
                {
                    var dashVec = Player.ServerPosition + Vector3.Normalize(m.ServerPosition - Player.ServerPosition) * 475;

                    Object[] obj = VectorPointProjectionOnLineSegment(dashVec.To2D(), _eSlide.Position.To2D(), _eSlide.EndPosition.To2D());
                    var isOnseg = (bool)obj[2];
                    
                    var pointLine = (Vector2)obj[1];
                    if (!isOnseg && !dashVec.UnderTurret(true) && m.Distance(pointLine.To3D()) > _eSlide.SData.LineWidth)
                    {

                        E.CastOnUnit(m, packets());
                        E.LastCastAttemptT = Environment.TickCount;
                        _eSlide = null;
                        return;
                    }
                }
            }

            if (_windWall != null && W.IsReady())
            {
                if (Player.Distance(_windWall.Position) < 400)
                {
                    //Game.PrintChat("RAWR");
                    W.Cast(_windWall.Position, packets());

                    var vec = Player.ServerPosition - (_windWall.Position - Player.ServerPosition) * 50;

                    Player.IssueOrder(GameObjectOrder.MoveTo, vec);
                    _windWall = null;
                }
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

            if (menu.Item("Draw_AutoQ").GetValue<bool>())
            {
                Vector2 wts = Drawing.WorldToScreen(Player.Position);
                if (menu.Item("Q_Auto").GetValue<KeyBind>().Active)
                    Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "Auto Q Enabled");
                else
                    Drawing.DrawText(wts[0] - 20, wts[1], Color.Red, "Auto Q Disabled");
            }
        }

    }
}