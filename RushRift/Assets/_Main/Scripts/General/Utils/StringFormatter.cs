using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MyTools.Utils
{
    public static class StringFormatter
    {
        public static string ConvertStringFormat(string input)
        {
            // Check if the input string is null or empty
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Initialize a StringBuilder object for efficient string concatenation
            var sb = new StringBuilder();

            // Initialize variables for tracking word boundaries, capitalization, and previous character
            var isWordStart = true;
            var isFirstLetter = true;
            var previousChar = '\0';

            // Check if the input string is all uppercase
            var isAllUppercase = input.All(char.IsUpper);

            // Iterate through each character in the input string
            foreach (var c in input)
            {
                // Check if the current character is a space or an underscore
                if (c == ' ' || c == '_')
                {
                    // Set the word start flag to true for the next word
                    isWordStart = true;
                    isFirstLetter = true;
                }
                else
                {
                    // If it's the start of a word, capitalize the first letter and append a space if the previous character was uppercase
                    if (isWordStart)
                    {
                        if (!isAllUppercase)
                        {
                            sb.Append(char.ToUpper(c));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        isWordStart = false;

                        if (char.IsUpper(previousChar))
                        {
                            sb.Append(' ');
                        }
                    }
                    else
                    {
                        // If it's not the start of a word and the current character is uppercase, append a space if the previous character was lowercase
                        if (char.IsUpper(c) && char.IsLower(previousChar))
                        {
                            sb.Append(' ');
                        }

                        // Append the character
                        sb.Append(c);
                    }

                    // Update the previous character
                    previousChar = c;

                    // Reset the isFirstLetter flag for subsequent characters in the word
                    isFirstLetter = false;
                }
            }

            return sb.ToString();
        }

        public static string FormatToTimer(this float targetTime)
        {
            if (targetTime < 0f)
                return "--:--.---"; // fallback if no time recorded

            var minutes = (int)(targetTime / 60f);
            var seconds = (int)(targetTime % 60f);
            var milliseconds = (int)((targetTime * 1000f) % 1000f);

            return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
        }

        public static string FormatToClockTimer(this float targetTime)
        {
            if (targetTime < 0f)
                return "--:--:--"; // fallback if no time recorded

            var minutes = (int)(targetTime / 60f);
            var seconds = (int)(targetTime % 60f);
            var milliseconds = (int)((targetTime * 1000f) % 1000f);

            return $"{minutes:00}:{seconds:00}:{milliseconds:00}";
        }
    }
}
