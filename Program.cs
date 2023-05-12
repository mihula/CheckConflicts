using System;
using System.IO;
using System.Text;

namespace CheckConflicts
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = string.Empty;

            if (args.Length > 0)
            {
                path = args[0];
            }
            else
            {
                // debug
                path = @"c:\Provys\amcni_tst\_net\src\";
            }

            var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                //Console.WriteLine($"File: {file}");
                var isInConflict = false;
                var linesOfConflict = 0;
                var conflictText = new StringBuilder();
                using (var fs = File.OpenRead(file))
                using (var sr = new StreamReader(fs))
                {
                    string line = string.Empty;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("namespace"))
                        {
                            //Console.WriteLine("...namespace > end of looking");
                            break;
                        }
                        if (line.StartsWith("======="))
                        {
                            if (linesOfConflict == 0)
                            {
                                conflictText.Clear();
                                break;
                            }
                        }
                        if (isInConflict)
                        {
                            linesOfConflict++;
                        }
                        if (line.StartsWith("<<<<<<< HEAD"))
                        {
                            //Console.WriteLine("...found conflict tag");
                            isInConflict = true;
                        }
                        if (isInConflict)
                        {
                            conflictText.AppendLine(line);
                        }
                        if (line.StartsWith(">>>>>>>"))
                        {
                            //Console.WriteLine("...found end-conflict tag");
                            isInConflict = false;
                        }
                    }
                }
                if (conflictText.Length > 0)
                {
                    Console.WriteLine($"File has real conflict: {file}");
                    Console.WriteLine(conflictText.ToString());
                    Console.WriteLine(new string('-', 80));
                }
            }

            //Console.ReadKey();
        }
    }
}
