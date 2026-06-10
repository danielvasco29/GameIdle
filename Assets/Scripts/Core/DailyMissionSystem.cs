using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameIdle
{
    // Três missões diárias que resetam à meia-noite. Progresso e claims persistidos no save.
    public static class DailyMissionSystem
    {
        public class Mission
        {
            public string id, name;
            public int target;
            public int gemReward;
        }

        public static readonly Mission[] All =
        {
            new() { id = "taps",    name = "Trabalhar 50 vezes",      target = 50,  gemReward = 5  },
            new() { id = "hires",   name = "Contratar 5 funcionários", target = 5,   gemReward = 8  },
            new() { id = "prestige",name = "Realizar 1 prestígio",     target = 1,   gemReward = 20 },
            new() { id = "kills", name = "Derrotar 5 monstros", target = 5, gemReward = 6 },
        };

        private static readonly int[] _progress = new int[4];
        private static readonly bool[] _claimed  = new bool[4];
        private static string _lastDate = "";

        public static string LastMissionDate => _lastDate;

        public static int  GetProgress(int i) => _progress[i];
        public static bool IsClaimed(int i)   => _claimed[i];
        public static bool IsComplete(int i)  => _progress[i] >= All[i].target;

        // True se há alguma missão concluída ainda não coletada.
        public static bool HasClaimable()
        {
            for (int i = 0; i < All.Length; i++)
                if (IsComplete(i) && !IsClaimed(i)) return true;
            return false;
        }

        // Called every game start — resets if date changed
        public static void CheckDailyReset()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (_lastDate == today) return;
            _lastDate = today;
            Array.Clear(_progress, 0, _progress.Length);
            Array.Clear(_claimed,  0, _claimed.Length);
        }

        public static void RegisterTap()
        {
            if (_claimed[0]) return;
            _progress[0] = Mathf.Min(_progress[0] + 1, All[0].target);
        }

        public static void RegisterHire()
        {
            if (_claimed[1]) return;
            _progress[1] = Mathf.Min(_progress[1] + 1, All[1].target);
        }

        public static void RegisterPrestige()
        {
            if (_claimed[2]) return;
            _progress[2] = Mathf.Min(_progress[2] + 1, All[2].target);
        }

        public static void RegisterKill()
        {
            if (_claimed[3]) return;
            _progress[3] = Mathf.Min(_progress[3] + 1, All[3].target);
        }

        public static bool TryClaim(int i)
        {
            if (_claimed[i] || !IsComplete(i)) return false;
            _claimed[i] = true;
            GameManager.Instance.AddGems(All[i].gemReward);
            UIManager.Instance.ShowToast($"Missão concluída! +{All[i].gemReward} gemas", new UnityEngine.Color(0.4f, 0.9f, 1f));
            SaveSystem.Save();
            return true;
        }

        public static void Load(string lastDate, List<int> progress, List<bool> claimed)
        {
            _lastDate = lastDate ?? "";
            for (int i = 0; i < _progress.Length; i++)
            {
                _progress[i] = (progress != null && i < progress.Count) ? progress[i] : 0;
                _claimed[i]  = (claimed  != null && i < claimed.Count)  ? claimed[i]  : false;
            }
            CheckDailyReset();
        }

        public static List<int>  GetProgressSave() { var l = new List<int>(_progress);  return l; }
        public static List<bool> GetClaimedSave()  { var l = new List<bool>(_claimed);  return l; }
    }
}
