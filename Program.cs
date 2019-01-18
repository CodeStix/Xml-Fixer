using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;

namespace XmlFixer
{
    class Program
    {
        public static bool pauseOnError = false;
        public static bool beautify = true;
        //public static bool hide = false;
        private static bool isWordDocument = false;
        public static string fixedFileLocation = @".\fixed.xml";

        private const char INDENT_PREFIX = ' ';
        private const string WORD_EXTRACT_DIRECTORY = "document";
        private const string INTERN_WORD_XML_FILE = @".\" + WORD_EXTRACT_DIRECTORY + @"\word\document.xml";
        private const string WORD_FIXED_FILE = @".\fixed.docx";


        public static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Xml-Fixer");
            Console.WriteLine("\tVersion: " + "1.0");
            Console.WriteLine("\tby Stijn Rogiest; 2019 (c)\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Thread.Sleep(1000);

            //args = new string[] { "test.txt" };

            string t = string.Join("", args);
            if (t.Contains("-?") || t.Contains("--help") || t.Contains("-h")) 
            {
                Console.WriteLine("Usage: " + "XmlFixer.exe [-u --ugly] [-p --not-pause] [-h -? --help] <filename>");
                Console.ReadKey();
                Environment.Exit(0);
            }
            if (t.Contains("-u") || t.Contains("--ugly"))
            {
                t = t.Replace("-u", "");

                beautify = false;
            }
            if (t.Contains("-p") || t.Contains("--error-pause"))
            {
                t = t.Replace("-p", "");

                pauseOnError = true;
            }

            string redFile = null;

            if (args.Length <= 0)
            {
                Console.WriteLine("Please drag *.xml file on top of executable. Or paste the filename here:");

                redFile = Console.ReadLine();
            }
            else
            {
                redFile = t;
            }

            if (!File.Exists(redFile))
            {
                Die("The given file does not exist.");
                return;
            }

            if (redFile.EndsWith(".docx"))
            {
                Console.WriteLine("A word document was given, extracting...");

                try
                {
                    ZipFile.ExtractToDirectory(redFile, WORD_EXTRACT_DIRECTORY);
                }
                catch(Exception e)
                {
                    Die("Could not extract: " + e.Message);
                    return;
                }

                Console.WriteLine("A word document was extracted.");

                beautify = false;
                isWordDocument = true;
                redFile = INTERN_WORD_XML_FILE;
                fixedFileLocation = INTERN_WORD_XML_FILE;

                if (!File.Exists(redFile))
                {
                    Die("The Word document could not be fixed.");
                    return;
                }
            }

            string input = File.ReadAllText(redFile);
            StreamWriter swf = new StreamWriter(fixedFileLocation);

            Stack<string> toClose = new Stack<string>();
            Queue<string> poppedTags = new Queue<string>();
            string line = "";
            int lineNumber = 1;
            bool opened = false;
            bool ender = false;
            string indent = "";
            string tagName = "";
            bool foundTagName = false;
            int level = 0;
            char prev = char.MinValue;
            /*Parallel.For(0, input.Length, (i) =>
            {

            });*/

            for(int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '\r' || c == '\n' || c == '\t')
                    continue;

                if (opened && prev == '<' && c == '/')
                {
                    ender = true;
                }

                if (opened && c == ' ' && !foundTagName)
                {
                    foundTagName = true;
                }

                if (opened && !foundTagName && c != '<' && c != '/' && c != '>')
                {
                    tagName += c;
                }

                if (!opened && c == '<')
                {
                    opened = true;
                }

                line += c;

                if (c == '>' && opened)
                {
                    bool exists = toClose.Contains(tagName);
                    bool hereIsError = false;
                    bool doWriteLine = true;
                    poppedTags.Clear();

                    if (ender)
                    {
                        if (exists)
                        {
                            if (beautify && indent.Length >= 1)
                                indent = indent.Substring(1);

                            while (toClose.Peek() != tagName)
                            {
                                poppedTags.Enqueue(toClose.Pop());
                            }
                            toClose.Pop();

                            if (poppedTags.Count > 0)
                            {
                                hereIsError = true;
                            }

                            level--;
                        }
                        else
                        {
                            hereIsError = true;
                            doWriteLine = false;
                        }
                    }

                    if (hereIsError)
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.Title = tagName;
                    if (pauseOnError)
                        Console.Write(lineNumber++ + ")\t" + (hereIsError ? "!!" : "  "));
                    Console.Write(indent);
                    if (beautify)
                        Console.WriteLine(line);
                    else
                        Console.Write(line);

                    /*sw.Write(indent);
                    if (beautify)
                        sw.WriteLine(line);
                    else
                        sw.Write(line);*/

                    swf.Write(indent);

                    if (hereIsError)
                    {
                        if (!exists)
                        {
                            Console.Title = $"The tag '{ tagName }' was closed before it was declared. Remove it.";

                            if (pauseOnError)
                                Console.ReadKey();
                        }
                        else
                        {
                            while (poppedTags.Count > 0)
                            {
                                string str = poppedTags.Dequeue();

                                Console.Title = $"The '{ str }' tag must end before the '{ tagName }' tag!";
                                swf.WriteLine($"{ indent }</{ str }>");

                                if (pauseOnError)
                                    Console.ReadKey();
                            }
                        }

                        Console.ResetColor();
                    }

                    if (doWriteLine)
                    {
                        if (beautify)
                            swf.WriteLine(line);
                        else
                            swf.Write(line);
                    }

                    line = "";

                    if (prev != '/' && prev != '?' && !ender)
                    {
                        level++;
                        if (beautify)
                            indent += INDENT_PREFIX;

                        toClose.Push(tagName);
                    }

                    if (prev == '?')
                        line += '\n';
                   
                    tagName = "";
                    foundTagName = false;
                    opened = false;
                    ender = false;
                }

                prev = c;
            }

