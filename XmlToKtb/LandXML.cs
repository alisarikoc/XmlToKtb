namespace XmlToKtb
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.landxml.org/schema/LandXML-1.2", IsNullable = false)]
    public class LandXML
    {
        /// <remarks/>
        public LandXMLUnits Units { get; set; }

        /// <remarks/>
        public LandXMLProject Project { get; set; }

        /// <remarks/>
        public LandXMLApplication Application { get; set; }

        /// <remarks/>
        public LandXMLAlignments Alignments { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
        public System.DateTime date { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "time")]
        public System.DateTime time { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal version { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string language { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool readOnly { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLUnits
    {
        /// <remarks/>
        public LandXMLUnitsMetric Metric { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLUnitsMetric
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string areaUnit { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string linearUnit { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string volumeUnit { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string temperatureUnit { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string pressureUnit { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string diameterUnit { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string angularUnit { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string directionUnit { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLProject
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLApplication
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string desc { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string manufacturer { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort version { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string manufacturerURL { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime timeStamp { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLAlignments
    {
        /// <remarks/>
        public LandXMLAlignmentsAlignment Alignment { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLAlignmentsAlignment
    {
        /// <remarks/>
        public LandXMLAlignmentsAlignmentCoordGeom CoordGeom { get; set; }

        /// <remarks/>
        public object CrossSects { get; set; }

        /// <remarks/>
        public LandXMLAlignmentsAlignmentProfile Profile { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal length { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal staStart { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string desc { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLAlignmentsAlignmentCoordGeom
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Curve", typeof(LandXMLAlignmentsAlignmentCoordGeomCurve))]
        [System.Xml.Serialization.XmlElementAttribute("Line", typeof(LandXMLAlignmentsAlignmentCoordGeomLine))]
        [System.Xml.Serialization.XmlElementAttribute("Spiral", typeof(LandXMLAlignmentsAlignmentCoordGeomSpiral))]
        public object[] Items { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLAlignmentsAlignmentCoordGeomCurve
    {
        /// <remarks/>
        public string Start { get; set; }

        /// <remarks/>
        public string Center { get; set; }

        /// <remarks/>
        public string End { get; set; }

        /// <remarks/>
        public string PI { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string rot { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal chord { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string crvType { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal delta { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal dirEnd { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal dirStart { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal external { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal length { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal midOrd { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal radius { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal tangent { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLAlignmentsAlignmentCoordGeomLine
    {
        /// <remarks/>
        public string Start { get; set; }

        /// <remarks/>
        public string End { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal dir { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal length { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLAlignmentsAlignmentCoordGeomSpiral
    {
        /// <remarks/>
        public string Start { get; set; }

        /// <remarks/>
        public string PI { get; set; }

        /// <remarks/>
        public string End { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal length { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public float radiusEnd { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public float radiusStart { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string rot { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string spiType { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal theta { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal totalY { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal totalX { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal tanLong { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal tanShort { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLAlignmentsAlignmentProfile
    {
        /// <remarks/>
        public LandXMLAlignmentsAlignmentProfileProfAlign ProfAlign { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLAlignmentsAlignmentProfileProfAlign
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("CircCurve", typeof(LandXMLAlignmentsAlignmentProfileProfAlignCircCurve))]
        [System.Xml.Serialization.XmlElementAttribute("PVI", typeof(string))]
        [System.Xml.Serialization.XmlElementAttribute("ParaCurve", typeof(LandXMLAlignmentsAlignmentProfileProfAlignParaCurve))]
        public object[] Items { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLAlignmentsAlignmentProfileProfAlignCircCurve
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal length { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal radius { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
    public class LandXMLAlignmentsAlignmentProfileProfAlignParaCurve
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal length { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; }
    }
}