// **Nocco** is a quick-and-dirty, literate-programming-style documentation
// generator. It is a C# port of [Docco](http://jashkenas.github.com/docco/),
// which was written by [Jeremy Ashkenas](https://github.com/jashkenas) in
// Coffescript and runs on node.js.
//
// Nocco produces HTML that displays your comments alongside your code.
// Comments are passed through
// [Markdown](http://daringfireball.net/projects/markdown/syntax), and code is
// highlighted using [google-code-prettify](http://code.google.com/p/google-code-prettify/)
// syntax highlighting. This page is the result of running Nocco against its
// own source files.
//
// Currently, to build Nocco, you'll have to have Visual Studio 2010. The project
// depends on [MarkdownSharp](http://code.google.com/p/markdownsharp/) and you'll
// have to install [.NET MVC 3](http://www.asp.net/mvc/mvc3) to get the
// System.Web.Razor assembly. The MarkdownSharp is a NuGet package that will be
// installed automatically when you build the project.
//
// To use Nocco, run it from the command-line:
//
//     nocco *.cs
//
// ...will generate linked HTML documentation for the named source files, saving
// it into a `docs` folder.
//
// The [source for Nocco](http://github.com/dontangg/nocco) is available on GitHub,
// and released under the MIT license.
//
// If **.NET** doesn't run on your platform, or you'd prefer a more convenient
// package, get [Rocco](http://rtomayko.github.com/rocco/), the Ruby port that's
// available as a gem. If you're writing shell scripts, try
// [Shocco](http://rtomayko.github.com/shocco/), a port for the **POSIX shell**.
// Both are by [Ryan Tomayko](http://github.com/rtomayko). If Python's more
// your speed, take a look at [Nick Fitzgerald](http://github.com/fitzgen)'s
// [Pycco](http://fitzgen.github.com/pycco/).

// Import namespaces to allow us to type shorter type names.
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Razor;

namespace Nocco {
	class Nocco {
		private static string _executingDirectory;
		private static List<string> _files;
		private static Type _templateType;

		//### Main Documentation Generation Functions

		// Generate the documentation for a source file by reading it in, splitting it
		// up into comment/code sections, highlighting them for the appropriate language,
		// and merging them into an HTML template.
		private static string GenerateDocumentation(string source) {
			var lines = File.ReadAllLines(source);
			var sections = Parse(source, lines);
			Hightlight(sections);
			return GenerateHtml(source, sections);
		}

		// Given a string of source code, parse out each comment and the code that
		// follows it, and create an individual `Section` for it.
		private static List<Section> Parse(string source, string[] lines) {
			var sections = new List<Section>();
			var language = GetLanguage(source);
			var hasCode = false;
			var docsText = new StringBuilder();
			var codeText = new StringBuilder();

			Action<string, string> save = (docs, code) => sections.Add(new Section { DocsHtml = docs, CodeHtml = code });
			Func<string, string> mapToMarkdown = docs => {
				if (language.MarkdownMaps != null)
					docs = language.MarkdownMaps.Aggregate(docs, (currentDocs, map) => Regex.Replace(currentDocs, map.Key, map.Value, RegexOptions.Multiline));
				return docs;
			};

			foreach (var line in lines) {
				if (language.CommentMatcher.IsMatch(line) && !language.CommentFilter.IsMatch(line)) {
					if (hasCode) {
						save(mapToMarkdown(docsText.ToString()), codeText.ToString());
						hasCode = false;
						docsText = new StringBuilder();
						codeText = new StringBuilder();
					}
					docsText.AppendLine(language.CommentMatcher.Replace(line, ""));
				}
				else {
					hasCode = true;
					codeText.AppendLine(line);
				}
			}
			save(mapToMarkdown(docsText.ToString()), codeText.ToString());

			return sections;
		}

		// Prepares a single chunk of code for HTML output and runs the text of its
		// corresponding comment through **Markdown**, using a C# implementation
		// called [MarkdownSharp](http://code.google.com/p/markdownsharp/).
		private static void Hightlight(List<Section> sections) {
			var markdown = new MarkdownSharp.Markdown();

			foreach (var section in sections) {
				section.DocsHtml = markdown.Transform(section.DocsHtml);
				section.CodeHtml = System.Web.HttpUtility.HtmlEncode(section.CodeHtml);
			}
		}

