using System.Threading;
using System.Threading.Tasks;
using UltimateFishBot.Classes.Helpers;

namespace UltimateFishBot.Classes.BodyParts
{
    public class Hands
    {
        private int _baitIndex;
        private string[] _baitKeys;

        public Hands()
        {
            _baitIndex = 0;
            UpdateKeys();
        }

        public void UpdateKeys()
        {
            _baitKeys = new string[7]
            {
                Properties.Settings.Default.BaitKey1,
                Properties.Settings.Default.BaitKey2,
                Properties.Settings.Default.BaitKey3,
                Properties.Settings.Default.BaitKey4,
                Properties.Settings.Default.BaitKey5,
                Properties.Settings.Default.BaitKey6,
                Properties.Settings.Default.BaitKey7,
            };
        }

        public async Task Cast(CancellationToken token)
        {
            Win32.ActivateWow();
            Win32.SendKey(Properties.Settings.Default.FishKey);
            await Task.Delay(Properties.Settings.Default.CastingDelay, token);
        }

        public async Task Loot()
        {
            Win32.SendMouseClick();
            await Task.Delay(Properties.Settings.Default.LootingDelay);
        }

        public void ResetBaitIndex()
        {
            _baitIndex = 0;
        }

        public async Task DoAction(Manager.NeededAction action, Mouth mouth, CancellationToken cancellationToken)
        {
            string actionKey;
            int sleepTime;

            switch (action)
            {
                case Manager.NeededAction.HearthStone:
                    {
                        actionKey = Properties.Settings.Default.HearthKey;
                        mouth.Say(Translate.GetTranslate("manager", "LABEL_HEARTHSTONE"));
                        sleepTime = 0;
                        break;
                    }
                case Manager.NeededAction.Lure:
                    {
                        actionKey = Properties.Settings.Default.LureKey;
                        mouth.Say(Translate.GetTranslate("manager", "LABEL_APPLY_LURE"));
                        sleepTime = 3;
                        break;
                    }
                case Manager.NeededAction.Charm:
                    {
                        actionKey = Properties.Settings.Default.CharmKey;
                        mouth.Say(Translate.GetTranslate("manager", "LABEL_APPLY_CHARM"));
                        sleepTime = 3;
                        break;
                    }
                case Manager.NeededAction.Raft:
                    {
                        actionKey = Properties.Settings.Default.RaftKey;
                        mouth.Say(Translate.GetTranslate("manager", "LABEL_APPLY_RAFT"));
                        sleepTime = 2;
                        break;
                    }
                case Manager.NeededAction.Bait:
                    {
                        int baitIndex = 0;

                        if (Properties.Settings.Default.CycleThroughBaitList)
                        {
                            if (_baitIndex >= 6)
                                _baitIndex = 0;

                            baitIndex = _baitIndex++;
                        }

                        actionKey = _baitKeys[baitIndex];
                        mouth.Say(Translate.GetTranslate("manager", "LABEL_APPLY_BAIT", baitIndex));
                        sleepTime = 3;
                        break;
                    }
                default:
                    return;
            }

            Win32.ActivateWow();
            Win32.SendKey(actionKey);
            await Task.Delay(sleepTime * 1000, cancellationToken);
        }
    }
}
