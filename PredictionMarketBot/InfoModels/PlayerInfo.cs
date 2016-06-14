using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictionMarketBot.InfoModels
{
    public class PlayerInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Money { get; set; }
        public ICollection<ShareInfo> Shares { get; set; }

        public PlayerInfo()
        {
            Shares = new List<ShareInfo>();
        }
    }
}
