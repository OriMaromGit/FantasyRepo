using FuzzySharp;

public static class NameComparer
{
    /// <summary>
    /// Calculates the Jaro-Winkler similarity between two strings.
    /// Jaro-Winkler is a string comparison algorithm that gives a score between 0 and 1,
    /// where 1 indicates an exact match. It considers:
    /// 
    /// - Matching characters: Characters that are the same and within a certain distance from each other.
    /// - Transpositions: Matching characters that appear in different order (penalized).
    /// - Common prefix: Gives a bonus if the beginning of the strings match (up to 4 characters).
    /// 
    /// This method is useful for detecting approximate string matches, especially when minor typos
    /// or formatting differences exist. Often used in fuzzy name matching, data deduplication, etc.
    /// </summary>
    /// <param name="s1">First string to compare</param>
    /// <param name="s2">Second string to compare</param>
    /// <returns>A similarity score between 0.0 (completely different) and 1.0 (identical)</returns>
    private static double ComputeJaroWinklerScore(string s1, string s2)
    {
        if (s1 == s2)
        {
            return 1.0;
        }

        var len1 = s1.Length;
        var len2 = s2.Length;
        int matchDistance = Math.Max(len1, len2) / 2 - 1;

        bool[] s1Matches = new bool[len1];
        bool[] s2Matches = new bool[len2];

        int matches = 0;

        for (int i = 0; i < len1; i++)
        {
            int start = Math.Max(0, i - matchDistance);
            int end = Math.Min(i + matchDistance + 1, len2);

            for (int j = start; j < end; j++)
            {
                if (s2Matches[j]) continue;
                if (s1[i] != s2[j]) continue;

                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
        {
            return 0;
        }

        double t = 0;
        int k = 0;
        for (int i = 0; i < len1; i++)
        {
            if (!s1Matches[i]) continue;
            while (!s2Matches[k]) k++;
            if (s1[i] != s2[k]) t++;
            k++;
        }

        t /= 2;

        double jaro = ((double)matches / len1 + (double)matches / len2 + (matches - t) / matches) / 3.0;

        int prefix = 0;
        for (int i = 0; i < Math.Min(4, Math.Min(len1, len2)); i++)
        {
            if (s1[i] == s2[i]) prefix++;
            else break;
        }

        return jaro + prefix * 0.1 * (1 - jaro);
    }

    /// <summary>
    /// Computes a combined similarity score between two strings using both FuzzySharp and Jaro-Winkler algorithms.
    /// 
    /// - FuzzySharp (based on Levenshtein distance):
    ///   • TokenSortRatio: Compares strings after sorting their words alphabetically (normalizes word order).
    ///   • PartialRatio: Finds the best matching substrings (useful when one name contains the other).
    ///   These return values between 0 and 100.
    ///         
    /// - Jaro-Winkler: Calculates a character-level similarity score emphasizing common prefixes and penalizing transpositions,
    ///   also scaled to 0–100 for comparison.
    /// 
    /// The final score is a simple average of all three metrics, providing a robust estimate of how similar two names are.
    /// </summary>
    /// <param name="name1">First name to compare</param>
    /// <param name="name2">Second name to compare</param>
    /// <returns>Average similarity score between 0 and 100</returns>
    public static double GetCombinedScore(string name1, string name2)
    {
        var tokenSortRatio = Fuzz.TokenSortRatio(name1, name2);
        var partialRatio = Fuzz.PartialRatio(name1, name2);
        var jaroWinklerScore = ComputeJaroWinklerScore(name1, name2) * 100;

        // TODO: think about weights for each metric
        return (tokenSortRatio + partialRatio + jaroWinklerScore) / 3.0;
    }
}


///   - Levenshtein distance measures how many single-character edits (insertions, deletions, or substitutions) are needed to change one string into another, 
///     making it a common algorithm for detecting small typographical differences between strings.
///     For example:
///         Levenshtein("kitten", "sitting") = 3
///         (Substitute 'k'→'s', 'e'→'i', and insert 'g' at the end)
///         
