﻿// A class to be used as the base class for the generated template.
using System;
using System.Collections.Generic;
using System.Text;

namespace Nocco {
	public abstract class TemplateBase {

		// Properties available from within the template
		public string Title { get; set; }
		public string PathToCss { get; set; }
        public string PathToCss1 { get; set; }
        public string PathToJs { get; set; }
        public string PathToJs1 { get; set; }
        public string PathToJs2 { get; set; }
        public string PathToJs3 { get; set; }

        // This is HTML Code
        public string ExtraCss { get; set; }


		public Func<string, string> GetSourcePath { get; set; }
        public bool IsCodeFile { get; set; }
        public String Intro { get; set; }
		public List<Section> Sections { get; set; }
		public List<string> Sources { get; set; }
        public String BackToTopPath { get; set; }

		public StringBuilder Buffer { get; set; }

        // This is HTML Code
        public String Menu { get; set; }

		protected TemplateBase() {
			Buffer = new StringBuilder();
		}

		// This `Execute` function will be defined in the inheriting template
		// class. It generates the HTML by calling `Write` and `WriteLiteral`.
		public abstract void Execute();

		public virtual void Write(object value) {
			WriteLiteral(value);
		}

		public virtual void WriteLiteral(object value) {
			Buffer.Append(value);
		}
	}
}
