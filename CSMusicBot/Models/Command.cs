using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSMusicBot.Models
{
    public record Command
    {
        public string Prefix { get; set; }
        public ulong Owner { get; set; }
        public string SuccessEmoji { get; set; }
        public string WarningEmoji { get; set; }
        public string ErrorEmoji { get; set; }
        public string LoadingEmoji { get; set; }
    }
}