            while (toClose.Count > 0)
            {
                string pop = toClose.Pop();

                Console.Title = "Tag not closed: " + pop;
                Console.ForegroundColor = ConsoleColor.Red;
                if (pauseOnError)
                    Console.WriteLine(lineNumber++ + ")\t" + "!!");
                //Console.ForegroundColor = ConsoleColor.Yellow;
                //Console.WriteLine($"{ indent }</{ pop }>");
                swf.WriteLine($"{ indent }</{ pop }>");

                if (beautify && indent.Length >= 1)
                    indent = indent.Substring(1);
                level--;

                if (pauseOnError)
                    Console.ReadKey();
            }

            swf.Close();

            if (isWordDocument)
            {
                Console.WriteLine("Archiving Word document...");

                try
                {
                    ZipFile.CreateFromDirectory(WORD_EXTRACT_DIRECTORY, WORD_FIXED_FILE, CompressionLevel.Optimal, false);
                }
                catch(Exception e)
                {
                    Die("Could not re-archive Word document: " + e.Message);
                    return;
                }

                Console.WriteLine("The fixed Word document was created.");
            }

            Console.ResetColor();
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(" -------------------- END OF FILE -------------------- ");
            Console.WriteLine("See starting directory to see fixed files. Fixed xml: " + fixedFileLocation);
            if (isWordDocument)
                Console.WriteLine("The Word document was probably fixed, location: " + WORD_FIXED_FILE);
            Console.WriteLine("Press enter to exit...");
            Console.ReadKey();
        }

        public static void Die(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ReadKey();
            Environment.Exit(0);
        }

