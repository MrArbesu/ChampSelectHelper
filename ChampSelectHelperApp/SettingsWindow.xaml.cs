﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Reflection;
using NETWORKLIST;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.ComponentModel;
using System.Net.Http;
using System.Windows.Controls.Primitives;

namespace ChampSelectHelperApp
{
    /// <summary>
    /// Lógica de interacción para SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private NetworkListManager networkListManager = new();

        private List<ChampInfo> champions;
        private List<PerkTreeInfo> perkTrees;
        private List<SpellInfo> spells;

        private Dictionary<int, ChampionSettings> settingsDict;

        private BitmapImage noChampImg = new BitmapImage(new Uri(@"pack://application:,,,/Resources/noChamp.png"));

        // if what is being saved are chromas, saves the skin of origin
        private List<int> saveSkinsOrChromas = new List<int>();
        private int saveOriginSkin = -1; // -1 means that skins are being saved

        private int[] saveStyles = new int[2] {-1,-1};
        private int[] savePerks = new int[9] {-1,-1,-1,-1,-1,-1,-1,-1,-1};
        private int[] saveSpells = new int[2] {-1,-1};

        public SettingsWindow()
        {
            InitializeComponent();

            Title = Program.APP_NAME + " v" + Program.APP_VERSION;

            settingsDict = JsonConvert.DeserializeObject<Dictionary<int, ChampionSettings>>(FileHandler.GetFileContent("save.json"));
            if (settingsDict is null) settingsDict = new();
        }

        public async void InitializeWindow()
        {
            CheckConnectivity();

            string response = await App.httpclient.GetStringAsync(Program.PERKS_JSON_URL);
            JArray parsedPerks = JArray.Parse(response);
            CreatePerkTreeInfo(parsedPerks);
            response = await App.httpclient.GetStringAsync(Program.CHAMPIONS_JSON_URL);
            JObject parsedChampions = JObject.Parse(response);
            CreateChampInfo(parsedChampions);
            response = await App.httpclient.GetStringAsync(Program.SPELLS_JSON_URL);
            JArray parsedSpells = JArray.Parse(response);
            CreateSpellInfo(parsedSpells);

            InitializeElements();
        }

        private void CheckConnectivity()
        {
            if (networkListManager.IsConnectedToInternet) return;

            var result = MessageBox.Show("There was a problem while trying to connect to the internet. Check your internet " +
                "connection.\n\nDo you want to retry?", "Connecction Error", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                CheckConnectivity();
            }
            else
            {
                Close();
            }
        }

        private void CreateChampInfo(JObject champJObject)
        {
            champions = new List<ChampInfo>(champJObject.Count);
            foreach (var champion in champJObject)
            {
                JObject value = (JObject)champion.Value;
                ChampInfo champInfo = new ChampInfo(value);
                champions.Add(champInfo);
            }
        }

        private void CreatePerkTreeInfo(JArray perkTreeJArray)
        {
            perkTrees = new List<PerkTreeInfo>(5);
            Task[] tasks = new Task[5];
            int i = 0;
            foreach (JObject perkTree in perkTreeJArray)
            {
                PerkTreeInfo perkTreeInfo = new PerkTreeInfo();
                perkTrees.Add(perkTreeInfo);
                tasks[i] = Task.Run(() => perkTreeInfo.CreatePerkTree(perkTree));
                i++;
            }
            Task.WaitAll(tasks);
            perkTrees.Sort((x, y) => x.Id.CompareTo(y.Id));
        }

        private void CreateSpellInfo(JArray spellJArray)
        {
            spells = new List<SpellInfo>();
            foreach (JObject spell in spellJArray)
            {
                JArray gameModes = (JArray)spell["gameModes"];
                foreach (string gameMode in gameModes)
                {
                    if (gameMode == "CLASSIC")
                    {
                        spells.Add(new SpellInfo(spell));
                        break;
                    }
                }
            }
        }

        private void InitializeElements()
        {
            //always use after having created champInfo
            foreach (ChampInfo champion in champions)
            {
                championComboBox.Items.Add(champion.Name);
            }

            foreach (SpellInfo spell in spells)
            {
                spell1ComboBox.Items.Add(spell.Name);
                spell2ComboBox.Items.Add(spell.Name);
            }

            championImage.Source = noChampImg;

            //Display elements when the loading has ended
            championComboBox.Visibility = Visibility.Visible;
            championImage.Visibility = Visibility.Visible;
        }

