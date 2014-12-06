using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace xSaliceReligionAIO.Champions
{
    class Fiora : Champion
    {
        public Fiora()
        {
            SetSpells();
            LoadMenu();
        }

        private void SetSpells()
        {
            Q = new Spell(SpellSlot.Q, 600);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R, 400);
        }

        private void LoadMenu()
        {
            //key
            var key = new Menu("Key", "Key");
            {
                key.AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("LastHitKey", "Last Hit!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("Combo_Switch", "Switch mode Key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                //add to menu
                menu.AddSubMenu(key);
            }

            var spellMenu = new Menu("SpellMenu", "SpellMenu");
            {
                var qMenu = new Menu("QMenu", "QMenu");
                {
                    qMenu.AddItem(new MenuItem("Q_Min_Distance", "Min range to Q").SetValue(new Slider(300, 0, 600)));
                    qMenu.AddItem(new MenuItem("Q_Gap_Close", "Q Minion to Gap Close").SetValue(true));
                    spellMenu.AddSubMenu(qMenu);
                }
                var wMenu = new Menu("WMenu", "WMenu");
                {
                    wMenu.AddItem(new MenuItem("W_Incoming", "W Block incoming Atk Always").SetValue(true));
                    wMenu.AddItem(new MenuItem("W_minion", "W Block Minion").SetValue(false));
                    spellMenu.AddSubMenu(wMenu);
                }
                var eMenu = new Menu("EMenu", "EMenu");
                {
                    eMenu.AddItem(new MenuItem("E_Reset", "E Auto-Attack Reset").SetValue(true));
                    spellMenu.AddSubMenu(eMenu);
                }

                var rMenu = new Menu("RMenu", "RMenu");
                {
                    rMenu.AddItem(new MenuItem("R_If_HP", "R If HP <=").SetValue(new Slider(20)));

                    //evading spells
                    var dangerous = new Menu("Dodge Dangerous", "Dodge Dangerous");
                    {
                        foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy))
                        {
                            dangerous.AddSubMenu(new Menu(hero.ChampionName, hero.ChampionName));
                            dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.Q).Name + "R_Dodge", hero.Spellbook.GetSpell(SpellSlot.Q).Name).SetValue(false));
                            dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.W).Name + "R_Dodge", hero.Spellbook.GetSpell(SpellSlot.W).Name).SetValue(false));
                            dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.E).Name + "R_Dodge", hero.Spellbook.GetSpell(SpellSlot.E).Name).SetValue(false));
                            dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.R).Name + "R_Dodge", hero.Spellbook.GetSpell(SpellSlot.R).Name).SetValue(false));
                        }
                        rMenu.AddSubMenu(dangerous);
                    }

                    spellMenu.AddSubMenu(rMenu);
                }

                menu.AddSubMenu(spellMenu);
            }

            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("selected", "Focus Selected Target").SetValue(true));
                combo.AddItem(new MenuItem("Combo_mode", "Combo Mode").SetValue(new StringList(new[] { "Normal", "Q-AA-Q-AA-Ult" })));
                combo.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                combo.AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
                combo.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                combo.AddItem(new MenuItem("Ignite", "Use Ignite").SetValue(true));
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

            var lastHit = new Menu("Lasthit", "Lasthit");
            {
                lastHit.AddItem(new MenuItem("UseQLastHit", "Use Q").SetValue(true));
                AddManaManagertoMenu(lastHit, "Lasthit", 30);
                //add to menu
                menu.AddSubMenu(lastHit);
            }

            var farm = new Menu("LaneClear", "LaneClear");
            {
                farm.AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
                farm.AddItem(new MenuItem("UseQFarm_Tower", "Do not Q under Tower").SetValue(true));
                farm.AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
                AddManaManagertoMenu(farm, "LaneClear", 30);
                //add to menu
                menu.AddSubMenu(farm);
            }
            var misc = new Menu("Misc", "Misc");
            {
                misc.AddItem(new MenuItem("smartKS", "Use Smart KS System").SetValue(true));
                //add to menu
                menu.AddSubMenu(misc);
            }
            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
                drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
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
                comboDamage += Player.GetSpellDamage(target, SpellSlot.Q)*2;

            if (W.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.W);

            if (R.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.R) / countEnemiesNearPosition(target.ServerPosition, R.Range);

            if (Items.CanUseItem(Bilge.Id))
                comboDamage += Player.GetItemDamage(target, Damage.DamageItems.Bilgewater);

            if (Items.CanUseItem(Botrk.Id))
                comboDamage += Player.GetItemDamage(target, Damage.DamageItems.Botrk);

            if (Items.CanUseItem(3077))
                comboDamage += Player.GetItemDamage(target, Damage.DamageItems.Tiamat);

            if (Items.CanUseItem(3074))
                comboDamage += Player.GetItemDamage(target, Damage.DamageItems.Hydra);

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                comboDamage += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            return (float)(comboDamage + Player.GetAutoAttackDamage(target) * 3);
        }

        private void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>(), "Combo");
        }

        private void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(),
                menu.Item("UseEHarass").GetValue<bool>(), false, "Harass");
        }

        private void UseSpells(bool useQ, bool useE, bool useR, string source)
        {
            if (source == "Harass" && !HasMana("Harass"))
                return;

            if (useR)
                Cast_R();

            if (useQ)
                Cast_Q();

            if (source == "Combo")
            {
                var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
                if (qTarget != null)
                {
                    if (GetComboDamage(qTarget) >= qTarget.Health && Ignite_Ready() && menu.Item("Ignite").GetValue<bool>() && Player.Distance(qTarget) < 300)
                        Use_Ignite(qTarget);

                    if (menu.Item("Botrk").GetValue<bool>())
                    {
                        if (GetComboDamage(qTarget) > qTarget.Health && !qTarget.HasBuffOfType(BuffType.Slow))
                        {
                            Use_Bilge(qTarget);
                            Use_Botrk(qTarget);
                        }
                    }
                }
            }

            if (useE && !menu.Item("E_Reset").GetValue<bool>())
                E.Cast(packets());
        }

        private void Lasthit()
        {
            if (menu.Item("UseQLastHit").GetValue<bool>() && HasMana("Lasthit"))
                Cast_Q_Last_Hit();
        }

        private void Farm()
        {
            if (!HasMana("LaneClear"))
                return;

            List<Obj_AI_Base> allMinionsE = MinionManager.GetMinions(Player.ServerPosition, Player.AttackRange + Player.BoundingRadius,
                MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (useQ)
                Cast_Q_Last_Hit();

            if (useE && allMinionsE.Count > 0 && E.IsReady())
                E.Cast();

        }

        private void SmartKs()
        {
            if (!menu.Item("smartKS").GetValue<bool>())
                return;
            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Q.Range) && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability)))
            {
                //Q *2
                if (Player.GetSpellDamage(target, SpellSlot.Q)*2 > target.Health && Player.Distance(target) < Q.Range && Q.IsReady())
                {
                    Q.CastOnUnit(target, packets());
                    return;
                }
                //Q
                if (Player.GetSpellDamage(target, SpellSlot.Q) > target.Health && Player.Distance(target) < Q.Range && Q.IsReady())
                {
                    Q.CastOnUnit(target, packets());
                    return;
                }
            }
        }

        private void Cast_Q()
        {
            var target = SimpleTs.GetTarget(Q.Range * 2, SimpleTs.DamageType.Physical);

            if (GetTargetFocus(Q.Range) != null)
                target = GetTargetFocus(Q.Range);

            int mode = menu.Item("Combo_mode").GetValue<StringList>().SelectedIndex;
            if (mode == 0)
            {
                if (Q.IsReady() && target != null)
                {
                    if (Q.IsKillable(target))
                        Q.CastOnUnit(target, packets());

                    if (Player.GetSpellDamage(target, SpellSlot.Q)*2 > target.Health)
                        Q.CastOnUnit(target, packets());

                    if (Environment.TickCount - Q.LastCastAttemptT > 3800)
                        Q.CastOnUnit(target, packets());

                    var minDistance = menu.Item("Q_Min_Distance").GetValue<Slider>().Value;

                    if (Player.Distance(target) > Q.Range && menu.Item("Q_Gap_Close").GetValue<bool>())
                    {
                        var allMinionQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All,
                            MinionTeam.NotAlly);

                        Obj_AI_Base bestMinion = allMinionQ[0];

                        foreach (var minion in allMinionQ)
                        {
                            if (target.Distance(minion) < Q.Range && Player.Distance(minion) < Q.Range &&
                                target.Distance(minion) < target.Distance(Player))
                                if (target.Distance(minion) < target.Distance(bestMinion))
                                    bestMinion = minion;
                        }
                    }

                    if (Player.Distance(target) > minDistance &&
                        Player.Distance(target) < Q.Range + target.BoundingRadius)
                    {
                        Q.CastOnUnit(target, packets());
                    }
                }
            }
            else if (mode == 1)//Ham mode
            {
                if (target == null)
                    return;

                if (Q.IsReady() && Environment.TickCount - Q.LastCastAttemptT > 4000 && Player.Distance(target) < Q.Range && Player.Distance(target) > Player.AttackRange)
                    Q.CastOnUnit(target, packets());
            }
        }

        private void Cast_Q_Last_Hit()
        {
            var allMinionQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range + Player.BoundingRadius, MinionTypes.All, MinionTeam.NotAlly);

            if (allMinionQ.Count > 0 && Q.IsReady())
            {

                foreach (var minion in allMinionQ)
                {
                    double dmg = Player.GetSpellDamage(minion, SpellSlot.Q);

                    if (dmg > minion.Health + 35)
                    {
                        if (menu.Item("UseQFarm_Tower").GetValue<bool>())
                        {
                            if (!Utility.UnderTurret(minion, true))
                            {
                                Q.Cast(minion, packets());
                                return;
                            }
                        }
                        else
                            Q.Cast(minion, packets());
                    }
                }
            }
        }

        private void Cast_R()
        {
            var target = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);

            var range = R.Range;
            if (GetTargetFocus(range) != null)
                target = GetTargetFocus(range);

            if (target != null && R.IsReady())
            {
                if (Player.GetSpellDamage(target, SpellSlot.R)/
                    countEnemiesNearPosition(target.ServerPosition, R.Range) >
                    target.Health - Player.GetAutoAttackDamage(target)*2)
                    R.CastOnUnit(target, packets());

                var rHpValue = menu.Item("R_If_HP").GetValue<Slider>().Value;
                if (GetHealthPercent() <= rHpValue)
                    R.CastOnUnit(target, packets());
            }
            
        }

        private int _lasttick;
        private void ModeSwitch()
        {
            int mode = menu.Item("Combo_mode").GetValue<StringList>().SelectedIndex;
            int lasttime = Environment.TickCount - _lasttick;

            if (menu.Item("Combo_Switch").GetValue<KeyBind>().Active && lasttime > Game.Ping)
            {
                if (mode == 0)
                {
                    menu.Item("Combo_mode").SetValue(new StringList(new[] { "Normal", "Q-AA-Q-AA-Ult" }, 1));
                    _lasttick = Environment.TickCount + 300;
                }
                else if (mode == 1)
                {
                    menu.Item("Combo_mode").SetValue(new StringList(new[] { "Normal", "Q-AA-Q-AA-Ult" }));
                    _lasttick = Environment.TickCount + 300;
                }
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            SmartKs();

            ModeSwitch();

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    Farm();

                if (menu.Item("LastHitKey").GetValue<KeyBind>().Active)
                    Lasthit();

                if (menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();
            }
        }

        public override void AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe)
            {
                if ((menu.Item("ComboActive").GetValue<KeyBind>().Active || menu.Item("HarassActive").GetValue<KeyBind>().Active )
                    && (target is Obj_AI_Hero))
                {
                    if (menu.Item("E_Reset").GetValue<bool>() && E.IsReady())
                        E.Cast();

                    if(Items.CanUseItem(3077))
                        Items.UseItem(3077);
                    if (Items.CanUseItem(3074))
                        Items.UseItem(3074);

                    int mode = menu.Item("Combo_mode").GetValue<StringList>().SelectedIndex;
                    if (mode == 1)
                    {
                        Q.CastOnUnit(target, packets());

                        if(!Q.IsReady() && R.IsReady())
                            R.CastOnUnit(target, packets());
                    }
                }
            }
        }

        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            SpellSlot castedSlot = ObjectManager.Player.GetSpellSlot(args.SData.Name, false);

            if (castedSlot == SpellSlot.Q)
            {
                Q.LastCastAttemptT = Environment.TickCount;
            }

            if (unit.IsMe)
                return;

            if (xSLxOrbwalker.IsAutoAttack(args.SData.Name) && args.Target.IsMe && Player.Distance(args.End) < 450)
            {
                if (menu.Item("W_Incoming").GetValue<bool>() ||
                    (menu.Item("ComboActive").GetValue<KeyBind>().Active && E.IsReady() &&
                     menu.Item("UseWCombo").GetValue<bool>()) ||
                    (menu.Item("HarassActive").GetValue<KeyBind>().Active && menu.Item("UseWHarass").GetValue<bool>()))
                {
                    if (!menu.Item("W_minion").GetValue<bool>() && !(unit is Obj_AI_Hero))
                        return;

                        W.Cast(packets());
                }
            }

            if (unit.IsEnemy && (unit is Obj_AI_Hero))
            {
                if (Player.Distance(unit) > R.Range || !R.IsReady())
                    return;

                if (menu.Item(args.SData.Name + "R_Dodge").GetValue<bool>() && args.SData.Name == "SyndraR")
                {
                    Utility.DelayAction.Add(150, () => R.CastOnUnit(unit, packets()));
                    return;
                }

                if (menu.Item(args.SData.Name + "R_Dodge").GetValue<bool>())
                    R.CastOnUnit(unit, packets());
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            if (menu.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (menu.Item("Draw_Q").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_R").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_R_Killable").GetValue<bool>() && R.IsReady())
            {
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(5000) && !x.IsDead && x.IsEnemy).OrderBy(x => x.Health))
                {
                    Vector2 wts = Drawing.WorldToScreen(target.Position);
                    if (Player.GetSpellDamage(target, SpellSlot.R) / countEnemiesNearPosition(target.ServerPosition, R.Range) > target.Health)
                    {
                        Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "KILL!!!");

                    }
                }
            }

            Vector2 wts2 = Drawing.WorldToScreen(Player.Position);
            int mode = menu.Item("Combo_mode").GetValue<StringList>().SelectedIndex;
            if (mode == 0)
                Drawing.DrawText(wts2[0] - 20, wts2[1], Color.White, "Normal");
            else if (mode == 1)
                Drawing.DrawText(wts2[0] - 20, wts2[1], Color.White, "Q-AA-Q-AA-Ult");
        }
    }
}
