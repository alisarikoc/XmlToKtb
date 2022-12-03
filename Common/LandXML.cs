
// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.

using System;
using System.Collections.Generic;
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
    private LandXMLCgPoints CgPointsField;
    private LandXMLSurfaces surfacesField;

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
    public LandXMLSurfaces Surfaces
    {
        get
        {
            return this.surfacesField;
        }
        set
        {
            this.surfacesField = value;
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

    public LandXMLCgPoints CgPoints
    {
        get
        {
            return this.CgPointsField;
        }
        set
        {
            this.CgPointsField = value;
        }
    }
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
    private LandXMLAlignmentsAlignmentCrossSect[] crossSectsField;
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
    [System.Xml.Serialization.XmlArrayItemAttribute("CrossSect", IsNullable = false)]
    public LandXMLAlignmentsAlignmentCrossSect[] CrossSects
    {
        get
        {
            return this.crossSectsField;
        }
        set
        {
            this.crossSectsField = value;
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
    private LandXMLAlignmentsAlignmentProfileProfSurf[] profSurfField;
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

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("ProfSurf")]
    public LandXMLAlignmentsAlignmentProfileProfSurf[] ProfSurf
    {
        get
        {
            return this.profSurfField;
        }
        set
        {
            this.profSurfField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentProfileProfSurf
{

    private string pntList2DField;

    private string nameField;

    private string stateField;

    /// <remarks/>
    public string PntList2D
    {
        get
        {
            return this.pntList2DField;
        }
        set
        {
            this.pntList2DField = value;
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
    public string state
    {
        get
        {
            return this.stateField;
        }
        set
        {
            this.stateField = value;
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

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCrossSect
{

    private LandXMLAlignmentsAlignmentCrossSectDesignCrossSectSurf[] designCrossSectSurfField;

    private decimal staField;

    private string nameField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("DesignCrossSectSurf")]
    public LandXMLAlignmentsAlignmentCrossSectDesignCrossSectSurf[] DesignCrossSectSurf
    {
        get
        {
            return this.designCrossSectSurfField;
        }
        set
        {
            this.designCrossSectSurfField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal sta
    {
        get
        {
            return this.staField;
        }
        set
        {
            this.staField = value;
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
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCrossSectDesignCrossSectSurf
{

    private LandXMLAlignmentsAlignmentCrossSectDesignCrossSectSurfCrossSectPnt[] crossSectPntField;

    private string nameField;

    private string sideField;

    private bool closedAreaField;

    private decimal areaField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("CrossSectPnt")]
    public LandXMLAlignmentsAlignmentCrossSectDesignCrossSectSurfCrossSectPnt[] CrossSectPnt
    {
        get
        {
            return this.crossSectPntField;
        }
        set
        {
            this.crossSectPntField = value;
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
    public string side
    {
        get
        {
            return this.sideField;
        }
        set
        {
            this.sideField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public bool closedArea
    {
        get
        {
            return this.closedAreaField;
        }
        set
        {
            this.closedAreaField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal area
    {
        get
        {
            return this.areaField;
        }
        set
        {
            this.areaField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCrossSectDesignCrossSectSurfCrossSectPnt
{

    private string codeField;

    private string valueField;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string code
    {
        get
        {
            return this.codeField;
        }
        set
        {
            this.codeField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value
    {
        get
        {
            return this.valueField;
        }
        set
        {
            this.valueField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLCgPoints
{

    private LandXMLCgPoint[] CgPointField;


    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("CgPoint")]
    public LandXMLCgPoint[] CgPoint
    {
        get
        {
            return this.CgPointField;
        }
        set
        {
            this.CgPointField = value;
        }
    }


}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true,
    Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLCgPoint
{
    private string nameField;

    private string codeField;

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
    public string code
    {
        get
        {
            return this.codeField;
        }
        set
        {
            this.codeField = value;
        }
    }

    /// <remarks/>
    [XmlText]
    public string value
    {
        get;
        set;
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLSurfaces
{
    private string nameField;

    /// <remarks/>
    public LandXMLSurfacesSurface Surface
    {
        get; set;
    }

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
public class LandXMLSurfacesSurface
{
    private LandXMLSurfacesSurfaceDefinition surfaceField;
    private decimal lengthField;
    private string descField;

    /// <remarks/>
    public LandXMLSurfacesSurfaceDefinition Definition
    {
        get
        {
            return surfaceField;
        }
        set
        {
            surfaceField = value;
        }
    }

    /// <remarks/>
    [XmlAttribute]
    public string name
    {
        get; set;
    }


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
public class LandXMLSurfacesSurfaceDefinition
{
    private string surfTypeField;

    private decimal area2DSurfField;

    private decimal area3DSurfField;

    private decimal elevMaxField;

    private decimal elevMinField;

    /// <remarks/>
    public LandXMLSurfacesSurfaceDefinitionPnts Pnts
    {
        get; set;
    }
    /// <remarks/>
    public LandXMLSurfacesSurfaceDefinitionFaces Faces
    {
        get; set;
    }
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string surfType
    {
        get
        {
            return surfTypeField;
        }
        set
        {
            surfTypeField = value;
        }
    }

}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLSurfacesSurfaceDefinitionPnts
{
    private string nameField;

    /// <remarks/>
    [XmlElement("P", typeof(LandXMLSurfacesSurfaceDefinitionPntsP))]
    public object[] Items
    {
        get; set;
    }

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
public class LandXMLSurfacesSurfaceDefinitionPntsP
{

    /// <remarks/>
    [XmlAttribute]
    public string id
    {
        get; set;
    }

    /// <remarks/>
    [XmlText]
    public string Value
    {
        get; set;
    }
}
/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LandXMLSurfacesSurfaceDefinitionFaces
{
    private string nameField;

    /// <remarks/>
    [XmlElement("F", typeof(LandXMLSurfacesSurfaceDefinitionFacesF))]
    public object[] Items
    {
        get; set;
    }

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
public class LandXMLSurfacesSurfaceDefinitionFacesF
{

    /// <remarks/>
    [XmlAttribute]
    public string id
    {
        get; set;
    }

    /// <remarks/>
    [XmlText]
    public string Value
    {
        get; set;
    }
}