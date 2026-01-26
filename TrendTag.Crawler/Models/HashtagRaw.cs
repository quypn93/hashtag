using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendTag.Crawler.Models {
    public class HashtagRaw {
        public string Tag { get; set; }
        public int Rank { get; set; }
        public DateTime CollectedDate { get; set; }
    }
}
