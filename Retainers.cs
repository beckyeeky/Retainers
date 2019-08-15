﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Pathing;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Pathing.Service_Navigation;
using TreeSharp;
using static ff14bot.RemoteWindows.Talk;


namespace Retainers
{
    public class Retainers : BotBase
    {
        private static readonly string botName = "Retainers Test";

        private SettingsForm settings;

        private bool debug;

        private static bool done;

        private Composite _root;


        public override string Name
        {
            get
            {
#if RB_CN
                return "栽培模式";
#else
                return "Retainers Test";
#endif
            }
        }

        public override bool WantButton => true;

        public override string EnglishName => "Retainers Test";

        public override PulseFlags PulseFlags => PulseFlags.All;

        public override bool RequiresProfile => false;

        public override Composite Root => _root;

        public override void OnButtonPress()
        {
            if (settings == null || settings.IsDisposed)
                settings = new SettingsForm();
            try
            {
                settings.Show();
                settings.Activate();
            }
            catch (ArgumentOutOfRangeException ee)
            {
            }
        }
        
        private void Log(string text, params object[] args)
        {
            var msg = string.Format("[" + botName + "] " + text, args);
            Logging.Write(Colors.Green, msg);
        }

        private void LogVerbose(string text, params object[] args)
        {
            if (!debug)
                return;
            var msg = string.Format("[" + botName + "] " + text, args);
            Logging.WriteVerbose(msg);
        }

        private void LogCritical(string text, params object[] args)
        {
            var msg = string.Format("[" + botName + "] " + text, args);
            Logging.Write(Colors.OrangeRed, msg);
        }

        public override void Start()
        {
            Navigator.PlayerMover = new SlideMover();
            Navigator.NavigationProvider = new ServiceNavigationProvider();
            _root = new ActionRunCoroutine(r => RetainerTest());
            done = false;
        }