        /*public static Dictionary<string, int> tags = new Dictionary<string, int>();

        private static string prefixer = "";
        private static StreamWriter beautifulFile;
        private static StreamWriter fixedFile;

        public static void Mai2n(string[] args)
        {
            string file = null;

            if (args.Length <= 0)
            {
                Console.WriteLine("Please drag *.xml file on top on this.");

                file = Console.ReadLine();

                return;
            }
            else
            {
                file = string.Join(" ", args);
            }

            string input = File.ReadAllText(file);

            beautifulFile = new StreamWriter("output.txt");
            foreach (string t in FindAllTags(input))
            {
                Console.WriteLine(t);
                beautifulFile.WriteLine(t);
            }
            beautifulFile.Close();
            DisplayResults(false);

            string cmd = "";
            while (cmd != "exit")
            {
                cmd = Console.ReadLine().Trim();
                string[] s = cmd.Split(' ');

                if (s[0] == "results")
                    DisplayResults(false);
                else if (s[0] == "results hide")
                    DisplayResults(true);
                else if (s[0] == "fix")
                {
                    foreach (string t in FindFixFor(input, s[1]))
                    {
                        Console.WriteLine(t);
                    }
                }
                else if (s[0] == "scan")
                {
                    foreach (string t in FindAllTags(input))
                    {
                        Console.WriteLine(t);
                        beautifulFile.WriteLine(t);
                    }
                    DisplayResults(false);
                }

            }

            Console.ReadKey();
        }

        public static IEnumerable<string> FindFixFor(string input, string tag)
        {
            Regex r = new Regex($"<(/)?([a-zA-Z0-9_:.-]+)([^>]*)>");//[] [a-zA-Z0-9_: \"/.=-]

            int level = 0;
            Stack<int> minLevels = new Stack<int>();
            minLevels.Push(0);

            foreach (Match m in r.Matches(input))
            {
                //Console.WriteLine($"Match: { m.Value } ({ m.Groups[1].Value })({ m.Groups[2].Value })({ m.Groups[3].Value })");

                string gr = m.Groups[2].Value.Trim();
                bool single = m.Groups[3].Value.Trim().EndsWith("/");
                bool ender = m.Groups[1].Value == "/";

                if (ender)
                {
                    if (prefixer.Length > 0)
                        prefixer = prefixer.Substring(0, prefixer.Length - 1);
                }

                if (single)
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                else if (ender)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                else
                    Console.ForegroundColor = ConsoleColor.Yellow;

                yield return prefixer + "<" + (ender ? "/" : "") + gr + (single ? "/" : "") + ">";

                if (single)
                {
                    RegisterFor(gr);
                    continue;
                }

                if (ender)
                {
                    level--;

                    if (gr == tag)
                    {
                        if (minLevels.Pop() != level)
                            Console.WriteLine(prefixer + $"<$$$$$$$$$$$ INSERT </{ tag }> HERE $$$$$$$$$$$>");
                    }
                }
                else
                {
                    if (gr == tag)
                        minLevels.Push(level);

                    level++;

                    prefixer += INDENT_PREFIX;
                }
            }

            while (minLevels.Count > 1)
            {
                minLevels.Pop();

                Console.WriteLine(prefixer + $"<$$$$$$$$$$$ INSERT </{ tag }> HERE $$$$$$$$$$$>");
            }
            
        }

        public static IEnumerable<string> FindAllTags(string input)
        {
            Regex r = new Regex($"<(/)?([a-zA-Z0-9_:.-]+)([^>]*)>");//[] [a-zA-Z0-9_: \"/.=-]

            foreach (Match m in r.Matches(input))
            {
                //Console.WriteLine($"Match: { m.Value } ({ m.Groups[1].Value })({ m.Groups[2].Value })({ m.Groups[3].Value })");

                string gr = m.Groups[2].Value.Trim();
                bool single = m.Groups[3].Value.Trim().EndsWith("/");
                bool ender = m.Groups[1].Value == "/";

                if (ender)
                {
                    if (prefixer.Length > 0)
                        prefixer = prefixer.Substring(0, prefixer.Length - 1);
                }

                if (single)
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                else if (ender)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                else
                    Console.ForegroundColor = ConsoleColor.Yellow;

                yield return prefixer + "<" + (ender ? "/" : "") + gr + (single ? "/" : "") + ">";
                
                if (single)
                {
                    RegisterFor(gr);
                    continue;
                }

                if (ender)
                {
                    DecreaseFor(gr);
                }
                else
                {
                    IncreaseFor(gr);

                    prefixer += INDENT_PREFIX;
                }
            }
        }

        public static void DisplayResults(bool hideGood)
        {
            int totalTags = tags.Count;
            int faults = 0;
            int good = 0;

            Console.ResetColor();
            Console.WriteLine($"Found { totalTags } different tags:");

            foreach (string key in tags.Keys)
            {
                int val = tags[key];

                if (val != 0)
                {
                    faults++;
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else
                {
                    good++;
                    Console.ForegroundColor = ConsoleColor.Green;
                }

                if (hideGood && val == 0)
                    continue;

                Console.WriteLine($"\t[{ key }] = { val }");
            }

            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"Different tags: { totalTags }");
            Console.WriteLine($"Faults: { faults }");
            Console.WriteLine($"Good: { good }");
        }

        public static void RegisterFor(string key)
        {
            key = key.Trim();

            if (!tags.ContainsKey(key))
                tags.Add(key, 0);
        }

        public static void IncreaseFor(string key)
        {
            RegisterFor(key);

            tags[key]++;
        }

        public static void DecreaseFor(string key)
        {
            RegisterFor(key);

            tags[key]--;
        }*/
    }
}