		// Once all of the code is finished highlighting, we can generate the HTML file
		// and write out the documentation. Pass the completed sections into the template
		// found in `Resources/Nocco.cshtml`
		private static string GenerateHtml(string source, List<Section> sections) {
			int depth;
			var destination = GetDestination(source, out depth);
			
			string pathToRoot = string.Concat(Enumerable.Repeat(".." + Path.DirectorySeparatorChar, depth));

			var htmlTemplate = Activator.CreateInstance(_templateType) as TemplateBase;

			htmlTemplate.Title = Path.GetFileName(source);
			htmlTemplate.PathToCss = Path.Combine(pathToRoot, "nocco.css").Replace('\\', '/');
		    htmlTemplate.PathToJs = Path.Combine(pathToRoot, "prettify.js").Replace('\\', '/');
			htmlTemplate.GetSourcePath = s => Path.Combine(pathToRoot, Path.ChangeExtension(s.ToLower(), ".html").Substring(2)).Replace('\\', '/');
			htmlTemplate.Sections = sections;
			htmlTemplate.Sources = _files;
			
			htmlTemplate.Execute();

			File.WriteAllText(destination, htmlTemplate.Buffer.ToString());

            return destination;
		}

		//### Helpers & Setup

		// Setup the Razor templating engine so that we can quickly pass the data in
		// and generate HTML.
		//
		// The file `Resources\Nocco.cshtml` is read and compiled into a new dll
		// with a type that extends the `TemplateBase` class. This new assembly is
		// loaded so that we can create an instance and pass data into it
		// and generate the HTML.
		private static Type SetupRazorTemplate() {
			var host = new RazorEngineHost(new CSharpRazorCodeLanguage());
			host.DefaultBaseClass = typeof(TemplateBase).FullName;
			host.DefaultNamespace = "RazorOutput";
			host.DefaultClassName = "Template";
			host.NamespaceImports.Add("System");

			GeneratorResults razorResult = null;
			using (var reader = new StreamReader(Path.Combine(_executingDirectory, "Resources", "Nocco.cshtml"))) {
				razorResult = new RazorTemplateEngine(host).GenerateCode(reader);
			}

			var compilerParams = new CompilerParameters {
				GenerateInMemory = true,
				GenerateExecutable = false,
				IncludeDebugInformation = false,
				CompilerOptions = "/target:library /optimize"
			};
			compilerParams.ReferencedAssemblies.Add(typeof(Nocco).Assembly.CodeBase.Replace("file:///", "").Replace("/", "\\"));

			var codeProvider = new Microsoft.CSharp.CSharpCodeProvider();
			var results = codeProvider.CompileAssemblyFromDom(compilerParams, razorResult.GeneratedCode);

			// Check for errors that may have occurred during template generation
			if (results.Errors.HasErrors) {
				foreach (var err in results.Errors.OfType<CompilerError>().Where(ce => !ce.IsWarning))
					Console.WriteLine("Error Compiling Template: ({0}, {1}) {2}", err.Line, err.Column, err.ErrorText);
			}

			return results.CompiledAssembly.GetType("RazorOutput.Template");
		}

		// A list of the languages that Nocco supports, mapping the file extension to
		// the symbol that indicates a comment. To add another language to Nocco's
		// repertoire, add it here.
		//
		// You can also specify a list of regular expression patterns and replacements. This
		// translates things like
		// [XML documentation comments](http://msdn.microsoft.com/en-us/library/b2s063f7.aspx) into Markdown.
		private static Dictionary<string, Language> Languages = new Dictionary<string, Language> {
			{ ".js", new Language {
				Name = "javascript",
				Symbol = "//",
				Ignores = new List<string> {
					"min.js"
				}
			}},
			{ ".cs", new Language {
				Name = "csharp",
				Symbol = "///?",
				Ignores = new List<string> {
					"Designer.cs"
				},
				MarkdownMaps = new Dictionary<string, string> {
					{ @"<c>([^<]*)</c>", "`$1`" },
					{ @"<param[^\>]*name=""([^""]*)""[^\>]*>([^<]*)</param>", "**argument** *$1*: $2" + Environment.NewLine },
					{ @"<returns>([^<]*)</returns>", "**returns**: $1" + Environment.NewLine },
					{ @"<see\s*cref=""([^""]*)""\s*/>", "see `$1`"},
					{ @"(</?example>|</?summary>|</?remarks>)", "" },
				}
			}},
			{ ".vb", new Language {
				Name = "vb.net",
				Symbol = "'+",
				Ignores = new List<string> {
					"Designer.vb"
				},
				MarkdownMaps = new Dictionary<string, string> {
					{ @"<c>([^<]*)</c>", "`$1`" },
					{ @"<param[^\>]*>([^<]*)</param>", "" },
					{ @"<returns>([^<]*)</returns>", "" },
					{ @"<see\s*cref=""([^""]*)""\s*/>", "see `$1`"},
					{ @"(</?example>|</?summary>|</?remarks>)", "" },
				}
			}}
		};

