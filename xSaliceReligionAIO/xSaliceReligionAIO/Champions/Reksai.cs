using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace xSaliceReligionAIO.Champions
{
    class Reksai : Champion
    {
        public Reksai()
        {
            LoadSpell();
            LoadMenu();
        }

        private void LoadSpell()
        {
            Q = new Spell(SpellSlot.Q, 1300);
            
            Q.SetSkillshot(.25f, 60f, 1400f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 250);

            R = new Spell(SpellSlot.R);
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
                combo.AddItem(new MenuItem("selected", "Focus Selected Target").SetValue(true));
                combo.AddItem(new MenuItem("UseQCombo", "Use Both Q").SetValue(true));
                combo.AddItem(new MenuItem("Q_Reset", "Q Auto-Attack Reset").SetValue(true));
                combo.AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
                combo.AddItem(new MenuItem("UseECombo", "Use Unbarrowed E").SetValue(true));
                combo.AddItem(new MenuItem("Ignite", "Use Ignite").SetValue(true));
                combo.AddItem(new MenuItem("Use_Item", "Use Items").SetValue(true));
                //add to menu
                menu.AddSubMenu(combo);
            }

            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
                harass.AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
                harass.AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
                //add to menu
                menu.AddSubMenu(harass);
            }

            var farm = new Menu("LaneClear", "LaneClear");
            {
                farm.AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
                farm.AddItem(new MenuItem("UseWFarm", "Use W").SetValue(true));
                farm.AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
                //add to menu
                menu.AddSubMenu(farm);
            }

            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
                drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_CD", "Draw Cool Down").SetValue(true));

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
                comboDamage += Player.GetSpellDamage(target, SpellSlot.Q);

            if (W.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.W);

            if (E.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.E);

            if (Ignite_Ready())
                comboDamage += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            return (float)(comboDamage + Player.GetAutoAttackDamage(target));
        }

        private void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>());
        }

        private void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(),
                menu.Item("UseEHarass").GetValue<bool>());
        }

        private void UseSpells(bool useQ, bool useW, bool useE)
        {
            if (_barrowed)
            {
                var target = TargetSelector.GetTarget(500, TargetSelector.DamageType.Physical);
                useQ = useQ && !menu.Item("Q_Reset").GetValue<bool>() && Q.IsReady();

                if (target == null)
                    return;

                if (useQ && Player.Distance(target) < Player.AttackRange + target.BoundingRadius)
                    Q.Cast(packets());

                if(useE && E.IsReady())
                    E.CastOnUnit(target, packets());
            }
            else
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

                if (useQ && Q.IsReady())
                    Q.Cast(target, packets());
            }

            if(useW)
                Cast_W();
        }

        private void Cast_W()
        {
            if (!W.IsReady())
                return;

            if (_barrowed)
            {
                var target = TargetSelector.GetTarget(250, TargetSelector.DamageType.Physical);

                if (target == null)
                    return;

                if (_unbarrowedQcd <= 0 && _unbarrowedEcd <= 0 && _barrowQcd > 0)
                    W.Cast();
            }
            else
            {
                if (_barrowQcd <= 0)
                    W.Cast(packets());
            }
        }

        public override void AfterAttack(AttackableUnit unit, AttackableUnit mytarget)
        {
            var target = (Obj_AI_Base)mytarget;

            if (unit.IsMe)
            {
                if ((menu.Item("ComboActive").GetValue<KeyBind>().Active || menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    && (target is Obj_AI_Hero))
                {

                    if (menu.Item("Botrk").GetValue<bool>())
                    {
                        if (GetComboDamage(target) > target.Health && !target.HasBuffOfType(BuffType.Slow))
                        {
                            Use_Bilge((Obj_AI_Hero)target);
                            Use_Botrk((Obj_AI_Hero)target);
                        }

                        if (Items.CanUseItem(3077))
                            Items.UseItem(3077);
                        if (Items.CanUseItem(3074))
                            Items.UseItem(3074);
                    }

                    var useQ = menu.Item("UseQCombo").GetValue<bool>() && menu.Item("Q_Reset").GetValue<bool>();

                    if (useQ && !_barrowed)
                        Q.Cast(packets());
                }
            }
        }

        private void Farm()
        {
            List<Obj_AI_Base> allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Player.AttackRange + Player.BoundingRadius, MinionTypes.All, MinionTeam.NotAlly);
            List<Obj_AI_Base> allMinionsQ2 = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            List<Obj_AI_Base> allMinionsE = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useW = menu.Item("UseWFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (_barrowed)
            {
                if (useQ && allMinionsQ2.Count > 0 && Q.IsReady())
                {
                    var pred = Q.GetCircularFarmLocation(allMinionsQ2);
                    Q.Cast(pred.Position);
                }
                    
            }
            else
            {
                if (useQ && allMinionsQ.Count > 0 && Q.IsReady())
                    Q.Cast();
                if (useE && allMinionsE.Count > 0)
                    E.CastOnUnit(allMinionsE[0]);
            }

            if (useW)
            {
                if (!_barrowed)
                {
                    if (_barrowQcd <= 0)
                        W.Cast();
                }
            }
        }

        #region cooldown
        private bool _barrowed;
        private static readonly float[] Qcd = { 4, 4, 4, 4, 4 };
        private static readonly float[] Wcd = { 4, 4, 4, 4, 4 };
        private static readonly float[] Ecd = { 12, 12, 12, 12, 12 };

        private static readonly float[] Q2Cd = { 11, 10, 9, 8, 7 };
        private static readonly float[] W2Cd = { 1, 1, 1, 1, 1 };
        private static readonly float[] E2Cd = { 20, 19.5f, 19, 18.5f, 18 };

        private static float _unbarrowedQcd, _unbarrowedWcd, _unbarrowedEcd ;
        private static float _barrowQcd , _barrowWcd , _barrowEcd ;
        private static float _unbarrowedQcdRem, _unbarrowedWcdRem, _unbarrowedEcdRem;
        private static float _barrowQcdRem, _barrowWcdRem, _barrowEcdRem;

        private void ProcessCooldowns()
        {
            _unbarrowedQcd = ((_unbarrowedQcdRem - Game.Time) > 0) ? (_unbarrowedQcdRem - Game.Time) : 0;
            _unbarrowedWcd = ((_unbarrowedWcdRem - Game.Time) > 0) ? (_unbarrowedWcdRem - Game.Time) : 0;
            _unbarrowedEcd = ((_unbarrowedEcdRem - Game.Time) > 0) ? (_unbarrowedEcdRem - Game.Time) : 0;
            _barrowQcd = ((_barrowQcdRem - Game.Time) > 0) ? (_barrowQcdRem - Game.Time) : 0;
            _barrowWcd = ((_barrowWcdRem - Game.Time) > 0) ? (_barrowWcdRem - Game.Time) : 0;
            _barrowEcd = ((_barrowEcdRem - Game.Time) > 0) ? (_barrowEcdRem - Game.Time) : 0;
        }

        private float CalculateCd(float time)
        {
            return time + (time * Player.PercentCooldownMod);
        }

        private void GetCooldowns(GameObjectProcessSpellCastEventArgs spell)
        {
            SpellSlot castedSlot = Player.GetSpellSlot(spell.SData.Name, false);

            if (_barrowed)
            {
                if (castedSlot == SpellSlot.Q)
                    _barrowQcdRem = Game.Time + CalculateCd(Qcd[Q.Level - 1]);
                if (castedSlot == SpellSlot.W)
                    _barrowWcdRem = Game.Time + CalculateCd(Wcd[W.Level - 1]);
                if (castedSlot == SpellSlot.E)
                    _barrowEcdRem = Game.Time + CalculateCd(Ecd[E.Level - 1]);
            }
            else
            {

                if (castedSlot == SpellSlot.Q)
                    _unbarrowedQcdRem = Game.Time + CalculateCd(Q2Cd[Q.Level - 1]);
                if (castedSlot == SpellSlot.W)
                    _unbarrowedWcdRem = Game.Time + CalculateCd(W2Cd[W.Level - 1]);
                if (castedSlot == SpellSlot.E)
                    _unbarrowedEcdRem = Game.Time + CalculateCd(E2Cd[E.Level - 1]);
            }
        }

        #endregion cooldown

        public override void Game_OnGameUpdate(EventArgs args)
        {
            //process dat cd
            ProcessCooldowns();

            _barrowed = Player.HasBuff("barrow");

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
            }
        }

        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            GetCooldowns(args);
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

            if (menu.Item("Draw_CD").GetValue<bool>())
            {
                var wts = Drawing.WorldToScreen(Player.Position);
                if (_barrowed) // lets show cooldown timers for the opposite form :)
                {

                    if (_unbarrowedQcd <= 0)
                        Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q Ready");
                    else
                        Drawing.DrawText(wts[0] - 80, wts[1], Color.Orange, "Q: " + _unbarrowedQcd.ToString("0.0"));
                    if (_unbarrowedWcd <= 0)
                        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W Ready");
                    else
                        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.Orange, "W: " + _unbarrowedWcd.ToString("0.0"));
                    if (_unbarrowedEcd <= 0)
                        Drawing.DrawText(wts[0], wts[1], Color.White, "E Ready");
                    else
                        Drawing.DrawText(wts[0], wts[1], Color.Orange, "E: " + _unbarrowedEcd.ToString("0.0"));

                }
                else
                {
                    if (_barrowQcd <= 0)
                        Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q Ready");
                    else
                        Drawing.DrawText(wts[0] - 80, wts[1], Color.Orange, "Q: " + _barrowQcd.ToString("0.0"));
                    if (_barrowWcd <= 0)
                        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W Ready");
                    else
                        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.Orange, "W: " + _barrowWcd.ToString("0.0"));
                    if (_barrowEcd <= 0)
                        Drawing.DrawText(wts[0], wts[1], Color.White, "E Ready");
                    else
                        Drawing.DrawText(wts[0], wts[1], Color.Orange, "E: " + _barrowEcd.ToString("0.0"));
                }
            }
        }
    }
}