        private void SaveChampion()
        {
            if (saveSkinsOrChromas.Count == 0 && savePerks[0] == -1 && saveSpells[0] == -1)
            {
                settingsDict.Remove(champions[championComboBox.SelectedIndex].Id);
            }
            else
            {
                ChampionSettings settings = new ChampionSettings(
                               champions[championComboBox.SelectedIndex].Id,
                               saveStyles,
                               savePerks,
                               saveSpells,
                               saveSkinsOrChromas,
                               saveOriginSkin
                               );
                settingsDict[settings.id] = settings;
            }

            // save in file
            string json = JsonConvert.SerializeObject(settingsDict);
            FileHandler.SaveInFile("save.json", json, false);
        }

        private void championComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            skinComboBox.Items.Clear();

            if (championComboBox.SelectedIndex == -1)
            {
                championImage.Source = noChampImg;
                skinCheckBox.IsChecked = false;
                perksCheckBox.IsChecked = false;
                spellsCheckBox.IsChecked = false;
                skinCheckBox.IsEnabled = false;
                perksCheckBox.IsEnabled = false;
                spellsCheckBox.IsEnabled = false;
                return;

            }
            
            championImage.Source = champions[championComboBox.SelectedIndex].Icon;
            foreach (SkinInfo skin in champions[championComboBox.SelectedIndex].Skins)
            {
                skinComboBox.Items.Add(skin.Name);
            }
            skinCheckBox.IsEnabled = true;
            perksCheckBox.IsEnabled = true;
            spellsCheckBox.IsEnabled = true;

            // set all the controls and save variables to the saved settings of the champion
            ChampInfo champion = champions[championComboBox.SelectedIndex];
            if (!settingsDict.TryGetValue(champion.Id, out ChampionSettings settings))
            {
                saveSkinsOrChromas = new List<int>();
                saveOriginSkin = -1;
                saveStyles = new int[2] {-1,-1};
                savePerks = new int[9] {-1,-1,-1,-1,-1,-1,-1,-1,-1};
                saveSpells = new int[2] {-1,-1};

                skinCheckBox.IsChecked = false;
                perksCheckBox.IsChecked = false;
                spellsCheckBox.IsChecked = false;
                return;
            }
            
            if (settings.skinsOrChromasId.Count != 0)
            {
                skinCheckBox.IsChecked = true;

                if (settings.originSkinId == -1)
                {
                    if (settings.skinsOrChromasId.Count == 1)
                    {
                        skinComboBox.SelectedIndex = IndexOfId(champion.Skins, settings.skinsOrChromasId[0]);
                    }
                    else skinRndmCheckBox.IsChecked = true;
                }
                else
                {
                    skinComboBox.SelectedIndex = IndexOfId(champion.Skins, settings.originSkinId);

                    chromaCheckBox.IsChecked = true;

                    if (settings.skinsOrChromasId.Count == 1)
                    {
                        chromaComboBox.SelectedIndex = 
                            IndexOfId(champion.Skins[skinComboBox.SelectedIndex].Chromas, settings.skinsOrChromasId[0]);
                    }
                    else chromaRndmCheckBox.IsChecked = true;
                }
            }
            else skinCheckBox.IsChecked = false;

            if (settings.perksId[0] != -1)
            {
                perksCheckBox.IsChecked = true;

                ChangeSubStyle(IndexOfId(perkTrees, settings.stylesId[1]));
                ChangePrimaryStyle(IndexOfId(perkTrees, settings.stylesId[0]));

                SelectPrimaryPerkImage(0, settings.stylesId[0], settings.perksId, keyStonesItemsControl);
                SelectPrimaryPerkImage(1, settings.stylesId[0], settings.perksId, primarySlot1ItemsControl);
                SelectPrimaryPerkImage(2, settings.stylesId[0], settings.perksId, primarySlot2ItemsControl);
                SelectPrimaryPerkImage(3, settings.stylesId[0], settings.perksId, primarySlot3ItemsControl);
                SelectSubPerkImage(settings.stylesId[1], settings.perksId);
                SelectMinorPerkImage(settings.perksId[6], offensivePerkGrid);
                SelectMinorPerkImage(settings.perksId[7], flexPerkGrid);
                SelectMinorPerkImage(settings.perksId[8], defensivePerkGrid);
            }
            else perksCheckBox.IsChecked = false;

