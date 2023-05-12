using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                path = @"c:\Provys\mtva_proj\_net\src\";
            }

            var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                //Console.WriteLine($"File: {file}");
                var inConflictState = ConflictState.None;
                var conflictSet = new ConflictSet();

                using (var fs = File.OpenRead(file))
                using (var sr = new StreamReader(fs))
                {
                    string line = string.Empty;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        if (line.StartsWith("<<<<<<<"))
                        {
                            inConflictState = ConflictState.Head;
                            conflictSet.StartConflict();
                            continue;
                        }
                        if (line.StartsWith("======="))
                        {
                            inConflictState = ConflictState.Merge;
                            continue;
                        }
                        if (line.StartsWith(">>>>>>>"))
                        {
                            inConflictState = ConflictState.None;
                            conflictSet.EndConflict();
                            continue;
                        }
                        if (line.StartsWith("namespace"))
                        {
                            break;
                        }

                        if (conflictSet.ActiveConflict == null) continue;

                        if (line.ToLowerInvariant().Contains("// modification history")) continue; // zbytecne moc konfliktu, nepotrebujem toto
                        if (line.StartsWith("// -------------------------------------")) continue; // zbytecne moc konfliktu, nepotrebujem toto

                        line = line.Replace("prazak", "doubek").Trim(); // nazdar zdendo ty lumpe!

                        switch (inConflictState)
                        {
                            case ConflictState.None:
                                // nemelo by se to sem dostat - nemel by byt zadny conflict Active
                                continue;

                            case ConflictState.Head:
                                conflictSet.ActiveConflict.HeadLines.Add(line);
                                break;

                            case ConflictState.Merge:
                                conflictSet.ActiveConflict.MergeLines.Add(line);
                                break;
                        }
                    }
                }

                if (conflictSet.Evaluate())
                {
                    Console.WriteLine($"{file} <<< file has real conflict");
                    Console.WriteLine(conflictSet.ToString());
                    Console.WriteLine(new string('-', 80));
                }
            }

            //Console.ReadKey();
        }
    }

    enum ConflictState
    {
        None,
        Head,
        Merge
    }

    class ConflictSet
    {
        public List<Conflict> Conflicts { get; private set; } = new List<Conflict>();
        public Conflict ActiveConflict { get; private set; } = null;

        internal void StartConflict()
        {
            Conflict result = new Conflict();
            ActiveConflict = result;
            Conflicts.Add(result);
        }

        internal void EndConflict()
        {
            ActiveConflict = null;
        }

        internal bool Evaluate()
        {
            var result = false;
            foreach (var conflict in Conflicts)
            {
                result = conflict.Evaluate() | result;
            }
            return result;
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            foreach (var conflict in Conflicts)
            {
                if (conflict.HeadLines.Count > 0)
                    result.Append(conflict.ToString());
            }
            return result.ToString();
        }
    }

    public class Conflict
    {
        public List<string> HeadLines { get; private set; } = new List<string>();
        public List<string> MergeLines { get; private set; } = new List<string>();
        internal bool Evaluate()
        {
            // vsechny HeadLines co jsou v MergeLines vyhodim a pokud neco v head zustalo > true jinak >false (neni to conflict)
            var newHeadLines = HeadLines.Where(line => !MergeLines.Contains(line)).ToList();
            var newMergeLines = MergeLines.Where(line => !HeadLines.Contains(line)).ToList();
            HeadLines = newHeadLines;
            MergeLines = newMergeLines;
            return HeadLines.Count > 0;
        }
        public override string ToString()
        {
            var result = new StringBuilder();
            result.AppendLine("<<<<<<< HEAD");
            foreach(var line in HeadLines) result.AppendLine(line);
            result.AppendLine("============");
            foreach (var line in MergeLines) result.AppendLine(line);
            result.AppendLine("MERGE >>>>>>");
            return result.ToString();
        }
    }
}
