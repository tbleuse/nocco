using System;
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
        public List<Tuple<string, string>> SymbolsMatching { get; set; }
        public List<Tuple<String, Regex>> CommentMatchers
        {
            get
            {
                List<Tuple<string, Regex>> res = new List<Tuple<string, Regex>>();
                foreach (string symbol in Symbols)
                {
                    res.Add(new Tuple<string, Regex>(symbol, new Regex(@"^\s*" + symbol + @"\s?")));
                }
                return res;
            }
        }

        // We don't want to see includes in the documentation
        public List<string> IgnoreOnStart { get; set; }

        public List<string> EndOfCode { get; set; }
        
	}
}
