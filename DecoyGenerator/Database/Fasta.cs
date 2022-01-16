using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using DecoyGenerator.ConsoleUtils;
using DecoyGenerator.Extensions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DecoyGenerator.Database
{
    public class Fasta
    {
        public List<FastaEntry> FastaEntries { get; private set; }
        public List<FastaEntry> DecoyEntries { get; private set; }
        private string WholeProteome { get; set; }
        public Dictionary<string, string> UniqueDigestedEntries { get; private set; }
        private int MinPeptideLength { get; }
        private int MaxPeptideLength { get; }
        private List<char> AAs { get; } = "ARNDBCEEQZGHILKMFPSTWYVU".ToList();

        private object _lockThis = new object();

        private Random _random = new Random(0);

        public Fasta(string fastaFile, int minPeptideLength, int maxPeptideLength)
        {
            Console.WriteLine($"Loading {fastaFile}");
            MinPeptideLength = minPeptideLength;
            MaxPeptideLength = maxPeptideLength;
            using StreamReader fasta = new StreamReader(fastaFile);
            FastaEntries = ParseFasta(fasta);
            WholeProteome = string.Join(' ', from x in FastaEntries select x.Sequence);
        }

        public void AddFasta(string fastaFile)
        {
            Console.WriteLine($"Loading {fastaFile}");
            using StreamReader fasta = new StreamReader(fastaFile);
            FastaEntries = FastaEntries.Concat(ParseFasta(fasta)).ToList();
            WholeProteome = string.Join(' ', from x in FastaEntries select x.Sequence);
        }
        
        private static List<FastaEntry> ParseFasta(StreamReader fastaFile)
        {
            // thanks to https://rosettacode.org/wiki/FASTA_format#C.23
            List<FastaEntry> entries = new List<FastaEntry>();
            //FastaEntry f = null;
            string line;
            List<string> seqList = new List<string>();
            string desc = String.Empty;
            while ((line = fastaFile.ReadLine()) != null)
            {
                // ignore comment lines
                if (line.StartsWith(";"))
                    continue;
                if (line.StartsWith("#"))
                    continue;
                if (line.StartsWith(">"))
                {
                    if (seqList.Count > 0)
                    {
                        entries.Add(new FastaEntry()
                        {
                            Sequence = String.Join("", seqList),
                            Description = desc
                        });
                    }

                    // var tDesc = line.Split('|');

                    // desc = tDesc.Length > 1 ? $"{tDesc[0]}|{tDesc[1]}" : tDesc[0];

                    desc = line.Remove(0, 1);
                    
                    seqList.Clear();
                }
                else
                {
                    seqList.Add(line);
                }
            }
            // add the last entry
            entries.Add(new FastaEntry()
            {
                Sequence = String.Join("", seqList),
                Description = desc
            });
            return entries;
        }

        private IEnumerable<(int Start, int End, string Sequence)> GetSubstrings(string sequence)
        {
            var stringList = new List<(int Start, int End, string Sequence)>();
            char[] seq = sequence.ToCharArray();
            for (int length = MinPeptideLength; length <= MaxPeptideLength; length++)
            {
                for (int i = 0; i < sequence.Length - length; i++)
                {
                    stringList.Add((i, i + length - 1, new string(new ArraySegment<char>(seq, i, length))));
                }
            }
            return stringList;
        }

        public void AddDecoys(string type="reverse", string prefix="auto")
        {
            if (type != "reverse" && type != "shuffle")
            {
                var exception = new ArgumentException(
                    "The argument 'type' must be one of {reverse, shuffle}.");
                throw exception;
            }

            if (prefix == "auto")
            {
                if (type == "reverse")
                {
                    prefix = "rev_";
                }
                else
                {
                    prefix = "decoy_";
                }
            }
            Console.WriteLine($"Generating decoys - type: {type}");
            DecoyEntries = new List<FastaEntry>();
            foreach (var entry in this.FastaEntries)
            {
                FastaEntry decoyEntry = new FastaEntry();
                    
                decoyEntry.Description = prefix + entry.Description;
                if (type == "reverse")
                {
                    decoyEntry.Sequence = entry.Sequence.Reverse();
                }
                else if (type == "shuffle")
                {
                    decoyEntry.Sequence = entry.Sequence.Shuffle(_random);
                }
                DecoyEntries.Add(decoyEntry);
            }
        }

        private string MutateSequence(string seq, string proteinSequence = null)
        {
            string samplingSequence;
            if (proteinSequence == null || proteinSequence.Length < 100)
            {
                samplingSequence = WholeProteome;
            }
            else
            {
                samplingSequence = proteinSequence;
            }
            var i = _random.Next(0, seq.Length - 1);
            var j = _random.Next(0, samplingSequence.Length);
            var stringAsArray = seq.ToCharArray();
            stringAsArray[i] = samplingSequence[j];
            return new string(stringAsArray);
        }

        public void FixDuplicateSequencesInFastaEntries()
        {
            int nDuplicatesFound = 0; // keep track of how many peptides are present in decoys and targets
            int nMutationEvents = 0; // keep track of the total mutation events to resolve duplicates
            Dictionary<string, string> decoyFixes = new Dictionary<string, string>(); // a dictionary for fixed decoy entries
            HashSet<string> notFoundInTargets = new HashSet<string>();
            HashSet<string> targetPeptides = new HashSet<string>(); // a set of digested targets
            var P = new ProgressIndicator(total: FastaEntries.Count, message: "Generating hashset of digested targets"); 
            P.Start();
            foreach (var entry in FastaEntries)
            {
                var peptides = entry.Sequence.GetAllSubSequences(MinPeptideLength, MaxPeptideLength);
                targetPeptides.UnionWith(peptides);
                P.Update();
            }
            P.Done();
            
            P = new ProgressIndicator(total: DecoyEntries.Count, message: "Searching for decoy sequences in the targets");
            P.Start();
            for (int i = 0; i < DecoyEntries.Count; i++)
            {
                FastaEntry entry = DecoyEntries[i];
                var peptides = GetSubstrings(entry.Sequence);
                foreach (var peptide in peptides)
                {
                    var seq = entry.Sequence.Substring(peptide.Start, peptide.Sequence.Length);
                    if (decoyFixes.Keys.Contains(seq))
                    {
                        var tempArray = entry.Sequence.ToCharArray();
                        decoyFixes[seq].CopyTo(0, tempArray, peptide.Start, seq.Length);
                        entry.Sequence = new string(tempArray);
                    }
                    else if (notFoundInTargets.Contains(seq))
                    { }
                    else
                    {
                        string originalSeq = seq;
                        bool duplicateFound = false;

                        while (targetPeptides.Contains(seq))
                        {
                            duplicateFound = true;
                            nMutationEvents++;
                            seq = MutateSequence(seq, entry.Sequence);
                            decoyFixes[originalSeq] = seq;
                        }
                        if (duplicateFound)
                        {
                            nDuplicatesFound++;
                            var tempArray = entry.Sequence.ToCharArray();
                            seq.CopyTo(0, tempArray, peptide.Start, seq.Length);
                            entry.Sequence = new string(tempArray);
                        }
                        else
                        {
                            notFoundInTargets.Add(seq);
                        }
                    }
                }
                P.Update();
            }
            P.Done();
            Console.WriteLine($"Number of decoy sequences found in target sequences: {nDuplicatesFound}");
            Console.WriteLine($"Average number of point mutations per duplicate required to remove all duplicates: " +
                              $"{Math.Round((double)nMutationEvents/nDuplicatesFound, 2)}");
        }

        public void Write(string filepath)
        {
            using StreamWriter f = new StreamWriter(filepath);
            foreach (var entry in FastaEntries)
            {
                f.WriteLine(">" + entry.Description);
                f.WriteLine(entry.Sequence);
            }
            foreach (var entry in DecoyEntries)
            {
                f.WriteLine(">" + entry.Description);
                f.WriteLine(entry.Sequence);
            }
        }
    }
}