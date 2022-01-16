using System;
using System.IO;
using DecoyGenerator.Database;
using DecoyGenerator.ConsoleUtils;

namespace DecoyGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Fasta fastaLibrary = new Fasta(args[0], 9, 15);
            
            fastaLibrary.AddDecoys(type: "reverse");
            fastaLibrary.FixDuplicateSequencesInFastaEntries();
            fastaLibrary.Write(args[0] + ".reverse_decoys.fasta");
            
            //fastaLibrary.AddDecoys(type: "shuffle");
            //fastaLibrary.FixDuplicateSequencesInFastaEntries();
            //fastaLibrary.Write(args[0] + ".shuffled_decoys.fasta");

            Console.Write("Press any key to exit...");
            var _ = Console.ReadKey();

            /*Console.WriteLine();
            Console.WriteLine("---------- REPORT ----------");
            Console.WriteLine($" Proteins: {fastaLibrary.FastaEntries.Count}");
            Console.WriteLine($" Target Peptides: {fastaLibrary.NTargets}");
            Console.WriteLine($" Decoy Peptides: {fastaLibrary.NDecoys}");
            Console.WriteLine($" Peptides requiring >1 shuffle: {fastaLibrary.NShuffleEvents}");
            Console.WriteLine($" Peptides requiring mutation: {fastaLibrary.NMutationEvents}");
            Console.WriteLine("----------------------------");*/
        }
    }
}