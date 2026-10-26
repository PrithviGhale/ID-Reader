namespace AgeCheck.Core;

// AAMVA's published IIN list identifies the issuing authority. DAJ describes
// the cardholder's address and must not be presented as the issuing state.
// https://www.aamva.org/identity/issuer-identification-numbers-(iin)
public static class IssuingJurisdiction
{
    public static string? FromIin(string iin) => iin switch
    {
        "604426" => "Prince Edward Island", "604427" => "American Samoa", "604428" => "Quebec",
        "604429" => "Yukon", "604430" => "Northern Mariana Islands", "604431" => "Puerto Rico",
        "604432" => "Alberta", "604433" => "Nunavut", "604434" => "Northwest Territories",
        "636000" => "Virginia", "636001" => "New York", "636002" => "Massachusetts",
        "636003" => "Maryland", "636004" => "North Carolina", "636005" => "South Carolina",
        "636006" => "Connecticut", "636007" => "Louisiana", "636008" => "Montana",
        "636009" => "New Mexico", "636010" => "Florida", "636011" => "Delaware",
        "636012" => "Ontario", "636013" => "Nova Scotia", "636014" => "California",
        "636015" => "Texas", "636016" => "Newfoundland and Labrador", "636017" => "New Brunswick",
        "636018" => "Iowa", "636019" => "Guam", "636020" => "Colorado",
        "636021" => "Arkansas", "636022" => "Kansas", "636023" => "Ohio",
        "636024" => "Vermont", "636025" => "Pennsylvania", "636026" => "Arizona",
        "636027" => "U.S. State Department", "636028" => "British Columbia", "636029" => "Oregon",
        "636030" => "Missouri", "636031" => "Wisconsin", "636032" => "Michigan",
        "636033" => "Alabama", "636034" => "North Dakota", "636035" => "Illinois",
        "636036" => "New Jersey", "636037" => "Indiana", "636038" => "Minnesota",
        "636039" => "New Hampshire", "636040" => "Utah", "636041" => "Maine",
        "636042" => "South Dakota", "636043" => "District of Columbia", "636044" => "Saskatchewan",
        "636045" => "Washington", "636046" => "Kentucky", "636047" => "Hawaii",
        "636048" => "Manitoba", "636049" => "Nevada", "636050" => "Idaho",
        "636051" => "Mississippi", "636052" => "Rhode Island", "636053" => "Tennessee",
        "636054" => "Nebraska", "636055" => "Georgia", "636056" => "Coahuila",
        "636057" => "Hidalgo", "636058" => "Oklahoma", "636059" => "Alaska",
        "636060" => "Wyoming", "636061" => "West Virginia", "636062" => "U.S. Virgin Islands",
        _ => null
    };
}
