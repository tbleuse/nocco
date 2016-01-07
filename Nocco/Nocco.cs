// GENERATE
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
        private static string GenerateDocumentation(string source, bool generateSummary)
        {
			var lines = File.ReadAllLines(source);
            // We remove the first line (GENERATE comment)
            lines = lines.Where((val, idx) => !val.Contains("GENERATE")).ToArray();
			var documentation = Parse(source, lines, generateSummary);
			Hightlight(documentation);
            return GenerateHtml(source, documentation);
		}     

		// Given a string of source code, parse out each comment and the code that
		// follows it, and create an individual `Section` for it.
        // This function return a [documentation file](LINK\DocumentationFile).
		private static DocumentationFile Parse(string source, string[] lines, bool generateSummary) {
            DocumentationFile documentation = new DocumentationFile
            {
                Sections = new List<Section>()
            };
			var language = GetLanguage(source);
          
			var hasCode = false;
            var intro = new StringBuilder();
			var docsText = new StringBuilder();
			var codeText = new StringBuilder();

            // If the language is mardown, it means that the file is
            // a pure documentation file, so every line is copied and
            // the file is considered as a file with only a Introduction
            if (language.Name == "markdown")
            {
                documentation.IsCodeFile = false;
                foreach (var line in lines)
                {
                    intro.AppendLine(line);
                }
                documentation.Intro = intro.ToString();
                return documentation;
            }
            else
            {
                documentation.IsCodeFile = true;
            }

			Action<string, string> save = (docs, code) => documentation.Sections.Add(new Section { DocsHtml = docs, CodeHtml = code });
			Func<string, string> mapToMarkdown = docs => {
				if (language.MarkdownMaps != null)
					docs = language.MarkdownMaps.Aggregate(docs, (currentDocs, map) => Regex.Replace(currentDocs, map.Key, map.Value, RegexOptions.Multiline));
				return docs;
			};

            // If the use chose to not generate the summary, Nocco behaves like [Don Wilson's version](https://github.com/dontangg/nocco)
            if (!generateSummary)
            {
                foreach (var line in lines)
                {
                    if (language.CommentMatcher.IsMatch(line) && !language.CommentFilter.IsMatch(line))
                    {
                        if (hasCode)
                        {
                            save(mapToMarkdown(docsText.ToString()), codeText.ToString());
                            hasCode = false;
                            docsText = new StringBuilder();
                            codeText = new StringBuilder();
                        }
                        docsText.AppendLine(language.CommentMatcher.Replace(line, ""));
                    }
                    else
                    {
                        hasCode = true;
                        codeText.AppendLine(line);
                    }
                }
                save(mapToMarkdown(docsText.ToString()), codeText.ToString());
            }

            else
            {
                // Defining new [index maintainer](LINK\IndexMaintainer) to be at the top of summary.
                IndexMaintainer indexMaintainer = new IndexMaintainer
                {
                    Name = "BASE",
                    Depth = 0,
                    Children = new List<IndexMaintainer>(),
                    IsMethod = false
                };
                IndexMaintainer currentIndexMaintainer = indexMaintainer;
                // We want to get all lines before the imports/using to be the introduction of the file.
                bool isIntro = true;
                bool ignoreLine = false;
                bool hasMatch = false;
                foreach (var line in lines.Where(l => l != String.Empty))
                {
                    // Make a copy of the line to modify it
                    // We wount with spaces for indentation
                    var lineToSave = line.Replace("\t", "    ");
                    // Find where the end of the intro is
                    // We don't want imports in the final document, so we use the `IgnoreOnStart` property
                    // of the [language](LINK\Language).
                    ignoreLine = false;
                    if (language.IgnoreOnStart == null || language.IgnoreOnStart.Count == 0)
                    {
                        isIntro = false;
                    }
                    else
                    {
                        foreach (string ignore in language.IgnoreOnStart)
                        {
                            if (lineToSave.StartsWith(ignore))
                            {
                                isIntro = false;
                                ignoreLine = true;
                            }
                        }
                    }
                    if (isIntro)
                    {
                        if (language.CommentMatcher.IsMatch(lineToSave) && !language.CommentFilter.IsMatch(lineToSave))
                        {
                            intro.AppendLine(language.CommentMatcher.Replace(lineToSave, ""));
                        }
                    }
                    else
                    {
                        int i = 0;
                        hasMatch = false;
                        while (i < language.CommentMatchers.Count && !hasMatch)
                        {
                            Regex commentMatcher = language.CommentMatchers[i].Item2;
                            String symbol = language.CommentMatchers[i].Item1;                            
                            if (commentMatcher.IsMatch(lineToSave) && !language.CommentFilter.IsMatch(lineToSave))
                            {
                                hasMatch = true;
                                if (language.SymbolsMatching.Count(m => m.Item1 == symbol) > 0)
                                {
                                    // New Index maintainer
                                    IndexMaintainer maintainer = new IndexMaintainer
                                    {
                                        Parent = currentIndexMaintainer,
                                        Depth = currentIndexMaintainer.Depth + 1,
                                        Content = lineToSave.Replace(symbol, "").Trim(),
                                        Children = new List<IndexMaintainer>(),
                                        IsMethod = false
                                    };
                                    // Is it a block of code?
                                    if (language.EndOfCode.Intersect(language.SymbolsMatching.Where(m => m.Item1 == symbol).Select(m => m.Item2)).Count() > 0)
                                    {
                                        int whitespace = 0;
                                        whitespace = lineToSave.TakeWhile(Char.IsWhiteSpace).Count();
                                        maintainer.IsMethod = true;
                                        maintainer.Offset = whitespace;
                                    }
                                    if (currentIndexMaintainer.Name != "BASE")
                                    {
                                        maintainer.Name = currentIndexMaintainer.Name + "." + (currentIndexMaintainer.Children.Count + 1).ToString();
                                    }
                                    else
                                    {
                                        maintainer.Name = (currentIndexMaintainer.Children.Count + 1).ToString();
                                    }
                                    int indexOfContent = lineToSave.IndexOf(maintainer.Content);
                                    for (int j = 0; j <= maintainer.Depth; ++j)
                                    {
                                        lineToSave = lineToSave.Insert(indexOfContent - 1, "#");
                                    }
                                    // We want t get the number of the menu item (1.1.1, 2.1 ...) in its title so we insert the name of the current
                                    // index element just before the content.
                                    indexOfContent = lineToSave.IndexOf(maintainer.Content);
                                    lineToSave = lineToSave.Insert(indexOfContent - 1, maintainer.Name + ".");
                                    // To allow user to navigation in the documentation file, we insert an empty `<span>` element with an unique `id`.                           
                                    lineToSave += "<span id=\"" + maintainer.Name + "\"></span>";

                                    // We add the new index element to the children list of the current index element, then
                                    // the new element becomes the current element.
                                    currentIndexMaintainer.Children.Add(maintainer);
                                    currentIndexMaintainer = maintainer;
                                    save(mapToMarkdown(docsText.ToString()), codeText.ToString());
                                    docsText = new StringBuilder();
                                    codeText = new StringBuilder();
                                    docsText.AppendLine(commentMatcher.Replace(lineToSave, ""));
                                    save(mapToMarkdown(docsText.ToString()), codeText.ToString());
                                    hasCode = false;
                                    docsText = new StringBuilder();
                                    codeText = new StringBuilder();
                                    break;
                                }
                                if (language.SymbolsMatching.Count(m => m.Item2 == symbol) > 0)
                                {
                                    // Close current IndexMaintainer
                                    if (currentIndexMaintainer.IsMethod == false)
                                    {
                                        currentIndexMaintainer = currentIndexMaintainer.Parent;
                                    }
                                }
                                if (hasCode)
                                {
                                    save(mapToMarkdown(docsText.ToString()), codeText.ToString());
                                    hasCode = false;
                                    docsText = new StringBuilder();
                                    codeText = new StringBuilder();
                                }
                                docsText.AppendLine(commentMatcher.Replace(lineToSave, ""));
                            }
                            ++i;
                        }
                        if (!hasMatch)
                        {
                            hasCode = true;
                            if (!ignoreLine)
                            {
                                codeText.AppendLine(lineToSave);
                                if (language.EndOfCode.Contains(lineToSave.Trim()))
                                {
                                    int whitespace = 0;
                                    whitespace = lineToSave.TakeWhile(Char.IsWhiteSpace).Count();
                                    if (currentIndexMaintainer.IsMethod && whitespace == currentIndexMaintainer.Offset)
                                    {
                                        save(mapToMarkdown(docsText.ToString()), codeText.ToString());
                                        currentIndexMaintainer = currentIndexMaintainer.Parent;
                                        hasCode = false;
                                        docsText = new StringBuilder();
                                        codeText = new StringBuilder();
                                    }
                                }
                            }
                        }
                    }
                }
                save(mapToMarkdown(docsText.ToString()), codeText.ToString());
                documentation.Intro = intro.ToString();
                String summary = String.Empty;
                if (indexMaintainer.Children.Count > 0)
                {
                    summary = BuildSummary(indexMaintainer);
                }
                if (summary != string.Empty)
                {
                    documentation.Intro += "<hr />" + summary + "<hr />";
                }
            }
            documentation.Sections = documentation.Sections.Where(s => !((s.CodeHtml == String.Empty || s.CodeHtml ==  "\r\n")
                && (s.DocsHtml == String.Empty || s.DocsHtml == "\r\n"))).ToList();
			return documentation;
		}       

		// Prepares a single chunk of code for HTML output and runs the text of its
		// corresponding comment through **Markdown**, using a C# implementation
		// called [MarkdownSharp](http://code.google.com/p/markdownsharp/).
		private static void Hightlight(DocumentationFile documentationFile) {
			var markdown = new MarkdownSharp.Markdown();

            documentationFile.Intro = markdown.Transform(documentationFile.Intro);
			foreach (var section in documentationFile.Sections) {
				section.DocsHtml = markdown.Transform(section.DocsHtml);
				section.CodeHtml = System.Web.HttpUtility.HtmlEncode(section.CodeHtml);
			}
		}

		// Once all of the code is finished highlighting, we can generate the HTML file
		// and write out the documentation. Pass the completed sections into the template
		// found in `Resources/Nocco.cshtml`
		private static string GenerateHtml(string source, DocumentationFile documentation) {
			int depth;
			var destination = GetDestination(source, out depth);
			
			string pathToRoot = string.Concat(Enumerable.Repeat(".." + Path.DirectorySeparatorChar, depth));

			var htmlTemplate = Activator.CreateInstance(_templateType) as TemplateBase;

			htmlTemplate.Title = Path.GetFileName(source);
			htmlTemplate.PathToCss = Path.Combine(pathToRoot, "nocco.css").Replace('\\', '/');
            htmlTemplate.PathToCss1 = Path.Combine(pathToRoot, "jquery.treeView.css").Replace('\\', '/');
		    htmlTemplate.PathToJs = Path.Combine(pathToRoot, "prettify.js").Replace('\\', '/');
            htmlTemplate.PathToJs1 = Path.Combine(pathToRoot, "jquery-1.9.1.js").Replace('\\', '/');
            htmlTemplate.PathToJs2 = Path.Combine(pathToRoot, "nocco.js").Replace('\\', '/');
            htmlTemplate.PathToJs3 = Path.Combine(pathToRoot, "jquery.treeView.js").Replace('\\', '/');            
			htmlTemplate.GetSourcePath = s => Path.Combine(pathToRoot, Path.ChangeExtension(s.ToLower(), ".html").Substring(2)).Replace('\\', '/');
            htmlTemplate.Intro = documentation.Intro;
            htmlTemplate.Sections = documentation.Sections;
            htmlTemplate.IsCodeFile = documentation.IsCodeFile;
            var menu = MakeMenu(MakeFolders(_files, pathToRoot));
            menu = menu.Remove(0, 4);
            menu = menu.Remove(menu.Length - 5, 5);
            htmlTemplate.Menu = menu;
			htmlTemplate.Sources = _files;
            htmlTemplate.BackToTopPath = pathToRoot + @"Images\arrow-top.png";
			htmlTemplate.Execute();

			File.WriteAllText(destination, htmlTemplate.Buffer.ToString());

            return destination;
		}
        #region Navigation utils
        // cb BuildSummary
        // Should be in a chtml template but cannot do recursivity yet
        private static String BuildSummary(IndexMaintainer maintainer)
        {
            String res = String.Empty;
            if (maintainer.Name == "BASE")
            {
                res += "<ul class=\"noDecoration\">";
                foreach (IndexMaintainer child in maintainer.Children)
                {
                    res += BuildSummary(child);
                }
                res += "</ul>";
            }
            else
            {
                res += "<li>";
                res += "<a class=\"menuLink\" href=\"#" + maintainer.Name + "\">" + maintainer.Name + ". " + maintainer.Content + "</a>";
                if (maintainer.Children.Count > 0)
                {
                    res += "<ul class=\"noDecoration\">";
                    foreach (IndexMaintainer child in maintainer.Children)
                    {
                        res += BuildSummary(child);
                    }
                    res += "</ul>";
                }
                res += "</li>";
            }
            return res;
        }

        // cb Make menu
        // Should be in a chtml template but cannot do recursivity yet
        public static string MakeMenu(Folder folder)
        {
            string menu = string.Empty;
            if (folder.Files.Count > 0 || folder.Folders.Count > 0)
            {
                menu += "<ul>";
                foreach (Folder fold in folder.Folders)
                {
                    menu += "<li><span class=\"folder\">" + fold.Name + "</span>";
                    menu += MakeMenu(fold);
                    menu += "</li>";
                }
                foreach (FileUrl file in folder.Files)
                {
                    menu += "<li><a href=\"" + file.Url + "\">" + file.Name + "</a>";
                    menu += "</li>";
                }

                menu += "</ul>";
            }
            return menu;
        }

        // cb Create Folders from files
        public static Folder MakeFolders(List<string> files, string pathToRoot)
        {
            Folder res = new Folder
            {
                Name = "Base",
                Folders = new List<Folder>(),
                Files = new List<FileUrl>()
            };
            Folder currentFolder = res;
            foreach (string filepath in files)
            {
                var dirs = Path.GetDirectoryName(filepath).Substring(1).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                int depth = dirs.Length;
                if (depth == 0)
                {
                    if (res.Folders.Count(f => f.Name == "Files") == 0)
                    {
                        res.Folders.Add(new Folder
                        {
                            Name = "Files",
                            Folders = new List<Folder>(),
                            Files = new List<FileUrl>()
                        });
                    }
                    Folder fold = res.Folders.SingleOrDefault(f => f.Name == "Files");
                    fold.Files.Add(new FileUrl
                    {
                        Url = pathToRoot + Path.ChangeExtension(filepath.Substring(1).Trim(new[] { Path.DirectorySeparatorChar }), "html").ToLower(),
                        Name = filepath.Substring(1).Trim(new[] { Path.DirectorySeparatorChar })
                    });
                }
                else
                {
                    int i = 0;
                    currentFolder = res;
                    while (i <= depth)
                    {
                        if (i == depth)
                        {
                            string[] splits = filepath.Substring(1).Split(new[] { Path.DirectorySeparatorChar });
                            string name = String.Empty;
                            if(splits.Length > 0)
                            {
                                name = splits[splits.Length - 1];
                            }
                            currentFolder.Files.Add(new FileUrl
                            {
                                Url = pathToRoot + Path.ChangeExtension(filepath.Substring(1).Trim(new [] { Path.DirectorySeparatorChar }), "html").ToLower(),
                                Name = name
                            });
                        }
                        else
                        {
                            if (currentFolder.Folders.Count(f => f.Name == dirs[i]) == 0)
                            {
                                currentFolder.Folders.Add(new Folder
                                {
                                    Name = dirs[i],
                                    Folders = new List<Folder>(),
                                    Files = new List<FileUrl>()
                                });
                            }
                            currentFolder = currentFolder.Folders.SingleOrDefault(f => f.Name == dirs[i]);
                        }
                        ++i;
                    }
                }
            }
            return res;
        }
        #endregion
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
            using (var reader = new StreamReader(Path.Combine(_executingDirectory, "Resources", "Nocco.cshtml")))
            {
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
            {".md", new Language{
                Name="markdown"
            }},
			{ ".js", new Language {
				Name = "javascript",
				Symbol = "//",
                Symbols = new List<string> { "// #region", "// #endregion", "// cb", "///?" },
                SymbolsMatching = new List<Tuple<string,string>> { 
                    new Tuple<string, string>("// #region", "// #endregion"),
                    new Tuple<string, string>("// cb", "}"),
                    new Tuple<string, string>("// cb", "};"),
                    new Tuple<string, string>("// cb", "});")
                },
                EndOfCode = new List<string> { "}", "};", "});" },
				Ignores = new List<string> {
					"min.js"
				}
			}},
			{ ".cs", new Language {
				Name = "csharp",
				Symbol = "///?",
                Symbols = new List<string> { "#region", "#endregion", "// cb", "///?" },
                SymbolsMatching = new List<Tuple<string,string>> { 
                    new Tuple<string, string>("#region", "#endregion"),
                    new Tuple<string, string>("// cb", "}")
                },
				Ignores = new List<string> {
					"Designer.cs"
				},
                EndOfCode = new List<string> { "}" },
				MarkdownMaps = new Dictionary<string, string> {
					{ @"<c>([^<]*)</c>", "`$1`" },
					{ @"<param[^\>]*name=""([^""]*)""[^\>]*>([^<]*)</param>", "**argument** *$1*: $2" + Environment.NewLine },
					{ @"<returns>([^<]*)</returns>", "**returns**: $1" + Environment.NewLine },
					{ @"<see\s*cref=""([^""]*)""\s*/>", "see `$1`"},
					{ @"(</?example>|</?summary>|</?remarks>)", "" },
				},
                IgnoreOnStart = new List<string> { "using" }
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
		public static void Generate(string[] args) {
            Regex targetReg = new Regex("\\*\\.[a-z]+");           
            List<string> targets = new List<string>();
            bool generateSummary = false;
            bool optionFile = false;
            string optionPath = string.Empty;
            foreach (var arg in args)
            {
                if (optionFile)
                {
                    optionPath = arg;
                }
                if (targetReg.IsMatch(arg))
                {
                    targets.Add(arg);
                }
                else if(arg == "--summary")
                {
                    generateSummary = true;
                }
                else if (arg == "--conf")
                {
                    optionFile = true;
                }
               
            }

			if (targets.Count > 0) {
				Directory.CreateDirectory("docs");

				_executingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				File.Copy(Path.Combine(_executingDirectory, "Resources", "Nocco.css"), Path.Combine("docs", "nocco.css"), true);
                File.Copy(Path.Combine(_executingDirectory, "Resources", "jquery.treeView.css"), Path.Combine("docs", "jquery.treeView.css"), true);
				File.Copy(Path.Combine(_executingDirectory, "Resources", "prettify.js"), Path.Combine("docs", "prettify.js"), true);
                File.Copy(Path.Combine(_executingDirectory, "Resources", "jquery-1.9.1.js"), Path.Combine("docs", "jquery-1.9.1.js"), true);
                File.Copy(Path.Combine(_executingDirectory, "Resources", "nocco.js"), Path.Combine("docs", "nocco.js"), true);
                File.Copy(Path.Combine(_executingDirectory, "Resources", "jquery.treeView.js"), Path.Combine("docs", "jquery.treeView.js"), true);
                // Create a folder Images to store the "back to top" arrow
                Directory.CreateDirectory(@"docs\Images");
                File.Copy(Path.Combine(_executingDirectory, "Resources", "arrow-top.png"), Path.Combine("docs", "Images", "arrow-top.png"), true);
                File.Copy(Path.Combine(_executingDirectory, "Resources", "arrow-down.png"), Path.Combine("docs", "Images", "arrow-down.png"), true);
                File.Copy(Path.Combine(_executingDirectory, "Resources", "arrow-left.png"), Path.Combine("docs", "Images", "arrow-left.png"), true);
				_templateType = SetupRazorTemplate();

                if (Directory.Exists("DocImages"))
                {
                    foreach (var file in Directory.GetFiles("DocImages"))
                    {
                        string fileName = file.Split(new char[] { '\\' })[1];
                        File.Copy(file, Path.Combine("docs", "Images", fileName), true);
                    }
                }

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
                    if (lines.Length > 0 && 
                        (lines[0].StartsWith("// GENERATE") || lines[0].StartsWith("//GENERATE") || file.EndsWith(".md")))
                    {
                        tempFiles.Add(file);
                    }
                }
                _files = tempFiles;
                #endregion
                List<String> allFiles = new List<string>();
                List<LinkFileToClass> allLinks = new List<LinkFileToClass>();
                foreach (var file in _files)
                {
                    allFiles.Add(GenerateDocumentation(file, generateSummary));
                }


                Regex reg = new Regex(@"LINK\\(\w+(\#\w+)?)");
                Regex regImg = new Regex(@"IMG\\(\w+)");
                foreach (var file in allFiles)
                {
                    allLinks.Add(new LinkFileToClass(file.Substring(7)));
                }

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
                                    // We handle anchor if there are.
                                    string typeNameWithAnchor = match.Value.Split(new char[] { '\\' })[1];
                                    string[] anchorTab = typeNameWithAnchor.Split(new char[] { '#' });
                                    string typeName = anchorTab[0];
                                    string anchorName = String.Empty;
                                    if (anchorTab.Length == 2)
                                    {
                                        anchorName = anchorTab[1];
                                    }
                                    LinkFileToClass link = null;
                                    // A bit of random...
                                    // Can do better by matching on folders
                                    if (allLinks.Count(l => l.FileTypeName == typeName.ToUpper()) > 0)
                                    {
                                        link = allLinks.First(l => l.FileTypeName == typeName.ToUpper());
                                    }                                    
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
                        if (line.Contains("src=\"IMG"))
                        {
                            var matches = regImg.Matches(line);

                            foreach (Match match in matches)
                            {
                                // Get the Image name
                                if (match.Value.Split(new char[] { '\\' }).Length > 1)
                                {
                                    string imageName = match.Value.Split(new char[] { '\\' })[1];
                                    lines[j] = lines[j].Replace(match.Value, currentFile.PathToRoot + "Images\\" + imageName);
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
