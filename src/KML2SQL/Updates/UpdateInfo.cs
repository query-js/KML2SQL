using System;

namespace KML2SQL.Updates
{
    internal class UpdateInfo
    {
        public DateTime LastCheckedForUpdates { get; set; }
        public DateTime LastTimeNagged { get; set; }
        public bool DontNag { get; set; }
        public string LastVersionSeen { get; set; }
    }
}