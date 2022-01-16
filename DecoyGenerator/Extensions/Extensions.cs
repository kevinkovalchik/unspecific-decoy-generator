using System;
using System.Collections.Generic;

namespace DecoyGenerator.Extensions
{
    public static class Extensions
    {
        private static void KnuthShuffle<T>(IList<T> array, Random random = null)
        {
            if (random == null)
            {
                random = new System.Random();
            }
            for (int i = 0; i < array.Count; i++)
            {
                int j = random.Next(i, array.Count);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        public static string Shuffle(this string str, Random random = null)
        {
            char [] stringAsCharArray = str.ToCharArray();
            KnuthShuffle(stringAsCharArray, random);
            return new string(stringAsCharArray);
        }

        public static string Reverse(this string str)
        {
            char[] stringAsArray = str.ToCharArray();
            Array.Reverse(stringAsArray);
            return new string(stringAsArray);
        }
        
        public static IList<string> GetAllSubSequences(
            this string sequence, int minPeptideLength, int maxPeptideLength)
        {
            var stringList = new List<string>();
            char[] seq = sequence.ToCharArray();
            for (int length = minPeptideLength; length <= maxPeptideLength; length++)
            {
                for (int i = 0; i < sequence.Length - length; i++)
                {
                    stringList.Add(new string(new ArraySegment<char>(seq, i, length)));
                }
            }
            return stringList;
        }
    }
}