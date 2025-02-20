﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ChampSelectHelperApp
{
    class SpellInfo : Info
    {
        public int Id { get; set; }
        public string Name { get; private set; }
        public BitmapImage Icon { get; private set; }

        public SpellInfo(JObject spell)
        {
            Id = (int)spell["id"];
            Name = (string)spell["name"];

            string path = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default" 
                + ((string)spell["iconPath"]).ToLowerInvariant().Substring(21);
            Icon = HelperFunctions.CreateBitmapImage(path);
        }
    }
}
