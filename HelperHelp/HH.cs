using Amazon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HelperHelp
{
    /// <summary>
    /// Provides global helper methods to all applications in the helper family
    /// </summary>
    public static class HH
    {
        /// <summary>
        /// Outputs text to the console
        /// </summary>
        /// <param name="text">The text to output</param>
        /// <param name="speak">If true, the text will be output</param>
        /// <param name="ln">If true, will use WriteLine instead of Write</param>
        public static void Spout(string text, bool speak, bool ln = true)
        {
            if (speak)
            {
                if (ln)
                {
                    Console.WriteLine(text);
                }
                else
                {
                    Console.Write(text);
                }
            }
        }

        /// <summary>
        /// Gets and returns the named region via its Name property value
        /// </summary>
        /// <param name="region">The name of the region property on the RegionEndpoint class</param>
        public static RegionEndpoint? GetRegionEndpoint(string region)
        {
            string reg = region;

            // this converts actual names (e.g. us-east-1) to short names found in code (e.g. USEast1)
            if (reg.Contains('-'))
            {
                // should be as so:
                // [0]: us
                // [1]: east
                // [2]: 1
                string[] pieces = reg.Split('-');
                if (pieces.Length == 3) {
                    reg = $"{pieces[0].ToUpper()}{char.ToUpper(pieces[1][0])}{pieces[1].Substring(1)}{pieces[2]}";
                }
            }

            Type regionEPType = typeof(RegionEndpoint);
            var regionInstance =
                (
                    from match in regionEPType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    where
                        match.FieldType == regionEPType &&
                        match.Name == reg
                    select match.GetValue(regionEPType)
                ).SingleOrDefault() as RegionEndpoint;

            return regionInstance;
        }
    }
}
