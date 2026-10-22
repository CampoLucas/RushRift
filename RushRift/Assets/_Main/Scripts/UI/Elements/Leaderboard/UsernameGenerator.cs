using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Generates random usernames from patterns such as [n][##][i][n].
/// </summary>
public class UsernameGenerator : MonoBehaviour
{
    [SerializeField] private List<string> patterns = new List<string>();
    [SerializeField] private List<string> names = new List<string>();
    [SerializeField] private List<string> icons = new List<string>();

    private static readonly Regex TokenRegex = new Regex(@"\[([^\]]*)\]");
    private static readonly Regex NumberRegex = new Regex(@"^#+$");

    public string GenerateUsername()
    {
        if (patterns.Count == 0)
        {
            Debug.LogWarning("UsernameGenerator: the patterns list is empty.", this);
            return string.Empty;
        }

        string pattern = patterns[Random.Range(0, patterns.Count)];
        StringBuilder builder = new StringBuilder();

        foreach (Match match in TokenRegex.Matches(pattern))
        {
            builder.Append(ResolveToken(match.Groups[1].Value));
        }

        return builder.ToString();
    }

    [ContextMenu("Log Username")]
    private void LogUsername()
    {
        Debug.Log(GenerateUsername(), this);
    }

    private string ResolveToken(string token)
    {
        if (token == "n")
        {
            return Pick(names, "names");
        }

        if (token == "i")
        {
            return Pick(icons, "icons");
        }

        if (NumberRegex.IsMatch(token))
        {
            int max = 1;
            for (int i = 1; i < token.Length; i++)
            {
                max *= 10;
            }
            return Random.Range(0, max + 1).ToString();
        }

        Debug.LogWarning($"UsernameGenerator: unknown token [{token}], skipping it.", this);
        return string.Empty;
    }

    private string Pick(List<string> pool, string poolName)
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning($"UsernameGenerator: the {poolName} list is empty, skipping the token.", this);
            return string.Empty;
        }

        return pool[Random.Range(0, pool.Count)];
    }
}
