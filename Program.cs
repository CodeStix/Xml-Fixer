using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace XmlFixer
{
    class Program
    {
        public static bool pauseOnError = true;
        public static bool beautify = false;

        private const char INDENT_PREFIX = ' ';

        public static void Main(string[] args)
        {
            //args = new string[] { "test.txt" };

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
            StreamWriter sw = new StreamWriter("beautiful.txt");
            StreamWriter swf = new StreamWriter("fixed.txt");

            Stack<string> toClose = new Stack<string>();
            string line = "";
            int lineNumber = 1;
            bool opened = false;
            bool ender = false;
            string indent = "";
            string tagName = "";
            //string poppedTagName = "";
            bool foundTagName = false;
            int level = 0;
            char prev = char.MinValue;
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
                    Queue<string> poppedTags = new Queue<string>();

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
                    Console.Write(lineNumber++ + ")\t" + (hereIsError ? "!!" : "  "));
                    Console.Write(indent);
                    if (beautify)
                        Console.WriteLine(line);
                    else
                        Console.Write(line);

                    sw.Write(indent);
                    if (beautify)
                        sw.WriteLine(line);
                    else
                        sw.Write(line);

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

                    if (prev != '/' && prev != '?' && !ender)
                    {
                        level++;
                        if (beautify)
                            indent += INDENT_PREFIX;

                        toClose.Push(tagName);
                    }

                    line = "";
                    tagName = "";
                    foundTagName = false;
                    opened = false;
                    ender = false;
                }

                prev = c;
            }

            sw.Close();

           
            while (toClose.Count > 0)
            {
                string pop = toClose.Pop();

                Console.Title = "Tag not closed: " + pop;
                Console.ForegroundColor = ConsoleColor.Red;
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

            Console.ResetColor();
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(" -------------------- END OF FILE -------------------- ");
            Console.WriteLine("Fixed file: fixed.xml");
            Console.WriteLine("Press enter to exit...");
            Console.ReadKey();
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
