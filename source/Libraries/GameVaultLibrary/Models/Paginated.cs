using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameVaultLibrary.Models
{
    public class Paginated<T>
    {
        public List<T> data { get; set; } = new List<T>();
        public Dictionary<string, object> meta { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> links { get; set; } = new Dictionary<string, string>();
    }
}
