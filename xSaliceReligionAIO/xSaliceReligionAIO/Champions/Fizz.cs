using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace xSaliceReligionAIO.Champions
{
    class Fizz : Champion
    {
        public Fizz()
        {
            //Set up mana
            //Q
            qMana = new[] { 55, 55, 60, 65, 70, 75 };
            //W
            wMana = new[] { 50, 50, 50, 50, 50, 50 };
            //E
            eMana = new[] { 85, 85, 85, 85, 85, 85 };
            //R
            rMana = new[] { 100, 100, 100, 100 };

            LoadSpells();
            LoadMenu();
        }

        private void LoadSpells()
        {
            
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
                    qMenu.AddItem(new MenuItem("Q_Auto", "Auto Q Toggle", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));
                    qMenu.AddItem(new MenuItem("Q_Auto_third", "Use 3rd Q in Auto Q", true).SetValue(true));
                    spellMenu.AddSubMenu(qMenu);
                }

                var eMenu = new Menu("EMenu", "EMenu");
                {
                    eMenu.AddItem(new MenuItem("E_Min_Dist", "Min Distance to use E", true).SetValue(new Slider(250, 1, 475)));
                    //e Evade
                    var dangerous = new Menu("Dodge Spells", "Dodge Spells");
                    {
                        foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy))
                        {
                            dangerous.AddSubMenu(new Menu(hero.ChampionName, hero.ChampionName));

                            var q = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.Q);
                            if (q != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(q.MissileSpellName + "E", q.MissileSpellName, true).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.Q).Name + "E", hero.Spellbook.GetSpell(SpellSlot.Q).Name, true).SetValue(false));

                            var w = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.W);
                            if (w != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(w.MissileSpellName + "E", w.MissileSpellName, true).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.W).Name + "E", hero.Spellbook.GetSpell(SpellSlot.W).Name, true).SetValue(false));

                            var e = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.E);
                            if (e != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(e.MissileSpellName + "E", e.MissileSpellName, true).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.E).Name + "E", hero.Spellbook.GetSpell(SpellSlot.E).Name, true).SetValue(false));

                            var r = SpellDatabase.Spells.FirstOrDefault(x => x.ChampionName == hero.ChampionName && x.Slot == SpellSlot.R);
                            if (r != null)
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(r.MissileSpellName + "E", r.MissileSpellName, true).SetValue(false));
                            else
                                dangerous.SubMenu(hero.ChampionName).AddItem(new MenuItem(hero.Spellbook.GetSpell(SpellSlot.R).Name + "E", hero.Spellbook.GetSpell(SpellSlot.R).Name, true).SetValue(false));
                        }
                        eMenu.AddSubMenu(dangerous);
                    }
                    spellMenu.AddSubMenu(eMenu);
                }

                var rMenu = new Menu("RMenu", "RMenu");
                {
                    rMenu.AddItem(new MenuItem("R_If_Killable", "R If Enemy Is killable", true).SetValue(true));
                    rMenu.AddItem(new MenuItem("rOnQ", "Cast R during Q Only", true).SetValue(true));
                    spellMenu.AddSubMenu(rMenu);
                }
                //add to menu
                menu.AddSubMenu(spellMenu);
            }

            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("UseQCombo", "Use Q", true).SetValue(true));
                combo.AddItem(new MenuItem("qHit", "R HitChance", true).SetValue(new Slider(2, 1, 3)));
                combo.AddItem(new MenuItem("UseECombo", "Use E", true).SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R", true).SetValue(true));
                //add to menu
                menu.AddSubMenu(combo);
            }
            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q", true).SetValue(true));
                harass.AddItem(new MenuItem("qHit2", "Q HitChance", true).SetValue(new Slider(2, 1, 3)));
                harass.AddItem(new MenuItem("UseEHarass", "Use E", true).SetValue(true));
                //add to menu
                menu.AddSubMenu(harass);
            }
            var farm = new Menu("Farming", "Farming");
            {
                farm.AddItem(new MenuItem("UseQFarm", "Use Q Farm", true).SetValue(true));
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
                drawMenu.AddItem(new MenuItem("Draw_Q2", "Draw Q Extended", true).SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_E", "Draw E", true).SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R", "Draw R", true).SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_AutoQ", "Draw Modes", true).SetValue(true));

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

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R) * 3;

            damage = ActiveItems.CalcDamage(enemy, damage);

            return (float)damage;
        }
    }
}