            if (settings.spellsId[0] != -1)
            {
                spellsCheckBox.IsChecked = true;

                spell1ComboBox.SelectedIndex = IndexOfId(spells, settings.spellsId[0]);
                spell2ComboBox.SelectedIndex = IndexOfId(spells, settings.spellsId[1]);
            }
            else spellsCheckBox.IsChecked = false;

            saveSkinsOrChromas = settings.skinsOrChromasId;
            saveOriginSkin = settings.originSkinId;
            saveStyles = settings.stylesId;
            savePerks = settings.perksId;
            saveSpells = settings.spellsId;
        }

        private int IndexOfId(IEnumerable<Info> info, int id)
        {
            for (int i = 0; i < info.Count(); i++)
            {
                if (info.ElementAt(i).Id == id)
                {
                    return i;
                }
            }
            return -1;
        }

        private void SelectPrimaryPerkImage(int save, int styleId, int[] savedPerks, ItemsControl itemsControl)
        {
            int index = IndexOfId(perkTrees[IndexOfId(perkTrees, styleId)].Slots[save], savedPerks[save]);
            itemsControl.UpdateLayout();
            Image image = (Image)((Border)VisualTreeHelper.GetChild(
                itemsControl.ItemContainerGenerator.ContainerFromIndex(index), 0)).Child;
            ChangeSlotPerkItemsControl(itemsControl, image);
        }

        private void SelectSubPerkImage(int styleId, int[] savedPerks)
        {
            ItemsControl[] itemsControls = new ItemsControl[3] { subSlot1ItemsControl, subSlot2ItemsControl, subSlot3ItemsControl };
            for (int i = 1; i < 4; i++)
            {
                int index1 = IndexOfId(perkTrees[IndexOfId(perkTrees, styleId)].Slots[i], savedPerks[4]);
                int index2 = IndexOfId(perkTrees[IndexOfId(perkTrees, styleId)].Slots[i], savedPerks[5]);
                if (index1 != -1)
                {
                    itemsControls[i - 1].UpdateLayout();
                    Image image = (Image)((Border)VisualTreeHelper.GetChild(
                    itemsControls[i - 1].ItemContainerGenerator.ContainerFromIndex(index1), 0)).Child;
                    ChangeSlotPerkItemsControl(itemsControls[i - 1], image);
                }
                else if (index2 != -1)
                {
                    itemsControls[i - 1].UpdateLayout();
                    Image image = (Image)((Border)VisualTreeHelper.GetChild(
                    itemsControls[i - 1].ItemContainerGenerator.ContainerFromIndex(index2), 0)).Child;
                    ChangeSlotPerkItemsControl(itemsControls[i - 1], image);
                }
                else ChangeSlotPerkItemsControl(itemsControls[i - 1], null);
            }
        }

        private void SelectMinorPerkImage(int id, UniformGrid grid)
        {
            foreach (Border border in grid.Children)
            {
                Image image = (Image)border.Child;
                int tag = int.Parse((string)image.Tag);
                if (tag == id)
                {
                    ChangeMinorPerkGrid(grid, image);
                    return;
                }
            }
        }

        private void skinCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (skinCheckBox.IsChecked == true)
            {
                skinComboBox.IsEnabled = true;
                skinRndmCheckBox.IsEnabled = true;
            }
            else
            {
                skinComboBox.SelectedIndex = -1;
                skinRndmCheckBox.IsChecked = false;
                chromaCheckBox.IsChecked = false;

                skinComboBox.IsEnabled = false;
                skinRndmCheckBox.IsEnabled = false;
                chromaCheckBox.IsEnabled = false;

                saveSkinsOrChromas = new();
                saveOriginSkin = -1;

                SaveChampion();
            }
        }

        private void skinComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chromaComboBox.Items.Clear();
            chromaCheckBox.IsChecked = false;

            if (skinComboBox.SelectedIndex == -1)
            {
                skinImage.Source = null;
                return;
            }
            
            CheckConnectivity();
            skinImage.Source = new BitmapImage(new Uri(champions[championComboBox.SelectedIndex].Skins[skinComboBox.SelectedIndex].IconUri));

            List<ChromaInfo>? chromas = champions[championComboBox.SelectedIndex].Skins[skinComboBox.SelectedIndex].Chromas;
            if (chromas is null)
            {
                chromaCheckBox.IsEnabled = false;
            }
            else
            {
                foreach (ChromaInfo chroma in chromas)
                {
                    chromaComboBox.Items.Add(chroma.Name);
                }
                chromaCheckBox.IsEnabled = true;
            }

            saveOriginSkin = -1;
            saveSkinsOrChromas = new() { champions[championComboBox.SelectedIndex].Skins[skinComboBox.SelectedIndex].Id };

            SaveChampion();
        }

        private void skinRndmCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (skinRndmCheckBox.IsChecked == true)
            {
                skinComboBox.SelectedIndex = -1;
                chromaCheckBox.IsChecked = false;
                skinComboBox.IsEnabled = false;
                chromaCheckBox.IsEnabled = false;
                skinRndmDialogButton.IsEnabled = true;
            }
            else
            {
                skinComboBox.IsEnabled = true;
                skinRndmDialogButton.IsEnabled = false;
            }
        }

        private void chromaCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (chromaCheckBox.IsChecked == true)
            {
                chromaComboBox.IsEnabled = true;
                chromaRndmCheckBox.IsEnabled = true;
            }
            else
            {
                chromaComboBox.SelectedIndex = -1;
                chromaRndmCheckBox.IsChecked = false;
                chromaComboBox.IsEnabled = false;
                chromaRndmCheckBox.IsEnabled = false;

                if (saveOriginSkin != -1 && skinComboBox.SelectedIndex != -1)
                {
                    saveSkinsOrChromas = new() { saveOriginSkin };
                    saveOriginSkin = -1;

                    SaveChampion();
                }
            }
        }

        private void chromaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (chromaComboBox.SelectedIndex != -1)
            {
                SkinInfo skin = champions[championComboBox.SelectedIndex].Skins[skinComboBox.SelectedIndex];
                saveOriginSkin = skin.Id;
                saveSkinsOrChromas = new() { skin.Chromas[chromaComboBox.SelectedIndex].Id };

                SaveChampion();
            }
        }

        private void chromaRndmCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (chromaRndmCheckBox.IsChecked == true)
            {
                chromaComboBox.SelectedIndex = -1;
                chromaComboBox.IsEnabled = false;
                chromaRndmDialogButton.IsEnabled = true;
            }
            else
            {
                chromaComboBox.IsEnabled = true;
                chromaRndmDialogButton.IsEnabled = false;
            }
        }

        private void skinRndmDialogButton_Click(object sender, RoutedEventArgs e)
        {
            List<CheckBoxListItem> checkBoxList = new();

            ChampInfo champion = champions[championComboBox.SelectedIndex];
            foreach (SkinInfo skin in champion.Skins)
            {
                checkBoxList.Add(new CheckBoxListItem(saveSkinsOrChromas.Contains(skin.Id), skin.Name, skin.Id));
            }

            new CheckBoxListWindow(this, checkBoxList, "Random Skins Pool").ShowDialog();

            //save it
            List<int> selected = GetSelectedIds(checkBoxList);
            if (selected.Count == 0)
            {
                MessageBox.Show("No elements were selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (selected.Count == 1)
            {
                skinRndmCheckBox.IsChecked = false;
                skinComboBox.SelectedIndex = IndexOfId(champion.Skins, selected[0]);
            }
            else
            {
                saveOriginSkin = -1;
                saveSkinsOrChromas = selected;

                SaveChampion();
            }
        }

        private void chromaRndmDialogButton_Click(object sender, RoutedEventArgs e)
        {
            List<CheckBoxListItem> checkBoxList = new();
            SkinInfo skin = champions[championComboBox.SelectedIndex].Skins[skinComboBox.SelectedIndex];
            checkBoxList.Add(new CheckBoxListItem(saveSkinsOrChromas.Contains(skin.Id) && saveOriginSkin == skin.Id, "No Chroma", skin.Id));

            foreach (ChromaInfo chroma in skin.Chromas)
            {
                checkBoxList.Add(new CheckBoxListItem(saveSkinsOrChromas.Contains(chroma.Id), chroma.Name, chroma.Id));
            }

            new CheckBoxListWindow(this, checkBoxList, "Random Chromas Pool").ShowDialog();

            //save it
            List<int> selectedIds = GetSelectedIds(checkBoxList);
            if (selectedIds.Count == 0)
            {
                MessageBox.Show("No elements were selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (selectedIds.Count == 1)
            {
                if (selectedIds[0] == skin.Id)
                {
                    chromaCheckBox.IsChecked = false;
                }
                else
                {
                    chromaRndmCheckBox.IsChecked = false;
                    chromaComboBox.SelectedIndex = IndexOfId(skin.Chromas, selectedIds[0]);
                }
            }
            else
            {
                saveOriginSkin = skin.Id;
                saveSkinsOrChromas = selectedIds;

                SaveChampion();
            }
        }

        private List<int> GetSelectedIds(List<CheckBoxListItem> checkBoxList)
        {
            List<int> ids = new List<int>();
            foreach (CheckBoxListItem checkBoxItem in checkBoxList)
            {
                if (checkBoxItem.IsChecked)
                {
                    ids.Add(checkBoxItem.Id);
                }
            }
            return ids;
        }

        private void perksCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (perksCheckBox.IsChecked == true)
            {
                perksGrid.Visibility = Visibility.Visible;
            }
            else
            {
                ChangePrimaryStyle(-1);
                perksGrid.Visibility = Visibility.Hidden;

                ChangeMinorPerkGrid(offensivePerkGrid, null);
                ChangeMinorPerkGrid(flexPerkGrid, null);
                ChangeMinorPerkGrid(defensivePerkGrid, null);

                savePerks[6] = -1;
                savePerks[7] = -1;
                savePerks[8] = -1;

                SaveChampion();
            }
        }

        private void primaryStyle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChangePrimaryStyle(int.Parse((string)((Image)sender).Tag));
        }

        private void subStyle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChangeSubStyle(int.Parse((string)((Image)sender).Tag));
        }

        private void ChangePrimaryStyle(int index)
        {
            if (index == -1)
            {
                ChangeStyleGrid(primaryStyleGrid, -1);

                keyStonesItemsControl.ItemsSource = null;
                primarySlot1ItemsControl.ItemsSource = null;
                primarySlot2ItemsControl.ItemsSource = null;
                primarySlot3ItemsControl.ItemsSource = null;

                subStyleGrid.Visibility = Visibility.Hidden;
                ChangeSubStyle(-1);

                if (saveStyles[0] == -1) return;
                saveStyles[0] = -1;

                savePerks[0] = -1;
                savePerks[1] = -1;
                savePerks[2] = -1;
                savePerks[3] = -1;
                return;
            }

            ChangeStyleGrid(primaryStyleGrid, index);

            for (int i = 0; i < subStyleGrid.Children.Count; i++)
            {
                Border border = (Border)subStyleGrid.Children[i];
                if (i == index)
                {
                    border.Visibility = Visibility.Collapsed;
                }
                else 
                {
                    border.Visibility = Visibility.Visible;
                    if (perkTrees[i].Id == saveStyles[1]) 
                    {
                        border.BorderBrush = Brushes.White;
                    }
                    else
                    {
                        border.BorderBrush = Brushes.Transparent;
                    }
                }
            }

            if (perkTrees[index].Id == saveStyles[1]) ChangeSubStyle(-1);

            subStyleGrid.Visibility = Visibility.Visible;
            
            keyStonesItemsControl.ItemsSource = perkTrees[index].Slots[0];
            primarySlot1ItemsControl.ItemsSource = perkTrees[index].Slots[1];
            primarySlot2ItemsControl.ItemsSource = perkTrees[index].Slots[2];
            primarySlot3ItemsControl.ItemsSource = perkTrees[index].Slots[3];

            if (saveStyles[0] == perkTrees[index].Id) return;
            saveStyles[0] = perkTrees[index].Id;

            savePerks[0] = -1;
            savePerks[1] = -1;
            savePerks[2] = -1;
            savePerks[3] = -1;
        }

        private void ChangeSubStyle(int index)
        {
            if (index == -1)
            {
                ChangeStyleGrid(subStyleGrid, -1);

                subSlot1ItemsControl.ItemsSource = null;
                subSlot2ItemsControl.ItemsSource = null;
                subSlot3ItemsControl.ItemsSource = null;
                
                if (saveStyles[1] == -1) return;
                saveStyles[1] = -1;

                savePerks[4] = -1;
                savePerks[5] = -1;
                return;
            }

            ChangeStyleGrid(subStyleGrid, index);

            subSlot1ItemsControl.ItemsSource = perkTrees[index].Slots[1];
            subSlot2ItemsControl.ItemsSource = perkTrees[index].Slots[2];
            subSlot3ItemsControl.ItemsSource = perkTrees[index].Slots[3];

            if (saveStyles[1] == perkTrees[index].Id) return;
            saveStyles[1] = perkTrees[index].Id;

            savePerks[4] = -1;
            savePerks[5] = -1;
        }

        private void ChangeStyleGrid(UniformGrid grid, int index)
        {
            foreach (Border border in grid.Children)
            {
                int tag = int.Parse((string)((Image)border.Child).Tag);
                if (tag == index)
                {
                    border.BorderBrush = Brushes.White;
                }
                else
                {
                    border.BorderBrush = Brushes.Transparent;
                }
            }
        }

        private void keyStonesImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int id = (int)((Image)sender).Tag;
            if (savePerks[0] == id) return;
            savePerks[0] = id;

            SavePerks();

            ChangeSlotPerkItemsControl(keyStonesItemsControl, sender);
        }

        private void primarySlot1Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int id = (int)((Image)sender).Tag;
            if (savePerks[1] == id) return;
            savePerks[1] = id;

            SavePerks();

            ChangeSlotPerkItemsControl(primarySlot1ItemsControl, sender);
        }

        private void primarySlot2Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int id = (int)((Image)sender).Tag;
            if (savePerks[2] == id) return;
            savePerks[2] = id;

            SavePerks();

            ChangeSlotPerkItemsControl(primarySlot2ItemsControl, sender);
        }

        private void primarySlot3Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int id = (int)((Image)sender).Tag;
            if (savePerks[3] == id) return;
            savePerks[3] = id;

            SavePerks();

            ChangeSlotPerkItemsControl(primarySlot3ItemsControl, sender);
        }

        private void subSlot1Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChangeSubSlotPerkItemsControl(subSlot1ItemsControl, sender);
        }

        private void subSlot2Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChangeSubSlotPerkItemsControl(subSlot2ItemsControl, sender);
        }

        private void subSlot3Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChangeSubSlotPerkItemsControl(subSlot3ItemsControl, sender);
        }

        private void ChangeSubSlotPerkItemsControl(ItemsControl itemsControl, object sender)
        {
            bool present = false;
            itemsControl.UpdateLayout();
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                Border border = (Border)VisualTreeHelper.GetChild(itemsControl.ItemContainerGenerator.ContainerFromIndex(i), 0);
                Image image = (Image)border.Child;
                int id = (int)image.Tag;
                if (savePerks[4] == id)
                {
                    (savePerks[4], savePerks[5]) = (savePerks[5], savePerks[4]);
                    present = true;
                    break;
                }
                else if (savePerks[5] == id)
                {
                    present = true;
                    break;
                }
            }

            if (present)
            {
                int idSelected = (int)((Image)sender).Tag;
                if (savePerks[5] == idSelected) return;
                savePerks[5] = idSelected;

                SavePerks();

                ChangeSlotPerkItemsControl(itemsControl, sender);
                return;
            }

            if (savePerks[5] != -1)
            {
                List<ItemsControl> itemsControls = new List<ItemsControl>() { subSlot1ItemsControl, subSlot2ItemsControl, subSlot3ItemsControl };
                itemsControls.Remove(itemsControl);
                bool changed = false;
                itemsControls[0].UpdateLayout();
                for (int i = 0; i < itemsControls[0].Items.Count; i++)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(itemsControls[0].ItemContainerGenerator.ContainerFromIndex(i), 0);
                    Image image = (Image)border.Child;
                    int id = (int)image.Tag;
                    if (savePerks[4] == id)
                    {
                        ChangeSlotPerkItemsControl(itemsControls[0], null);
                        changed = true;
                    }
                }

                if (!changed) ChangeSlotPerkItemsControl(itemsControls[1], null);
            }

            savePerks[4] = savePerks[5];
            savePerks[5] = (int)((Image)sender).Tag;

            SavePerks();

            ChangeSlotPerkItemsControl(itemsControl, sender);
        }

        private void ChangeSlotPerkItemsControl(ItemsControl itemsControl, object? sender)
        {
            itemsControl.UpdateLayout();
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                Border border = (Border)VisualTreeHelper.GetChild(itemsControl.ItemContainerGenerator.ContainerFromIndex(i), 0);
                Image image = (Image)border.Child;
                if (image == sender)
                {
                    image.Source = ((PerkInfo)image.DataContext).Icon;
                    border.BorderBrush = Brushes.White;
                }
                else
                {
                    image.Source = ((PerkInfo)image.DataContext).GrayIcon;
                    border.BorderBrush = Brushes.Transparent;
                }
            }
        }

        private void offensivePerkImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int id = int.Parse((string)((Image)sender).Tag);
            if (savePerks[6] == id) return;
            savePerks[6] = id;

            SavePerks();

            ChangeMinorPerkGrid(offensivePerkGrid, sender);
        }

        private void flexPerkImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int id = int.Parse((string)((Image)sender).Tag);
            if (savePerks[7] == id) return;
            savePerks[7] = id;

            SavePerks();

            ChangeMinorPerkGrid(flexPerkGrid, sender);
        }

        private void defensivePerkImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int id = int.Parse((string)((Image)sender).Tag);
            if (savePerks[8] == id) return;
            savePerks[8] = id;

            SavePerks();

            ChangeMinorPerkGrid(defensivePerkGrid, sender);
        }

        private void ChangeMinorPerkGrid(UniformGrid grid, object? sender)
        {
            foreach (Border border in grid.Children)
            {
                Image image = (Image)border.Child;
                if (image == sender)
                {
                    border.BorderBrush = Brushes.White;
                }
                else
                {
                    border.BorderBrush = Brushes.Transparent;
                }
            }
        }

        private void SavePerks()
        {
            for (int i = 0; i < savePerks.Length; i++)
            {
                if (savePerks[i] == -1) return;
            }

            SaveChampion();
        }

        private void spellsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (spellsCheckBox.IsChecked == true)
            {
                spell1ComboBox.IsEnabled = true;
                spell2ComboBox.IsEnabled = true;
            }
            else
            {
                spell1ComboBox.SelectedIndex = -1;
                spell2ComboBox.SelectedIndex = -1;
                spell1ComboBox.IsEnabled = false;
                spell2ComboBox.IsEnabled = false;

                SaveChampion();
            }
        }

        private void spell1ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (spell1ComboBox.SelectedIndex == -1)
            {
                spell1Image.Source = null;

                saveSpells[0] = -1;
            }
            else
            {
                spell1Image.Source = spells[spell1ComboBox.SelectedIndex].Icon;
                if (spell1ComboBox.SelectedIndex == spell2ComboBox.SelectedIndex) spell2ComboBox.SelectedIndex = -1;

                saveSpells[0] = spells[spell1ComboBox.SelectedIndex].Id;

                SaveSpells();
            }
        }

        private void spell2ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (spell2ComboBox.SelectedIndex == -1)
            {
                spell2Image.Source = null;

                saveSpells[1] = -1;
            }
            else
            {
                spell2Image.Source = spells[spell2ComboBox.SelectedIndex].Icon;
                if (spell1ComboBox.SelectedIndex == spell2ComboBox.SelectedIndex) spell1ComboBox.SelectedIndex = -1;

                saveSpells[1] = spells[spell2ComboBox.SelectedIndex].Id;

                SaveSpells();
            }
        }

        private void SaveSpells()
        {
            if (saveSpells[0] != -1 && saveSpells[1] != -1)
            {
                SaveChampion();
            }
        }
    }
}
