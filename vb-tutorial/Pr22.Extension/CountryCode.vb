Imports System.Collections.Generic

Namespace Pr22Extension

    Public Class CountryCode

        Shared values As CountryCode() = { _
        New CountryCode("AFG", "Afghanistan"), _
        New CountryCode("ALA", "Åland Islands"), _
        New CountryCode("ALB", "Albania"), _
        New CountryCode("DZA", "Algeria"), _
        New CountryCode("ASM", "American Samoa"), _
        New CountryCode("AND", "Andorra"), _
        New CountryCode("AGO", "Angola"), _
        New CountryCode("AIA", "Anguilla"), _
        New CountryCode("ATA", "Antarctica"), _
        New CountryCode("ATG", "Antigua and Barbuda"), _
        New CountryCode("ARG", "Argentina"), _
        New CountryCode("ARM", "Armenia"), _
        New CountryCode("ABW", "Aruba"), _
        New CountryCode("AUS", "Australia"), _
        New CountryCode("AUT", "Austria"), _
        New CountryCode("AZE", "Azerbaijan"), _
        New CountryCode("BHS", "Bahamas"), _
        New CountryCode("BHR", "Bahrain"), _
        New CountryCode("BGD", "Bangladesh"), _
        New CountryCode("BRB", "Barbados"), _
        New CountryCode("BLR", "Belarus"), _
        New CountryCode("BEL", "Belgium"), _
        New CountryCode("BLZ", "Belize"), _
        New CountryCode("BEN", "Benin"), _
        New CountryCode("BMU", "Bermuda"), _
        New CountryCode("BTN", "Bhutan"), _
        New CountryCode("BOL", "Bolivia"), _
        New CountryCode("BES", "Bonaire, Sint Eustatius and Saba"), _
        New CountryCode("BIH", "Bosnia and Herzegovina"), _
        New CountryCode("BWA", "Botswana"), _
        New CountryCode("BVT", "Bouvet Island"), _
        New CountryCode("BRA", "Brazil"), _
        New CountryCode("IOT", "British Indian Ocean Territory"), _
        New CountryCode("BRN", "Brunei Darussalam"), _
        New CountryCode("BGR", "Bulgaria"), _
        New CountryCode("BFA", "Burkina Faso"), _
        New CountryCode("BDI", "Burundi"), _
        New CountryCode("CPV", "Cabo Verde"), _
        New CountryCode("KHM", "Cambodia"), _
        New CountryCode("CMR", "Cameroon"), _
        New CountryCode("CAN", "Canada"), _
        New CountryCode("CYM", "Cayman Islands"), _
        New CountryCode("CAF", "Central African Republic"), _
        New CountryCode("TCD", "Chad"), _
        New CountryCode("CHL", "Chile"), _
        New CountryCode("CHN", "China"), _
        New CountryCode("CXR", "Christmas Island"), _
        New CountryCode("CCK", "Cocos (Keeling) Islands"), _
        New CountryCode("COL", "Colombia"), _
        New CountryCode("COM", "Comoros"), _
        New CountryCode("COD", "Democratic Republic of the Congo"), _
        New CountryCode("COG", "Congo"), _
        New CountryCode("COK", "Cook Islands"), _
        New CountryCode("CRI", "Costa Rica"), _
        New CountryCode("CIV", "Côte d'Ivoire"), _
        New CountryCode("HRV", "Croatia"), _
        New CountryCode("CUB", "Cuba"), _
        New CountryCode("CUW", "Curaçao"), _
        New CountryCode("CYP", "Cyprus"), _
        New CountryCode("CZE", "Czechia"), _
        New CountryCode("DNK", "Denmark"), _
        New CountryCode("DJI", "Djibouti"), _
        New CountryCode("DMA", "Dominica"), _
        New CountryCode("DOM", "Dominican Republic"), _
        New CountryCode("ECU", "Ecuador"), _
        New CountryCode("EGY", "Egypt"), _
        New CountryCode("SLV", "El Salvador"), _
        New CountryCode("GNQ", "Equatorial Guinea"), _
        New CountryCode("ERI", "Eritrea"), _
        New CountryCode("EST", "Estonia"), _
        New CountryCode("SWZ", "Eswatini"), _
        New CountryCode("ETH", "Ethiopia"), _
        New CountryCode("FLK", "Falkland Islands (Malvinas)"), _
        New CountryCode("FRO", "Faroe Islands"), _
        New CountryCode("FJI", "Fiji"), _
        New CountryCode("FIN", "Finland"), _
        New CountryCode("FRA", "France"), _
        New CountryCode("GUF", "French Guiana"), _
        New CountryCode("PYF", "French Polynesia"), _
        New CountryCode("ATF", "French Southern Territories"), _
        New CountryCode("GAB", "Gabon"), _
        New CountryCode("GMB", "Gambia"), _
        New CountryCode("GEO", "Georgia"), _
        New CountryCode("D", "Germany"), _
        New CountryCode("GHA", "Ghana"), _
        New CountryCode("GIB", "Gibraltar"), _
        New CountryCode("GRC", "Greece"), _
        New CountryCode("GRL", "Greenland"), _
        New CountryCode("GRD", "Grenada"), _
        New CountryCode("GLP", "Guadeloupe"), _
        New CountryCode("GUM", "Guam"), _
        New CountryCode("GTM", "Guatemala"), _
        New CountryCode("GGY", "Guernsey"), _
        New CountryCode("GIN", "Guinea"), _
        New CountryCode("GNB", "Guinea-Bissau"), _
        New CountryCode("GUY", "Guyana"), _
        New CountryCode("HTI", "Haiti"), _
        New CountryCode("HMD", "Heard and McDonald Islands"), _
        New CountryCode("VAT", "Holy See (Vatican City State)"), _
        New CountryCode("HND", "Honduras"), _
        New CountryCode("HKG", "Hong Kong"), _
        New CountryCode("HUN", "Hungary"), _
        New CountryCode("ISL", "Iceland"), _
        New CountryCode("IND", "India"), _
        New CountryCode("IDN", "Indonesia"), _
        New CountryCode("IRN", "Islamic Republic of Iran"), _
        New CountryCode("IRQ", "Iraq"), _
        New CountryCode("IRL", "Ireland"), _
        New CountryCode("IMN", "Isle of Man"), _
        New CountryCode("ISR", "Israel"), _
        New CountryCode("ITA", "Italy"), _
        New CountryCode("JAM", "Jamaica"), _
        New CountryCode("JPN", "Japan"), _
        New CountryCode("JEY", "Jersey"), _
        New CountryCode("JOR", "Jordan"), _
        New CountryCode("KAZ", "Kazakhstan"), _
        New CountryCode("KEN", "Kenya"), _
        New CountryCode("KIR", "Kiribati"), _
        New CountryCode("PRK", "Democratic People's Republic of Korea"), _
        New CountryCode("KOR", "Republic of Korea"), _
        New CountryCode("KWT", "Kuwait"), _
        New CountryCode("KGZ", "Kyrgyzstan"), _
        New CountryCode("LAO", "Lao People's Democratic Republic"), _
        New CountryCode("LVA", "Latvia"), _
        New CountryCode("LBN", "Lebanon"), _
        New CountryCode("LSO", "Lesotho"), _
        New CountryCode("LBR", "Liberia"), _
        New CountryCode("LBY", "Libya"), _
        New CountryCode("LIE", "Liechtenstein"), _
        New CountryCode("LTU", "Lithuania"), _
        New CountryCode("LUX", "Luxembourg"), _
        New CountryCode("MAC", "Macao"), _
        New CountryCode("MDG", "Madagascar"), _
        New CountryCode("MWI", "Malawi"), _
        New CountryCode("MYS", "Malaysia"), _
        New CountryCode("MDV", "Maldives"), _
        New CountryCode("MLI", "Mali"), _
        New CountryCode("MLT", "Malta"), _
        New CountryCode("MHL", "Marshall Islands"), _
        New CountryCode("MTQ", "Martinique"), _
        New CountryCode("MRT", "Mauritania"), _
        New CountryCode("MUS", "Mauritius"), _
        New CountryCode("MYT", "Mayotte"), _
        New CountryCode("MEX", "Mexico"), _
        New CountryCode("FSM", "Federated States of Micronesia"), _
        New CountryCode("MDA", "Republic of Moldova"), _
        New CountryCode("MCO", "Monaco"), _
        New CountryCode("MNG", "Mongolia"), _
        New CountryCode("MNE", "Montenegro"), _
        New CountryCode("MSR", "Montserrat"), _
        New CountryCode("MAR", "Morocco"), _
        New CountryCode("MOZ", "Mozambique"), _
        New CountryCode("MMR", "Myanmar"), _
        New CountryCode("NAM", "Namibia"), _
        New CountryCode("NRU", "Nauru"), _
        New CountryCode("NPL", "Nepal"), _
        New CountryCode("NLD", "Netherlands"), _
        New CountryCode("NCL", "New Caledonia"), _
        New CountryCode("NZL", "New Zealand"), _
        New CountryCode("NIC", "Nicaragua"), _
        New CountryCode("NER", "Niger"), _
        New CountryCode("NGA", "Nigeria"), _
        New CountryCode("NIU", "Niue"), _
        New CountryCode("NFK", "Norfolk Island"), _
        New CountryCode("MKD", "North Macedonia"), _
        New CountryCode("MNP", "Northern Mariana Islands"), _
        New CountryCode("NOR", "Norway"), _
        New CountryCode("OMN", "Oman"), _
        New CountryCode("PAK", "Pakistan"), _
        New CountryCode("PLW", "Palau"), _
        New CountryCode("PSE", "State of Palestine"), _
        New CountryCode("PAN", "Panama"), _
        New CountryCode("PNG", "Papua New Guinea"), _
        New CountryCode("PRY", "Paraguay"), _
        New CountryCode("PER", "Peru"), _
        New CountryCode("PHL", "Philippines"), _
        New CountryCode("PCN", "Pitcairn"), _
        New CountryCode("POL", "Poland"), _
        New CountryCode("PRT", "Portugal"), _
        New CountryCode("PRI", "Puerto Rico"), _
        New CountryCode("QAT", "Qatar"), _
        New CountryCode("REU", "Réunion"), _
        New CountryCode("ROU", "Romania"), _
        New CountryCode("RUS", "Russian Federation"), _
        New CountryCode("RWA", "Rwanda"), _
        New CountryCode("BLM", "Saint Barthélemy"), _
        New CountryCode("SHN", "Saint Helena"), _
        New CountryCode("KNA", "Saint Kitts and Nevis"), _
        New CountryCode("LCA", "Saint Lucia"), _
        New CountryCode("MAF", "Saint Martin (French)"), _
        New CountryCode("SPM", "Saint Pierre and Miquelon"), _
        New CountryCode("VCT", "Saint Vincent and the Grenadines"), _
        New CountryCode("WSM", "Samoa"), _
        New CountryCode("SMR", "San Marino"), _
        New CountryCode("STP", "Sao Tome and Principe"), _
        New CountryCode("SAU", "Saudi Arabia"), _
        New CountryCode("SEN", "Senegal"), _
        New CountryCode("SRB", "Serbia"), _
        New CountryCode("SYC", "Seychelles"), _
        New CountryCode("SLE", "Sierra Leone"), _
        New CountryCode("SGP", "Singapore"), _
        New CountryCode("SXM", "Sint Maarten (Dutch)"), _
        New CountryCode("SVK", "Slovakia"), _
        New CountryCode("SVN", "Slovenia"), _
        New CountryCode("SLB", "Solomon Islands"), _
        New CountryCode("SOM", "Somalia"), _
        New CountryCode("ZAF", "South Africa"), _
        New CountryCode("SGS", "South Georgia and the South Sandwich Islands"), _
        New CountryCode("SSD", "South Sudan"), _
        New CountryCode("ESP", "Spain"), _
        New CountryCode("LKA", "Sri Lanka"), _
        New CountryCode("SDN", "Sudan"), _
        New CountryCode("SUR", "Suriname"), _
        New CountryCode("SJM", "Svalbard and Jan Mayen Islands"), _
        New CountryCode("SWE", "Sweden"), _
        New CountryCode("CHE", "Switzerland"), _
        New CountryCode("SYR", "Syrian Arab Republic"), _
        New CountryCode("TWN", "Taiwan, Republic of China"), _
        New CountryCode("TJK", "Tajikistan"), _
        New CountryCode("TZA", "United Republic of Tanzania"), _
        New CountryCode("THA", "Thailand"), _
        New CountryCode("TLS", "Timor-Leste"), _
        New CountryCode("TGO", "Togo"), _
        New CountryCode("TKL", "Tokelau"), _
        New CountryCode("TON", "Tonga"), _
        New CountryCode("TTO", "Trinidad and Tobago"), _
        New CountryCode("TUN", "Tunisia"), _
        New CountryCode("TUR", "Turkey"), _
        New CountryCode("TKM", "Turkmenistan"), _
        New CountryCode("TCA", "Turks and Caicos Islands"), _
        New CountryCode("TUV", "Tuvalu"), _
        New CountryCode("UGA", "Uganda"), _
        New CountryCode("UKR", "Ukraine"), _
        New CountryCode("ARE", "United Arab Emirates"), _
        New CountryCode("GBR", "United Kingdom"), _
        New CountryCode("UMI", "United States Minor Outlying Islands"), _
        New CountryCode("USA", "United States of America"), _
        New CountryCode("URY", "Uruguay"), _
        New CountryCode("UZB", "Uzbekistan"), _
        New CountryCode("VUT", "Vanuatu"), _
        New CountryCode("VEN", "Venezuela"), _
        New CountryCode("VNM", "Viet Nam"), _
        New CountryCode("VGB", "Virgin Islands (British)"), _
        New CountryCode("VIR", "Virgin Islands (U.S.)"), _
        New CountryCode("WLF", "Wallis and Futuna Islands"), _
        New CountryCode("ESH", "Western Sahara"), _
        New CountryCode("YEM", "Yemen"), _
        New CountryCode("ZMB", "Zambia"), _
        New CountryCode("ZWE", "Zimbabwe"), _
 _
        New CountryCode("GBD", "British Overseas Territories Citizen"), _
        New CountryCode("GBN", "British National (Overseas)"), _
        New CountryCode("GBO", "British Overseas Citizen"), _
        New CountryCode("GBP", "British Protected Person"), _
        New CountryCode("GBS", "British Subject"), _
 _
        New CountryCode("UNO", "United Nations"), _
        New CountryCode("UNA", "United Nations"), _
        New CountryCode("UNK", "Kosovo (United Nations)"), _
 _
        New CountryCode("XBA", "African Development Bank"), _
        New CountryCode("XIM", "African Export-Import Bank"), _
        New CountryCode("XCC", "Caribbean Community"), _
        New CountryCode("XCE", "Council of Europe"), _
        New CountryCode("XCO", "Common Market for Eastern and Southern Africa"), _
        New CountryCode("XEC", "Economic Community of West African States"), _
        New CountryCode("XPO", "Interpol"), _
        New CountryCode("XES", "Organization of Eastern Caribbean States"), _
        New CountryCode("XMP", "Parliamentary Assembly of the Mediterranean"), _
        New CountryCode("XOM", "Sovereign Military Order of Malta"), _
        New CountryCode("XDC", "Southern African Development Community"), _
 _
        New CountryCode("XXA", "Stateless"), _
        New CountryCode("XXB", "Refugee"), _
        New CountryCode("XXC", "Refugee"), _
        New CountryCode("XXX", "Unspecified"), _
 _
        New CountryCode("EUE", "European Union"), _
        New CountryCode("RKS", "Republic of Kosovo"), _
        New CountryCode("XCT", "North Cyprus"), _
 _
        New CountryCode("ANT", "Netherlands Antilles"), _
        New CountryCode("NTZ", "Neutral Zone"), _
        New CountryCode("SCG", "Serbia and Montenegro"), _
        New CountryCode("YUG", "Yugoslavia"), _
        New CountryCode("FXX", "France, Metropolitan"), _
 _
        New CountryCode("IAO", "International Civil Aviation Organization")}

        Private m_name As String
        Private m_code As String

        Shared countries_code As New SortedDictionary(Of String, String)()

        Private Sub New(ByVal code As String, ByVal name As String)
            Me.m_code = code
            Me.m_name = name
        End Sub

        Shared Sub New()
            For i As Integer = 0 To values.Length - 1
                Dim cc As CountryCode = values(i)
                countries_code(cc.Code) = cc.Name
            Next
        End Sub

        Private ReadOnly Property Code() As String
            Get
                Return m_code
            End Get
        End Property
        Private ReadOnly Property Name() As String
            Get
                Return m_name
            End Get
        End Property

        Public Shared Function GetName(ByVal code As String) As String
            Dim value As String = ""
            If countries_code.TryGetValue(code, value) Then Return value
            Return ""
        End Function
    End Class
End Namespace
