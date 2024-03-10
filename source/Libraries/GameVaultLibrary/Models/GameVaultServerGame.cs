using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameVaultLibrary.Models
{
    public class GameVaultServerGame
    {
        public int id { get; set; }
        public string title { get; set; }
        public string version { get; set; }
        public ulong size { get; set; }

        // Other properties are ignored for now in case the API changes at a later date - For now, just rely on the builtin metadata providers
    }
}
