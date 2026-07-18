using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Common
{
    // JSON yapısına uygun sınıf
    namespace Common
    {
        public class RootObject
        {
            [JsonPropertyName("type")]
            public string Type
            {
                get; set;
            }

            [JsonPropertyName("features")]
            public List<Feature> Features
            {
                get; set;
            }
        }

        public class Feature
        {
            [JsonPropertyName("type")]
            public string Type
            {
                get; set;
            }

            [JsonPropertyName("geometry")]
            public Geometry Geometry
            {
                get; set;
            }

            [JsonPropertyName("properties")]
            public Properties Properties
            {
                get; set;
            }
        }

        public class Geometry
        {
            [JsonPropertyName("type")]
            public string Type
            {
                get; set;
            }

            [JsonPropertyName("coordinates")]
            public Dictionary<string, Dictionary<string, Coordinate>> Coordinates
            {
                get; set;
            }
        }

        public class Coordinate
        {
            [JsonPropertyName("0")]
            public double Longitude
            {
                get; set;
            }

            [JsonPropertyName("1")]
            public double Latitude
            {
                get; set;
            }
        }

        public class Properties
        {
            [JsonPropertyName("ilceAd")]
            public string IlceAd
            {
                get; set;
            }

            [JsonPropertyName("mevkii")]
            public string Mevkii
            {
                get; set;
            }

            [JsonPropertyName("ilId")]
            public int IlId
            {
                get; set;
            }

            [JsonPropertyName("durum")]
            public string Durum
            {
                get; set;
            }

            [JsonPropertyName("ilceId")]
            public int IlceId
            {
                get; set;
            }

            [JsonPropertyName("zeminKmdurum")]
            public string ZeminKmdurum
            {
                get; set;
            }

            [JsonPropertyName("parselNo")]
            public string ParselNo
            {
                get; set;
            }

            [JsonPropertyName("mahalleAd")]
            public string MahalleAd
            {
                get; set;
            }

            [JsonPropertyName("ozet")]
            public string Ozet
            {
                get; set;
            }

            [JsonPropertyName("alan")]
            public string Alan
            {
                get; set;
            }

            [JsonPropertyName("adaNo")]
            public string AdaNo
            {
                get; set;
            }

            [JsonPropertyName("nitelik")]
            public string Nitelik
            {
                get; set;
            }

            [JsonPropertyName("ilAd")]
            public string IlAd
            {
                get; set;
            }

            [JsonPropertyName("mahalleId")]
            public int MahalleId
            {
                get; set;
            }

            [JsonPropertyName("pafta")]
            public string Pafta
            {
                get; set;
            }
        }
    }
}