		// Get the current language we're documenting, based on the extension.
		private static Language GetLanguage(string source) {
			var extension = Path.GetExtension(source);
			return Languages.ContainsKey(extension) ? Languages[extension] : null;
		}

		// Compute the destination HTML path for an input source file path. If the source
		// is `Example.cs`, the HTML will be at `docs/example.html`
		private static string GetDestination(string filepath, out int depth) {
			var dirs = Path.GetDirectoryName(filepath).Substring(1).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			depth = dirs.Length;

			var dest = Path.Combine("docs", string.Join(Path.DirectorySeparatorChar.ToString(), dirs)).ToLower();
			Directory.CreateDirectory(dest);

			return Path.Combine("docs", Path.ChangeExtension(filepath, "html").ToLower());
		}

		// Find all the files that match the pattern(s) passed in as arguments and
		// generate documentation for each one.
		public static void Generate(string[] targets) {
			if (targets.Length > 0) {
				Directory.CreateDirectory("docs");

				_executingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				File.Copy(Path.Combine(_executingDirectory, "Resources", "Nocco.css"), Path.Combine("docs", "nocco.css"), true);
				File.Copy(Path.Combine(_executingDirectory, "Resources", "prettify.js"), Path.Combine("docs", "prettify.js"), true);

				_templateType = SetupRazorTemplate();

				_files = new List<string>();
				foreach (var target in targets) {
					_files.AddRange(Directory.GetFiles(".", target, SearchOption.AllDirectories).Where(filename => {
						var language = GetLanguage(Path.GetFileName(filename)) ;

						if (language == null)
							return false;
						
						// Check if the file extension should be ignored
						if (language.Ignores != null && language.Ignores.Any(ignore => filename.EndsWith(ignore)))
							return false;

						// Don't include certain directories
						var foldersToExclude = new string[] { @"\docs", @"\bin", @"\obj" };
						if (foldersToExclude.Any(folder => Path.GetDirectoryName(filename).Contains(folder)))
							return false;

						return true;
					}));
                }
                #region Select files
                List<string> tempFiles = new List<string>();
                foreach (var file in _files)
                {
                    var lines = File.ReadAllLines(file);
                    if (lines.Length > 0 && (lines[0].StartsWith("// GENERATE") || lines[0].StartsWith("//GENERATE")))
                    {
                        tempFiles.Add(file);
                    }
                }
                _files = tempFiles;
                #endregion
                List<String> allFiles = new List<string>();
                List<LinkFileToClass> allLinks = new List<LinkFileToClass>();
                foreach (var file in _files)
                    allFiles.Add(GenerateDocumentation(file));


                Regex reg = new Regex(@"LINK\\(\w+(\#\w+)?)");
                foreach (var file in allFiles)
                    allLinks.Add(new LinkFileToClass(file.Substring(7)));

                foreach (var file in allFiles)
                {
                    LinkFileToClass currentFile = allLinks.SingleOrDefault(l => l.MyPath == file.Substring(7));
                    var lines = File.ReadAllLines(file);
                    for (int j = 0; j < lines.Length; ++j)
                    {
                        var line = lines[j];
                        if (line.Contains("<a href=\"LINK"))
                        {
                            line.ToList();
                            if (reg.IsMatch(line))
                            {
                                var matches = reg.Matches(line);

                                foreach (Match match in matches)
                                {
                                    // On gère ici les liens avec les ancres s'il y en a
                                    string typeNameWithAnchor = match.Value.Split(new char[] { '\\' })[1];
                                    string[] anchorTab = typeNameWithAnchor.Split(new char[] { '#' });
                                    string typeName = anchorTab[0];
                                    string anchorName = String.Empty;
                                    if (anchorTab.Length == 2)
                                    {
                                        anchorName = anchorTab[1];
                                    }
                                    LinkFileToClass link = allLinks.SingleOrDefault(l => l.FileTypeName == typeName.ToUpper());
                                    if (link != null)
                                    {
                                        if (anchorName == String.Empty)
                                        {
                                            lines[j] = lines[j].Replace(match.Value, currentFile.PathToRoot + link.MyPath);
                                        }
                                        else
                                        {
                                            lines[j] = lines[j].Replace(match.Value, currentFile.PathToRoot + link.MyPath + "#" + anchorName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    File.WriteAllLines(file, lines);
                }

			}
		}
	}
}
