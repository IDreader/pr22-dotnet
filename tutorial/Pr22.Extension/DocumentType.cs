namespace Pr22.Extension
{
    /// <summary>
    /// Document identifier name converter.
    /// </summary>
    class DocumentType
    {
        /// <summary>
        /// Returns the name of a general document.
        /// </summary>
        /// <param name="id">The document code.</param>
        /// <returns>The name of a general document.</returns>
        public static string GetDocumentName(int id)
        {
            string[] values = {
                "Unknown document",
                "ICAO standard Passport (MRP)",
                "ICAO standard 2 row Travel Document (TD-2)",
                "ICAO standard 3 row Travel Document (TD-1)",
                "ICAO standard visa (MRV-A) (MRV-B)",
                "French ID card",
                "Pre ICAO standard 3 row Travel Document",
                "Slovak ID card",
                "AAMVA standard driving license",
                "Belgian ID",
                "Swiss driving license",
                "ID of Cote d'Ivoire",
                "Financial Transaction Card",
                "IATA boarding pass",
                "ICAO Travel Document (TD-1, front page, named)",
                "ICAO Travel Document (TD-1, front page, typed)",
                "ISO standard driving license",
                "Mail item",
                "ICAO standard electronic document (Passport/ID)",
                "EID",
                "ESign",
                "NFC",
                "European standard driving license",
                "Portuguese ID",
                "Ecuadorian ID",
                "ID card with MRZ",
                "USA military ID",
                "Vehicle Registration Document",
                "Local border traffic permit"
            };
            return id < 0 || id >= values.Length ? "" : values[id];
        }

        /// <summary>
        /// Returns the document type name.
        /// </summary>
        /// <param name="doc_type">Document type identifier string.</param>
        /// <returns>The name of the document type.</returns>
        public static string GetDocTypeName(string doc_type)
        {
            if (doc_type.StartsWith("DL"))
            {
                if (doc_type == "DLL") return "driving license for learner";
                else return "driving license";
            }
            else if (doc_type.StartsWith("ID"))
            {
                if (doc_type == "IDF") return "ID card for foreigner";
                else if (doc_type == "IDC") return "ID card for children";
                else return "ID card";
            }
            else if (doc_type.StartsWith("PP"))
            {
                if (doc_type == "PPD") return "diplomatic passport";
                else if (doc_type == "PPS") return "service passport";
                else if (doc_type == "PPE") return "emergency passport";
                else if (doc_type == "PPC") return "passport for children";
                else return "passport";
            }
            else if (doc_type.StartsWith("TD")) return "travel document";
            else if (doc_type.StartsWith("RP")) return "residence permit";
            else if (doc_type.StartsWith("VS")) return "visa";
            else if (doc_type.StartsWith("WP")) return "work permit";
            else if (doc_type.StartsWith("SI")) return "social insurance document";
            else return "document";
        }

        /// <summary>
        /// Returns the page name.
        /// </summary>
        /// <param name="doc_page">Document page identifier.</param>
        /// <returns>Document page name.</returns>
        public static string GetPageName(string doc_page)
        {
            if (doc_page == "F") return "front";
            else if (doc_page == "B") return "back";
            return "";
        }
    }
}
