using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nocco
{
    public class IndexMaintainer
    {
        public String Name { get; set; }
        public String Content { get; set; }
        public int Depth { get; set; }

        public IndexMaintainer Parent { get; set; }

        public List<IndexMaintainer> Children { get; set; }

        public bool IsMethod { get; set; }
        public int Offset { get; set; }
    }
}
