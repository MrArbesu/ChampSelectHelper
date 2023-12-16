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

namespace ChampSelectHelperApp
{
    /// <summary>
    /// Lógica de interacción para SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly NetworkListManager networkListManager;

        private ChampInfo[] champInfo;
        private Dictionary<int, ChampionSettings> champSettDict = new();

        private BitmapImage noChampImg;

        public SettingsWindow()
        {
            InitializeComponent();

            networkListManager = new NetworkListManager();

            Title = Program.APP_NAME + " v" + Program.APP_VERSION;

            noChampImg = new BitmapImage(new Uri(@"pack://application:,,,/" + Assembly.GetExecutingAssembly().GetName().Name + ";component/Resources/noChamp.png"));

            skinCheckBox.IsEnabled = false;
            skinComboBox.IsEnabled = false;
            skinRndmCheckBox.IsEnabled = false;
            skinImage.IsEnabled = false;

            chromaCheckBox.IsEnabled = false;
            chromaComboBox.IsEnabled = false;
            chromaRndmCheckBox.IsEnabled = false;
        }

        public void InitializeWindow()
        {
            CheckConnectivity();
            try
            {
                using (WebClient httpClient = new())
                {
                    string response = httpClient.DownloadString(Program.CHAMPIONS_JSON_URL);
                    JObject parsedResponse = JObject.Parse(response);
                    ChampInfoArrayParser(parsedResponse);
                    InitializeElements();
                    return;
                }
            }
            catch (WebException ex) 
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void InitializeElements()
        {
            //always use after having created champInfo
            foreach (ChampInfo info in champInfo!)
            {
                championComboBox.Items.Add(info.Name);
            }

            championImage.Source = noChampImg;

            championComboBox.Visibility = Visibility.Visible;
            championImage.Visibility = Visibility.Visible;
        }

        private void ChampInfoArrayParser(JObject champJObject)
        {
            champInfo = new ChampInfo[champJObject.Count];
            int i = 0;
            foreach (var champion in champJObject)
            {
                var value = champion.Value;
                string name = (string)value["name"];

                JArray skins = (JArray)value["skins"];
                SkinInfo[] skinInfo = new SkinInfo[skins.Count];
                int j = 0;
                foreach (JObject skin in skins)
                {
                    JArray chromas = (JArray)skin["chromas"];
                    ChromaInfo[]? chromaInfo = null;
                    if (chromas.HasValues)
                    {
                        if (chromas[0].Type == JTokenType.Object)
                        {
                            chromaInfo = new ChromaInfo[chromas.Count];
                            int k = 0;
                            foreach (JObject chroma in chromas)
                            {
                                chromaInfo[k] = new ChromaInfo((int)chroma["id"], (string)chroma["name"]);
                                k++;
                            }
                        }
                    }
                    skinInfo[j] = new SkinInfo((int)skin["id"], (string)skin["name"], (string)skin["uncenteredSplashPath"], chromaInfo);
                    j++;
                }
                champInfo[i] = new ChampInfo((int)value["id"], name, (string)value["icon"], skinInfo);
                i++;
            }
        }

        private void CheckConnectivity()
        {
            if (networkListManager.IsConnectedToInternet)
            {
                return;
            }
            var result = MessageBox.Show("There was a problem while trying to connect to the internet. Check your internet connection.\n\nDo you want to retry?", "Connecction Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
            if (result == MessageBoxResult.Yes)
            {
                CheckConnectivity();
            }
            else
            {
                Close();
            }
        }

        private void championComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            skinComboBox.Items.Clear();

            if (championComboBox.SelectedIndex == -1)
            {
                championImage.Source = noChampImg;
                skinCheckBox.IsChecked = false;
                skinCheckBox.IsEnabled = false;
            }
            else
            {
                championImage.Source = champInfo[championComboBox.SelectedIndex].Icon;
                foreach (SkinInfo skin in champInfo[championComboBox.SelectedIndex].Skins)
                {
                    skinComboBox.Items.Add(skin.Name);
                }
                skinCheckBox.IsChecked = false;
                skinCheckBox.IsEnabled = true;
            }
        }

        private void skinComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chromaComboBox.Items.Clear();

            if (skinComboBox.SelectedIndex == -1)
            {
                skinImage.Source = null;
            }
            else
            {
                CheckConnectivity();
                skinImage.Source = new BitmapImage(new Uri(champInfo[championComboBox.SelectedIndex].Skins[skinComboBox.SelectedIndex].IconUri));

                ChromaInfo[]? chromas = champInfo[championComboBox.SelectedIndex].Skins[skinComboBox.SelectedIndex].Chromas;
                if (chromas is null)
                {
                    chromaComboBox.IsEnabled = false;
                    chromaRndmCheckBox.IsEnabled = false;
                }
                else
                {
                    foreach (ChromaInfo chroma in chromas)
                    {
                        chromaComboBox.Items.Add(chroma.Name);
                    }
                    chromaCheckBox.IsEnabled = true;
                }
            }
        }

        private void skinCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (skinCheckBox.IsChecked == true)
            {
                skinComboBox.IsEnabled = true;
                skinRndmCheckBox.IsEnabled = true;
                skinImage.IsEnabled = true;
            }
            else
            {
                skinComboBox.SelectedIndex = -1;
                skinRndmCheckBox.IsChecked = false;
                chromaCheckBox.IsChecked = false;

                skinComboBox.IsEnabled = false;
                skinRndmCheckBox.IsEnabled = false;
                skinImage.IsEnabled = false;
                chromaCheckBox.IsEnabled = false;
            }
        }

        private void skinRndmCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (skinRndmCheckBox.IsChecked == true)
            {
                skinComboBox.SelectedIndex = -1;
                chromaCheckBox.IsChecked = false;
                skinComboBox.IsEnabled = false;
                chromaCheckBox.IsEnabled = true;
            }
            else
            {
                skinComboBox.IsEnabled = true;
                chromaCheckBox.IsChecked = false;
                chromaCheckBox.IsEnabled = false;
            }
        }

        private void chromaCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (chromaCheckBox.IsChecked != true)
            {
                chromaComboBox.SelectedIndex = -1;
                chromaRndmCheckBox.IsChecked = false;
                chromaComboBox.IsEnabled = false;
                chromaRndmCheckBox.IsEnabled = false;
            }
            else if (skinRndmCheckBox.IsChecked == true)
            {
                chromaComboBox.SelectedIndex = -1;
                chromaRndmCheckBox.IsChecked = true;
                chromaRndmCheckBox.IsEnabled = false;
            }
            else
            {
                chromaComboBox.IsEnabled = true;
                chromaRndmCheckBox.IsEnabled = true;
            }
        }

        private void chromaRndmCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (chromaRndmCheckBox.IsChecked == true)
            {
                chromaComboBox.SelectedIndex = -1;
                chromaComboBox.IsEnabled = false;
            }
            else
            {
                chromaComboBox.IsEnabled = true;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            new CheckBoxListWindow(new List<CheckBoxListItem>(), "Random Chromas").ShowDialog();
        }
    }
}
