using System.Globalization;
using UnityEngine.UIElements;

namespace Game.Utils
{
    public static class StringUtils
    {
        /// <summary>
        /// Computes the FNV-1a hash for the string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int ComputeFNV1aHash(this string str)
        {
            var hash = (uint)2166136261;
            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                hash = (hash ^ c) * 16777619;
            }

            return unchecked((int)hash);
        }

        public static string ToUpper(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.ToUpper();
        }
        
        public static string ToLower(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.ToLower();
        }
        
        public static string ToTitle(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(input.ToLower());
        }
        
        public static string ToSentenceCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            input = input.ToLower();
            return char.ToUpper(input[0]) + input.Substring(1);
        }

        public static string RemoveSpaces(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Replace(" ", "");
        }

        public static string CapitalizeEachWord(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            string[] words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
            return string.Join(" ", words);
        }
        
        private const string Ellipsis = "…";

        /// <summary>
        /// Trims a string to fit inside the width of a TextElement (Label, Button, etc).
        /// </summary>
        public static string TrimToVisualElement(string text, TextElement element)
        {
            if (string.IsNullOrEmpty(text) || element == null)
                return text;

            var availableWidth = element.resolvedStyle.width;

            // If element has no valid layout yet, skip
            if (float.IsNaN(availableWidth) || availableWidth <= 0)
                return text;

            var textWidth = MeasureTextWidth(text, element);
            if (textWidth <= availableWidth)
                return text;

            var trimmed = text;
            while (trimmed.Length > 0 && MeasureTextWidth(trimmed + Ellipsis, element) > availableWidth)
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }

            return trimmed + Ellipsis;
        }

        /// <summary>
        /// Trim string based on a max pixel width.
        /// </summary>
        public static string TrimToWidth(string text, float maxWidth, TextElement element)
        {
            if (string.IsNullOrEmpty(text) || element == null)
                return text;

            if (MeasureTextWidth(text, element) <= maxWidth)
                return text;

            var trimmed = text;
            while (trimmed.Length > 0 && MeasureTextWidth(trimmed + Ellipsis, element) > maxWidth)
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }

            return trimmed + Ellipsis;
        }

        /// <summary>
        /// Trim string based on character count.
        /// </summary>
        public static string TrimToLength(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
                return text;

            if (maxChars <= 0)
                return Ellipsis;

            return text.Substring(0, maxChars - 1) + Ellipsis;
        }

        /// <summary>
        /// Measures the text width using the TextElement’s style and font settings.
        /// </summary>
        private static float MeasureTextWidth(string text, TextElement element)
        {
            // MeasureTextSize is available for TextElement in UI Toolkit
            var size = element.MeasureTextSize(
                text,
                0,
                VisualElement.MeasureMode.Undefined,
                0,
                VisualElement.MeasureMode.Undefined
            );

            return size.x;
        }
    }
}
