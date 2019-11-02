
// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.

using System;
using System.ComponentModel;
using System.Xml.Serialization;

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
[XmlRoot(Namespace = "http://www.landxml.org/schema/LandXML-1.2", IsNullable = false)]



public class LandXML
{
    private LandXMLProject projectField;
    private LandXMLAlignments alignmentsField;
    private DateTime timeField;
    private string languageField;

    /// <remarks/>
    public LandXMLUnits Units { get; set; }

    /// <remarks/>
    public LandXMLProject Project
    {
        get
        {
            return projectField;
        }
        set
        {
            projectField = value;
        }
    }

    /// <remarks/>
    public LandXMLApplication Application { get; set; }

    /// <remarks/>
    public LandXMLAlignments Alignments
    {
        get
        {
            return alignmentsField;
        }
        set
        {
            alignmentsField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute(DataType = "date")]
    public DateTime date { get; set; }

    /// <remarks/>
    [XmlAttribute(DataType = "time")]
    public DateTime time
    {
        get
        {
            return timeField;
        }
        set
        {
            timeField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string version { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string language
    {
        get
        {
            return languageField;
        }
        set
        {
            languageField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public bool readOnly { get; set; }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLUnits
{

    /// <remarks/>
    public LandXMLUnitsMetric Metric { get; set; }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLUnitsMetric
{
    private string linearUnitField;
    private string temperatureUnitField;
    private string diameterUnitField;
    private string directionUnitField;

    /// <remarks/>
    [XmlAttribute]
    public string areaUnit { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string linearUnit
    {
        get
        {
            return linearUnitField;
        }
        set
        {
            linearUnitField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string volumeUnit { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string temperatureUnit
    {
        get
        {
            return temperatureUnitField;
        }
        set
        {
            temperatureUnitField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string pressureUnit { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string diameterUnit
    {
        get
        {
            return diameterUnitField;
        }
        set
        {
            diameterUnitField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string angularUnit { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string directionUnit
    {
        get
        {
            return directionUnitField;
        }
        set
        {
            directionUnitField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLProject
{

    /// <remarks/>
    [XmlAttribute]
    public string name { get; set; }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLApplication
{
    private string descField;
    private string versionField;
    private DateTime timeStampField;

    /// <remarks/>
    [XmlAttribute]
    public string name { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string desc
    {
        get
        {
            return descField;
        }
        set
        {
            descField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string manufacturer { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string version
    {
        get
        {
            return versionField;
        }
        set
        {
            versionField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string manufacturerURL { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public DateTime timeStamp
    {
        get
        {
            return timeStampField;
        }
        set
        {
            timeStampField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLAlignments
{
    private string nameField;

    /// <remarks/>
    public LandXMLAlignmentsAlignment Alignment { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string name
    {
        get
        {
            return nameField;
        }
        set
        {
            nameField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLAlignmentsAlignment
{
    private LandXMLAlignmentsAlignmentProfile profileField;
    private LandXMLAlignmentsAlignmentCant cantField;
    private decimal lengthField;
    private string descField;

    /// <remarks/>
    public LandXMLAlignmentsAlignmentCoordGeom CoordGeom { get; set; }

    /// <remarks/>
    public LandXMLAlignmentsAlignmentProfile Profile
    {
        get
        {
            return profileField;
        }
        set
        {
            profileField = value;
        }
    }

    /// <remarks/>
    public LandXMLAlignmentsAlignmentCant Cant
    {
        get
        {
            return this.cantField;
        }
        set
        {
            this.cantField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string name { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal length
    {
        get
        {
            return lengthField;
        }
        set
        {
            lengthField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public decimal staStart { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string desc
    {
        get
        {
            return descField;
        }
        set
        {
            descField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLAlignmentsAlignmentCoordGeom
{

    /// <remarks/>
    [XmlElement("Curve", typeof(LandXMLAlignmentsAlignmentCoordGeomCurve))]
    [XmlElement("Line", typeof(LandXMLAlignmentsAlignmentCoordGeomLine))]
    [XmlElement("Spiral", typeof(LandXMLAlignmentsAlignmentCoordGeomSpiral))]
    public object[] Items { get; set; }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLAlignmentsAlignmentCoordGeomCurve
{
    private string centerField;
    private string piField;
    private decimal chordField;
    private decimal deltaField;
    private decimal dirStartField;
    private decimal lengthField;
    private decimal radiusField;

    /// <remarks/>
    public string Start { get; set; }

    /// <remarks/>
    public string Center
    {
        get
        {
            return centerField;
        }
        set
        {
            centerField = value;
        }
    }

    /// <remarks/>
    public string End { get; set; }

    /// <remarks/>
    public string PI
    {
        get
        {
            return piField;
        }
        set
        {
            piField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string rot { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal chord
    {
        get
        {
            return chordField;
        }
        set
        {
            chordField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string crvType { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal delta
    {
        get
        {
            return deltaField;
        }
        set
        {
            deltaField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public decimal dirEnd { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal dirStart
    {
        get
        {
            return dirStartField;
        }
        set
        {
            dirStartField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public decimal external { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal length
    {
        get
        {
            return lengthField;
        }
        set
        {
            lengthField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public decimal midOrd { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal radius
    {
        get
        {
            return radiusField;
        }
        set
        {
            radiusField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public decimal tangent { get; set; }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLAlignmentsAlignmentCoordGeomLine
{
    private string endField;
    private decimal lengthField;

    /// <remarks/>
    public string Start { get; set; }

    /// <remarks/>
    public string End
    {
        get
        {
            return endField;
        }
        set
        {
            endField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public decimal dir { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal length
    {
        get
        {
            return lengthField;
        }
        set
        {
            lengthField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLAlignmentsAlignmentCoordGeomSpiral
{
    private string piField;
    private decimal lengthField;
    private string radiusStartField;
    private string spiTypeField;
    private decimal totalYField;
    private decimal tanLongField;

    /// <remarks/>
    public string Start { get; set; }

    /// <remarks/>
    public string PI
    {
        get
        {
            return piField;
        }
        set
        {
            piField = value;
        }
    }

    /// <remarks/>
    public string End { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal length
    {
        get
        {
            return lengthField;
        }
        set
        {
            lengthField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string radiusEnd { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string radiusStart
    {
        get
        {
            return radiusStartField;
        }
        set
        {
            radiusStartField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string rot { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string spiType
    {
        get
        {
            return spiTypeField;
        }
        set
        {
            spiTypeField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public decimal theta { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal totalY
    {
        get
        {
            return totalYField;
        }
        set
        {
            totalYField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public decimal totalX { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal tanLong
    {
        get
        {
            return tanLongField;
        }
        set
        {
            tanLongField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public decimal tanShort { get; set; }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLAlignmentsAlignmentProfile
{
    private string nameField;

    /// <remarks/>
    public LandXMLAlignmentsAlignmentProfileProfAlign ProfAlign { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string name
    {
        get
        {
            return nameField;
        }
        set
        {
            nameField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLAlignmentsAlignmentProfileProfAlign
{
    private string nameField;

    /// <remarks/>
    [XmlElement("CircCurve", typeof(LandXMLAlignmentsAlignmentProfileProfAlignCircCurve))]
    [XmlElement("PVI", typeof(LandXMLAlignmentsAlignmentProfileProfAlignPVI))]
    [XmlElement("ParaCurve", typeof(LandXMLAlignmentsAlignmentProfileProfAlignParaCurve))]
    public object[] Items { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public string name
    {
        get
        {
            return nameField;
        }
        set
        {
            nameField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLAlignmentsAlignmentProfileProfAlignCircCurve
{
    private decimal radiusField;

    /// <remarks/>
    [XmlAttribute]
    public decimal length { get; set; }

    /// <remarks/>
    [XmlAttribute]
    public decimal radius
    {
        get
        {
            return radiusField;
        }
        set
        {
            radiusField = value;
        }
    }

    /// <remarks/>
    [XmlText]
    public string Value { get; set; }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLAlignmentsAlignmentProfileProfAlignParaCurve
{
    private string valueField;

    /// <remarks/>
    [XmlAttribute]
    public decimal length { get; set; }

    /// <remarks/>
    [XmlText]
    public string Value
    {
        get
        {
            return valueField;
        }
        set
        {
            valueField = value;
        }
    }
}
public class LandXMLAlignmentsAlignmentProfileProfAlignPVI
{
    private string valueField;

    /// <remarks/>
    [XmlAttribute]
    public decimal length { get; set; }

    /// <remarks/>
    [XmlText]
    public string Value
    {
        get
        {
            return valueField;
        }
        set
        {
            valueField = value;
        }
    }
}
/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCant
{

    private LandXMLAlignmentsAlignmentCantCantStation[] cantStationField;

    private string nameField;

    private decimal gaugeField;

    private string rotationPointField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("CantStation")]
    public LandXMLAlignmentsAlignmentCantCantStation[] CantStation
    {
        get
        {
            return this.cantStationField;
        }
        set
        {
            this.cantStationField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string name
    {
        get
        {
            return this.nameField;
        }
        set
        {
            this.nameField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal gauge
    {
        get
        {
            return this.gaugeField;
        }
        set
        {
            this.gaugeField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string rotationPoint
    {
        get
        {
            return this.rotationPointField;
        }
        set
        {
            this.rotationPointField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCantCantStation
{

    private decimal stationField;

    private decimal appliedCantField;

    private string curvatureField;

    private decimal equilibriumCantField;

    private bool equilibriumCantFieldSpecified;

    private decimal cantDeficiencyField;

    private bool cantDeficiencyFieldSpecified;

    private decimal cantGradientField;

    private bool cantGradientFieldSpecified;

    private decimal speedField;

    private bool speedFieldSpecified;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal station
    {
        get
        {
            return this.stationField;
        }
        set
        {
            this.stationField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal appliedCant
    {
        get
        {
            return this.appliedCantField;
        }
        set
        {
            this.appliedCantField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string curvature
    {
        get
        {
            return this.curvatureField;
        }
        set
        {
            this.curvatureField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal equilibriumCant
    {
        get
        {
            return this.equilibriumCantField;
        }
        set
        {
            this.equilibriumCantField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool equilibriumCantSpecified
    {
        get
        {
            return this.equilibriumCantFieldSpecified;
        }
        set
        {
            this.equilibriumCantFieldSpecified = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal cantDeficiency
    {
        get
        {
            return this.cantDeficiencyField;
        }
        set
        {
            this.cantDeficiencyField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool cantDeficiencySpecified
    {
        get
        {
            return this.cantDeficiencyFieldSpecified;
        }
        set
        {
            this.cantDeficiencyFieldSpecified = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal cantGradient
    {
        get
        {
            return this.cantGradientField;
        }
        set
        {
            this.cantGradientField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool cantGradientSpecified
    {
        get
        {
            return this.cantGradientFieldSpecified;
        }
        set
        {
            this.cantGradientFieldSpecified = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal speed
    {
        get
        {
            return this.speedField;
        }
        set
        {
            this.speedField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool speedSpecified
    {
        get
        {
            return this.speedFieldSpecified;
        }
        set
        {
            this.speedFieldSpecified = value;
        }
    }
}

