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
    }
}