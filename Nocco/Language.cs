// A smart class used for generating nice HTML based on the language of your
// choice.
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nocco
{
	class Language
	{
		public string Name;
		public string Symbol;
		public Regex CommentMatcher { get { return new Regex(@"^\s*" + Symbol + @"\s?"); } }
		public Regex CommentFilter { get { return new Regex(@"(^#![/]|^\s*#\{)"); } }
		public IDictionary<string, string> MarkdownMaps;
		public IList<string> Ignores;

        // We want to consider C# regions as comment symbol
        public List<string> Symbols { get; set; }

        public List<Regex> CommentMatchers
        {
            get
            {
                List<Regex> res = new List<Regex>();
                foreach (string symbol in Symbols)
                {
                    res.Add(new Regex(@"^\s*" + symbol + @"\s?"));
                }
                return res;
            }
        }

        // We don't want to see includes in the documentation
        public List<string> IgnoreOnStart { get; set; }
        
	}
}