        /*The await sleeps shouldn't be necessary but if they aren't there the game crashes some times since
        it tries to send commands to a window that isn't open even though it reports it as open (guess it didn't load yet)*/
        private async Task<bool> RetainerTest()
        {
            if (!done)
            {
                Log(" ");
                Log("==================================================");
                Log("====================Retainers=====================");
                Log("==================================================");
                Log(" ");

                //var retainerIndex = 0;

                //Settings variables
                debug = RetainerSettings.Instance.DebugLogging;
                int numRetainers = RetainerSettings.Instance.NumberOfRetainers;

                List<RetainerInventory> retList = new List<RetainerInventory>();
                List<KeyValuePair<uint,int>> moveToOrder = new List<KeyValuePair<uint, int>>();
                Dictionary<uint, List<KeyValuePair<int,uint>>> masterInventory = new Dictionary<uint, List<KeyValuePair<int, uint>>>();
                //       bool test = false;

                


                for (int retainerIndex = 0; retainerIndex < numRetainers; retainerIndex++)
                {
                    var inventory = new RetainerInventory();

                    if (!RetainerList.IsOpen) await UseSummoningBell();

                    await Coroutine.Wait(5000, () => RetainerList.IsOpen);

                    await Coroutine.Sleep(1000);

                    if (!RetainerList.IsOpen) Log("Failed opening retainer list");

                    LogVerbose("Open:" + RetainerList.IsOpen);

                    await RetainerList.SelectRetainer(retainerIndex);

                    Log("Selected Retainer: " + retainerIndex);


                    await Coroutine.Wait(5000, () => RetainerTasks.IsOpen);

                    RetainerTasks.OpenInventory();

                    await Coroutine.Sleep(500);

                    if (RetainerTasks.IsInventoryOpen())
                    {
                        LogVerbose("Inventory open");
                        foreach (var retbag in InventoryManager.GetBagsByInventoryBagId(RetainerTasks.RetainerBagIds))
                            foreach (BagSlot item in retbag.FilledSlots.Where(RetainerInventory.FilterStackable))
                                try
                                {
                                    inventory.AddItem(item);
                                    if (masterInventory.ContainsKey(item.TrueItemId))
                                    {
                                        masterInventory[item.TrueItemId].Add(new KeyValuePair<int, uint>(retainerIndex, item.Count));
                                    }
                                    else
                                    {
                                        masterInventory.Add(item.TrueItemId,new List<KeyValuePair<int, uint>>());
                                        masterInventory[item.TrueItemId].Add(new KeyValuePair<int, uint>(retainerIndex, item.Count));
                                    }
                                    //Logging.Write("Name: {0} Count: {1} BagId: {2} IsHQ: {3}", item.Item.EnglishName, item.Item.StackSize, item.BagId, item.Item.IsHighQuality);
                                }
                                catch (Exception e)
                                {
                                    Log("SHIT:" + e);
                                    throw;
                                }

                        LogVerbose("Inventory done");

                        Log("Checking retainer[{0}] against player inventory",retainerIndex);

                        foreach (var item in (ff14bot.Managers.InventoryManager.FilledSlots.Where(x => x.BagId == InventoryBagId.Bag1 || x.BagId == InventoryBagId.Bag2 || x.BagId == InventoryBagId.Bag3 || x.BagId == InventoryBagId.Bag4)).Where(RetainerInventory.FilterStackable))
                        {
                            if (inventory.HasItem(item.TrueItemId))
                            {
                                Log("BOTH PLAYER AND RETAINER HAVE Name: " + item.Item.EnglishName +
                                    "\tItemCategory: " + item.Item.EquipmentCatagory + "\tId: " + item.Item.Id);

                                if (RetainerSettings.Instance.DepositFromPlayer)
                                {
                                    Log("Moved: " + RetainerInventory.MoveItem(item,
                                            inventory.GetItem(item.TrueItemId)));
                                    await Coroutine.Sleep(200);
                                }
                            }
                        }

                        Log("Done checking against player inventory");

                        RetainerTasks.CloseInventory();

                        await Coroutine.Wait(3000, () => RetainerTasks.IsOpen);

                        //await Coroutine.Sleep(1000);

                        //Call quit in tasks and get through dialog

                        RetainerTasks.CloseTasks();

                        await Coroutine.Sleep(500);

                        await Coroutine.Wait(3000, () => DialogOpen);

                        if (DialogOpen) Next();

                        await Coroutine.Sleep(200);

                        await Coroutine.Wait(3000, () => RetainerList.IsOpen);

                        LogVerbose("Should be back at retainer list by now");

                        //inventory.PrintList();
                    }

                    retList.Add(inventory);
                }

                //await Coroutine.Sleep(1000);

                
                if (debug)
                {
                    foreach (var itemId in masterInventory)
                    {
                        string retainers = "";

                        foreach (var retainerId in itemId.Value)
                        {
                            retainers += $"Retainer[{retainerId.Key}] has {retainerId.Value} ";
                        }

                        Log("Item {0}: {1}", itemId.Key, retainers);
                    } 
                }

                LogCritical("Duplicate items Found:");

                foreach (var itemId in masterInventory.Where(r => r.Value.Count > 1))
                {
                    string retainers = "";

                    foreach (var retainerId in itemId.Value)
                    {
                        retainers += $"Retainer[{retainerId.Key}] has {retainerId.Value} ";
                    }

                    Log("Item {0}: {1}", itemId.Key, retainers);
                }

                LogVerbose("Closing Retainer List");

                RetainerList.Close();


                TreeRoot.Stop("Done playing with retainers");

                done = true;
            }

            return true;
        }

        private async Task<bool> UseSummoningBell()
        {
            List<GameObject> list;
            list = GameObjectManager.GameObjects
                .Where(r => r.Name == "Summoning Bell")
                .OrderBy(j => j.Distance())
                .ToList();

            if (list.Count <= 0)
            {
                LogCritical("No Summoning Bell Found");
                return false;
            }

            var bell = list[0];

            LogVerbose("Found nearest bell: {0} Distance: {1}", bell, bell.Distance2D(Core.Me.Location));

            if (bell.Distance2D(Core.Me.Location) >= 3)
            {
                await MoveSummoningBell(bell.Location);
                if (bell.Distance2D(Core.Me.Location) >= 3) return false;
            }
            bell.Interact();
            // No need to wait on IsOpen when we already do it in the main task.
            await Coroutine.Wait(5000, () => RetainerList.IsOpen);
            LogVerbose("Summoning Bell Used");

            return true;
        }


        private static async Task<bool> MoveSummoningBell(Vector3 loc)
        {
            var moving = MoveResult.GeneratingPath;
            while (!(moving == MoveResult.Done ||
                     moving == MoveResult.ReachedDestination ||
                     moving == MoveResult.Failed ||
                     moving == MoveResult.Failure ||
                     moving == MoveResult.PathGenerationFailed))
            {

                moving = Flightor.MoveTo(new FlyToParameters(loc));

                await Coroutine.Yield();
            }

            return true;
        }
    }
}