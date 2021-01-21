namespace AzureSDKTests.Models
{
    using System.Collections.Generic;
    using System.Linq;
    
    public static class QueryHelper
    {
        
        
        public static IList<string> ExtractListValuesBetweenMarkersInQueryExpression(
            string query, 
            string valuesStartMarker, 
            string valuesStartAlternativeMarker, 
            string valuesEndMarker, 
            string valuesEndAlternativeMarker)
        {
            var startMarker = valuesStartMarker;
            var startMarkerIndex = query.IndexOf(valuesStartMarker);

            if (startMarkerIndex == -1)
            {
                startMarker = valuesStartAlternativeMarker;
                startMarkerIndex = query.IndexOf(startMarker);
            }
            var endMarkerIndex = -1;
            if (startMarkerIndex > -1)
            {
                var endMarker = valuesEndMarker;
                endMarkerIndex = query.IndexOf(valuesEndMarker, startMarkerIndex);
                if (endMarkerIndex == -1)
                {
                    endMarker = valuesEndAlternativeMarker;
                    endMarkerIndex = query.IndexOf(valuesEndAlternativeMarker, startMarkerIndex);
                }
                if (endMarkerIndex > startMarkerIndex)
                {
                    int endIndex = endMarkerIndex - startMarkerIndex;
                    var originalValuesString = Between(query,startMarker, endMarker);
                    var listValues = originalValuesString.Split(",").Select(x => x.Trim());
                    return originalValuesString.Split(",").Select(x => x.Trim()).ToArray().ToList();
                }
            }
            return null;
        }

        public static string AddVmToComputerGroupQuery(string query,string vmIdentifier, bool isIdentifierVmuuid) {

            var formattedVmIdentifier = "\"" + vmIdentifier + "\"";
            if (query == null || query == "")
            {
                if (isIdentifierVmuuid)
                {
                    return string.Format(Constants.DefaultKQLComputerGroupQueryFormat,"\"\"", formattedVmIdentifier);
                }
                else
                {
                    return string.Format(Constants.DefaultKQLComputerGroupQueryFormat, formattedVmIdentifier, "\"\"");
                }
            }

            var computers = ExtractListValuesBetweenMarkersInQueryExpression(query,
                "Computer in~ (",
                "Computer in (",
                ") or VMUUID in",
                ") | distinct"
                );
            var vmuuids = ExtractListValuesBetweenMarkersInQueryExpression(query,
                 "VMUUID in~ (",
                 "VMUUID in (",
                 ")",
                 ") | distinct"
                 );

            if (computers == null && vmuuids == null) 
            {
                throw new System.Exception($"Group query is not compatible: {query}");
            }

            if (isIdentifierVmuuid && !vmuuids.Contains(formattedVmIdentifier))
            {
                vmuuids.Remove("\"\"");
                vmuuids.Add(formattedVmIdentifier);
            }
            else if(!isIdentifierVmuuid && !computers.Contains(formattedVmIdentifier))
            {
                computers.Remove("\"\"");
                computers.Add(formattedVmIdentifier);
            }
            return string.Format(Constants.DefaultKQLComputerGroupQueryFormat,string.Join(",", computers), string.Join(",", vmuuids));
        }

        public static string Between(string str, string FirstString, string LastString)
        {
            str = str.Substring(str.IndexOf(FirstString) + FirstString.Length);
            return str.Substring(0, str.IndexOf(LastString));
        }

    }

    public class Constants
    {
        public const string DefaultKQLComputerGroupQueryFormat = "Heartbeat | where Computer in~ ({0}) or VMUUID in~ ({1}) | distinct Computer";
    }
}
