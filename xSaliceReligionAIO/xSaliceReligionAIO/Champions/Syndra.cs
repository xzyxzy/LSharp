using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace xSaliceReligionAIO.Champions
{
    class Syndra : Champion
    {
        public Syndra()
        {
            SetSpells();
            LoadMenu();
        }

        private Spell _qe;

        private void SetSpells()
        {
            Q = new Spell(SpellSlot.Q, 800);
            Q.SetSkillshot(600f, 130f, 2000f, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 1200);
            W.SetSkillshot(0, 140f, 2000f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 700);
            E.SetSkillshot(300f, (float)(45 * 0.5), 2500f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 750);

            _qe = new Spell(SpellSlot.Q, 1250);
            _qe.SetSkillshot(900f, 60f, 2000f, false, SkillshotType.SkillshotLine);

        }

        private void LoadMenu()
        {
            var key = new Menu("Key", "Key");
            {
                key.AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));
                key.AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("Misc_QE_Mouse", "QE to mouse").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                //add to menu
                menu.AddSubMenu(key);
            }

            var spellMenu = new Menu("SpellMenu", "SpellMenu");
            {
                var qMenu = new Menu("QMenu", "QMenu");
                {
                    qMenu.AddItem(new MenuItem("Q_Auto_Immobile", "Auto Q on Immobile").SetValue(true));
                    spellMenu.AddSubMenu(qMenu);
                }

                var wMenu = new Menu("WMenu", "WMenu");
                {
                    wMenu.AddItem(new MenuItem("W_Only_Orb", "Only Pick Up Orb").SetValue(false));
                    spellMenu.AddSubMenu(wMenu);
                }
                var rMenu = new Menu("RMenu", "RMenu");
                {
                    rMenu.AddItem(new MenuItem("R_Overkill_Check", "Overkill Check").SetValue(true));

                    rMenu.AddSubMenu(new Menu("Don't use R on", "Dont_R"));
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team)
                    )
                        rMenu.SubMenu("Dont_R")
                            .AddItem(new MenuItem("Dont_R" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));

                    spellMenu.AddSubMenu(rMenu);
                }

                menu.AddSubMenu(spellMenu);
            }

            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                combo.AddItem(new MenuItem("UseQECombo", "Use QE").SetValue(true));
                combo.AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
                combo.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                combo.AddItem(new MenuItem("Ignite", "Use Ignite").SetValue(true));
                combo.AddItem(new MenuItem("DFG", "DFG").SetValue(true));
                menu.AddSubMenu(combo);
            }

            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
                harass.AddItem(new MenuItem("UseQEHarass", "Use W").SetValue(true));
                harass.AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
                harass.AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
                AddManaManagertoMenu(harass, "Harass", 30);
                //add to menu
                menu.AddSubMenu(harass);
            }

            var farm = new Menu("LaneClear", "LaneClear");
            {
                farm.AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
                farm.AddItem(new MenuItem("UseWFarm", "Use W").SetValue(true));
                farm.AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
                AddManaManagertoMenu(farm, "LaneClear", 30);
                //add to menu
                menu.AddSubMenu(farm);
            }

            var miscMenu = new Menu("Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("QE_Interrupt", "Use QE to Interrupt").SetValue(true));
                miscMenu.AddItem(new MenuItem("E_Gap_Closer", "Use E On Gap Closer").SetValue(true));
                miscMenu.AddItem(new MenuItem("smartKS", "Use Smart KS System").SetValue(true));
                menu.AddSubMenu(miscMenu);
            }

            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
                drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_QE", "Draw QE").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R_Killable", "Draw R Mark on Killable").SetValue(true));

                MenuItem drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage").SetValue(true);
                drawMenu.AddItem(drawComboDamageMenu);
                Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
                drawComboDamageMenu.ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs eventArgs)
                    {
                        Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                    };

                menu.AddSubMenu(drawMenu);
            }
        }

        private float GetComboDamage(Obj_AI_Base target)
        {
            double comboDamage = 0;

            if (Q.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.Q);

            if (W.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.W);

            if (E.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.E);

            comboDamage += Get_Ult_Dmg(target);

            if (Ignite_Ready())
                comboDamage += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            return (float)(comboDamage + Player.GetAutoAttackDamage(target));
        }

        private float Get_Ult_Dmg(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Items.CanUseItem(DFG.Id) && menu.Item("DFG").GetValue<bool>())
                damage += Player.GetItemDamage(enemy, Damage.DamageItems.Dfg) / 1.2;

            if (R.IsReady())
                damage += (3 + getOrbCount()) * Player.GetSpellDamage(enemy, SpellSlot.R, 1) - 20;

            return (float)damage * (Items.CanUseItem(DFG.Id) ? 1.2f : 1);
        }

        private void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>(), menu.Item("UseQECombo").GetValue<bool>(), "Combo");
        }

        private void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(),
                menu.Item("UseEHarass").GetValue<bool>(), false, menu.Item("UseQEHarass").GetValue<bool>(), "Harass");
        }

        private void UseSpells(bool useQ, bool useW, bool useE, bool useR, bool useQe, string source)
        {
            if (source == "Harass" && !HasMana("Harass"))
                return;

            var useIgnite = menu.Item("Ignite").GetValue<bool>();

            if(useQ)
                Cast_Q();

            if(useW)
                Cast_W(true);

            var qTarget = SimpleTs.GetTarget(650, SimpleTs.DamageType.Magical);
            if (qTarget != null)
            {
                if (GetComboDamage(qTarget) >= qTarget.Health && Ignite_Ready() && useIgnite)
                    Use_Ignite(qTarget);
            }

            if(useE)
                Cast_E();

            if(useQe)
                Cast_QE();

            if(useR)
                Cast_R();
        }

        private void Farm()
        {
            if (!HasMana("LaneClear"))
                return;

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useW = menu.Item("UseWFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (useQ)
                CastBasicFarm(Q);

            if(useW)
                Cast_W(false);

            if(useE)
                CastBasicFarm(E);
        }

        private void SmartKs()
        {
            if (!menu.Item("smartKS").GetValue<bool>())
                return;

             foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(_qe.Range) && x.IsEnemy && !x.IsDead).OrderByDescending(GetComboDamage))
            {
                //Q
                if (Q.IsKillable(target) && Player.Distance(target) < Q.Range)
                {
                    Q.Cast(target);
                }
                //E
                if (E.IsKillable(target) && Player.Distance(target) < E.Range)
                {
                    E.Cast(target);
                }
                //QE
                if (E.IsKillable(target) && Player.Distance(target) < _qe.Range)
                {
                    Cast_QE(false, target);
                }
            }
        }

        private void Cast_Q()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            if (qTarget == null)
                return;
            Q.UpdateSourcePosition();
            if (Q.IsReady())
                Q.Cast(qTarget, packets());
        }

        private void Cast_W(bool mode)
        {
            if (mode)
            {
                var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);

                var grabbableObj = Get_Nearest_orb();
                var wToggleState = Player.Spellbook.GetSpell(SpellSlot.W).ToggleState;

                if (wTarget == null)
                    return;
                if (wToggleState == 1 && Environment.TickCount - W.LastCastAttemptT > Game.Ping && W.IsReady() &&
                    grabbableObj != null)
                {
                    if (grabbableObj.Distance(Player) < W.Range)
                    {
                        W.Cast(grabbableObj.ServerPosition);
                        W.LastCastAttemptT = Environment.TickCount + 500;
                        return;
                    }
                }

                W.UpdateSourcePosition(Get_Current_Orb().ServerPosition, Get_Current_Orb().ServerPosition);

                if (Player.Distance(wTarget) < E.Range)
                {
                    if (wToggleState != 1 && W.IsReady() && Environment.TickCount - W.LastCastAttemptT > -500 + Game.Ping)
                    {
                        W.Cast(wTarget);
                        return;
                    }
                }

                if (wToggleState != 1 && W.IsReady())
                {
                    W.Cast(wTarget);
                }
            }
            else
            {
                var allMinionsW = MinionManager.GetMinions(Player.ServerPosition, W.Range + W.Width + 20, MinionTypes.All, MinionTeam.NotAlly);

                if (allMinionsW.Count < 2)
                    return;

                var grabbableObj = Get_Nearest_orb();
                var wToggleState = Player.Spellbook.GetSpell(SpellSlot.W).ToggleState;

                if (wToggleState == 1 && Environment.TickCount - W.LastCastAttemptT > Game.Ping && W.IsReady() &&
                        grabbableObj != null)
                {
                    W.Cast(grabbableObj.ServerPosition);
                    W.LastCastAttemptT = Environment.TickCount + 1000;
                    return;
                }

                W.UpdateSourcePosition(Get_Current_Orb().ServerPosition, Get_Current_Orb().ServerPosition);

                var farmLocation = W.GetCircularFarmLocation(allMinionsW);

                if (farmLocation.MinionsHit >= 1)
                    W.Cast(farmLocation.Position);
            }
        }

        private void Cast_E()
        {
            if (getOrbCount() <= 0)
                return;
            var target = SimpleTs.GetTarget(_qe.Range + 100, SimpleTs.DamageType.Magical);

            foreach (var orb in getOrb().Where(x => Player.Distance(x) < E.Range))
            {
                var startPos = orb.ServerPosition;
                var endPos = Player.ServerPosition + (startPos - Player.ServerPosition) * _qe.Range;

                E.UpdateSourcePosition();
                var targetPos = _qe.GetPrediction(target);

                var projection = targetPos.UnitPosition.To2D().ProjectOn(startPos.To2D(), endPos.To2D());

                if (!projection.IsOnSegment || !E.IsReady() ||
                    !(projection.LinePoint.Distance(targetPos.UnitPosition.To2D()) < _qe.Width + target.BoundingRadius))
                    return;
                E.Cast(orb.ServerPosition, packets());
                W.LastCastAttemptT = Environment.TickCount + 500;
                return;
            }
        }

        private void Cast_R()
        {
            var rTarget = SimpleTs.GetTarget(R.Level > 2 ? R.Range : 675, SimpleTs.DamageType.Magical);

            if (rTarget == null)
                return;
            if (menu.Item("Dont_R" + rTarget.ChampionName) == null)
                return;
            if (menu.Item("Dont_R" + rTarget.ChampionName).GetValue<bool>())
                return;
            if (menu.Item("R_Overkill_Check").GetValue<bool>())
            {
                if (Player.GetSpellDamage(rTarget, SpellSlot.Q)/2 > rTarget.Health)
                {
                    return;
                }
                if (Get_Ult_Dmg(rTarget) > rTarget.Health + 20 && rTarget.Distance(Player) < R.Range)
                {
                    if (Items.CanUseItem(DFG.Id) && menu.Item("DFG").GetValue<bool>())
                        Use_DFG(rTarget);

                    R.CastOnUnit(rTarget, packets());
                }
            }
            else if (Get_Ult_Dmg(rTarget) > rTarget.Health - 20 && rTarget.Distance(Player) < R.Range)
            {
                if (Items.CanUseItem(DFG.Id) && menu.Item("DFG").GetValue<bool>())
                    Use_DFG(rTarget);

                R.CastOnUnit(rTarget, packets());
            }
        }

        private void Cast_QE(bool usePred = true , Obj_AI_Base target = null)
        {
            var qeTarget = SimpleTs.GetTarget(_qe.Range, SimpleTs.DamageType.Magical);

            if (target != null)
                qeTarget = (Obj_AI_Hero)target;

            if (qeTarget == null)
                return;

            _qe.UpdateSourcePosition();

            var qePred = _qe.GetPrediction(qeTarget);
            var predVec = Player.ServerPosition + Vector3.Normalize(qePred.UnitPosition - Player.ServerPosition) * (E.Range - 100);

            if (!Q.IsReady() || !E.IsReady())
                return;

            Q.Cast(predVec, packets());
            _qe.LastCastAttemptT = Environment.TickCount;
        }

        private void CastQeMouse()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(_qe.Range)))
                if (Game.CursorPos.Distance(enemy.ServerPosition) < 300)
                    Cast_QE(false, enemy);
        }

        private void QImmobile()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            if (!menu.Item("Q_Auto_Immobile").GetValue<bool>() || qTarget == null)
                return;
            if (Q.GetPrediction(qTarget).Hitchance == HitChance.Immobile)
                Q.Cast(qTarget);
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {

            if (menu.Item("Misc_QE_Mouse").GetValue<KeyBind>().Active)
            {
                CastQeMouse();
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

                if (menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();
            }

            QImmobile();
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            if (menu.Item("Draw_Disabled").GetValue<bool>())
                return;

            xSLxOrbwalker.EnableDrawing();
            if (menu.Item("Draw_Q").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
            if (menu.Item("Draw_QE").GetValue<bool>())
                if (Q.Level > 0 && E.Level > 0)
                    Utility.DrawCircle(Player.Position, _qe.Range, Q.IsReady() && E.IsReady() ? Color.Green : Color.Red);
            if (menu.Item("Draw_W").GetValue<bool>())
                if (W.Level > 0)
                    Utility.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_R").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_R_Killable").GetValue<bool>() && R.IsReady())
            {
                foreach (
                    var wts in
                        from unit in
                            ObjectManager.Get<Obj_AI_Hero>()
                                .Where(x => x.IsValidTarget(2000) && !x.IsDead && x.IsEnemy)
                                .OrderBy(x => x.Health)
                        let health = unit.Health + unit.HPRegenRate + 10
                        where Get_Ult_Dmg(unit) > health
                        select Drawing.WorldToScreen(unit.Position))
                {
                    Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "KILL!!!");
                }
            }

        }

        private int getOrbCount()
        {
            return
                ObjectManager.Get<Obj_AI_Minion>().Count(obj => obj.IsValid && obj.Team == ObjectManager.Player.Team && obj.Name == "Seed");
        }

        private IEnumerable<Obj_AI_Minion> getOrb()
        {
            return ObjectManager.Get<Obj_AI_Minion>().Where(obj => obj.IsValid && obj.Team == ObjectManager.Player.Team && obj.Name == "Seed").ToList();
        }

        private Obj_AI_Minion Get_Nearest_orb()
        {
            var orb =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(obj => obj.IsValid && obj.Team == ObjectManager.Player.Team && obj.Name == "Seed")
                    .ToList()
                    .OrderBy(x => Player.Distance(x))
                    .FirstOrDefault();
            if (orb != null)
                return orb;

            if (menu.Item("W_Only_Orb").GetValue<bool>())
                return null;
            var minion = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(x => x.IsValidTarget(W.Range));

            return minion;
        }

        private Obj_AI_Base Get_Current_Orb()
        {
            var orb = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(x => x.Team == Player.Team && x.Name == "Seed" && !x.IsTargetable);

            if (orb != null)
                return orb;

            var minion = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(x => x.IsInvulnerable && x.Name != "Seed" && x.Name != "k");

            return minion;
        }

        public override void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!menu.Item("E_Gap_Closer").GetValue<bool>())
                return;

            if (!E.IsReady() || !gapcloser.Sender.IsValidTarget(E.Range))
                return;
            E.Cast(gapcloser.Sender, packets());
            W.LastCastAttemptT = Environment.TickCount + 500;
        }


        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!unit.IsMe || !E.IsReady() || (spell.SData.Name != "SyndraQ") ||
                Environment.TickCount - _qe.LastCastAttemptT >= 300)
                return;
            E.Cast(spell.End, packets());
            W.LastCastAttemptT = Environment.TickCount + 500;
        }
        public override void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (spell.DangerLevel < InterruptableDangerLevel.Medium || unit.IsAlly)
                return;

            if (menu.Item("QE_Interrupt").GetValue<bool>() && unit.IsValidTarget(_qe.Range))
                Cast_QE(false, unit);
        }
    }
}
