using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.RemoteAgents;
using ff14bot.RemoteWindows;
using TreeSharp;

namespace Retainers
{
    public class RetainerSettings : JsonSettings
    {
        private static RetainerSettings _settings;

        private bool _deposit;

        private bool _debug;

        private bool _gil;

        private bool _merge;

        private int _numOfRetainers;


        public RetainerSettings() : base(Path.Combine(CharacterSettingsDirectory, "RetainerSettings.json"))
        {
        }

        public static RetainerSettings Instance => _settings ?? (_settings = new RetainerSettings());

        [Description("Entrust items to retainer if the have the same item?")]
        [DefaultValue(true)] //shift +x
        public bool DepositFromPlayer
        {
            get => _deposit;
            set
            {
                if (_deposit != value)
                {
                    _deposit = value;
                    Save();
                }
            }
        }

        [Description("Print verbose debug info")]
        [DefaultValue(false)] //shift +x
        public bool DebugLogging
        {
            get => _debug;
            set
            {
                if (_debug != value)
                {
                    _debug = value;
                    Save();
                }
            }
        }

        [Description("Withdrawal Gil from each retainer")]
        [DefaultValue(true)] //shift +x
        public bool GetGil
        {
            get => _gil;
            set
            {
                if (_gil != value)
                {
                    _gil = value;
                    Save();
                }
            }
        }


        [Description("Don't try and merge duplicate item stacks between retainers")]
        [DefaultValue(false)] //shift +x
        public bool DontOrganizeRetainers
        {
            get => _merge;
            set
            {
                if (_merge != value)
                {
                    _merge = value;
                    Save();
                }
            }
        }
    }

}