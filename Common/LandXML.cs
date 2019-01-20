
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

    private LandXMLUnits unitsField;

    private LandXMLProject projectField;

    private LandXMLApplication applicationField;

    private LandXMLAlignments alignmentsField;

    private DateTime dateField;

    private DateTime timeField;

    private string versionField;

    private string languageField;

    private bool readOnlyField;

    /// <remarks/>
    public LandXMLUnits Units
    {
        get
        {
            return unitsField;
        }
        set
        {
            unitsField = value;
        }
    }

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
    public LandXMLApplication Application
    {
        get
        {
            return applicationField;
        }
        set
        {
            applicationField = value;
        }
    }

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
    public DateTime date
    {
        get
        {
            return dateField;
        }
        set
        {
            dateField = value;
        }
    }

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
    public bool readOnly
    {
        get
        {
            return readOnlyField;
        }
        set
        {
            readOnlyField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLUnits
{

    private LandXMLUnitsMetric metricField;

    /// <remarks/>
    public LandXMLUnitsMetric Metric
    {
        get
        {
            return metricField;
        }
        set
        {
            metricField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLUnitsMetric
{

    private string areaUnitField;

    private string linearUnitField;

    private string volumeUnitField;

    private string temperatureUnitField;

    private string pressureUnitField;

    private string diameterUnitField;

    private string angularUnitField;

    private string directionUnitField;

    /// <remarks/>
    [XmlAttribute]
    public string areaUnit
    {
        get
        {
            return areaUnitField;
        }
        set
        {
            areaUnitField = value;
        }
    }

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
    public string volumeUnit
    {
        get
        {
            return volumeUnitField;
        }
        set
        {
            volumeUnitField = value;
        }
    }

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
    public string pressureUnit
    {
        get
        {
            return pressureUnitField;
        }
        set
        {
            pressureUnitField = value;
        }
    }

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
    public string angularUnit
    {
        get
        {
            return angularUnitField;
        }
        set
        {
            angularUnitField = value;
        }
    }

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
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLProject
{

    private string nameField;

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
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLApplication
{

    private string nameField;

    private string descField;

    private string manufacturerField;

    private string versionField;

    private string manufacturerURLField;

    private DateTime timeStampField;

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
    public string manufacturer
    {
        get
        {
            return manufacturerField;
        }
        set
        {
            manufacturerField = value;
        }
    }

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
    public string manufacturerURL
    {
        get
        {
            return manufacturerURLField;
        }
        set
        {
            manufacturerURLField = value;
        }
    }

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
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignments
{

    private LandXMLAlignmentsAlignment alignmentField;

    private string nameField;

    /// <remarks/>
    public LandXMLAlignmentsAlignment Alignment
    {
        get
        {
            return alignmentField;
        }
        set
        {
            alignmentField = value;
        }
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
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignmentsAlignment
{

    private LandXMLAlignmentsAlignmentCoordGeom coordGeomField;

    private LandXMLAlignmentsAlignmentProfile profileField;

    private string nameField;

    private decimal lengthField;

    private decimal staStartField;

    private string descField;

    /// <remarks/>
    public LandXMLAlignmentsAlignmentCoordGeom CoordGeom
    {
        get
        {
            return coordGeomField;
        }
        set
        {
            coordGeomField = value;
        }
    }

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
    public decimal staStart
    {
        get
        {
            return staStartField;
        }
        set
        {
            staStartField = value;
        }
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
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignmentsAlignmentCoordGeom
{

    private object[] itemsField;

    /// <remarks/>
    [XmlElement("Curve", typeof(LandXMLAlignmentsAlignmentCoordGeomCurve))]
    [XmlElement("Line", typeof(LandXMLAlignmentsAlignmentCoordGeomLine))]
    [XmlElement("Spiral", typeof(LandXMLAlignmentsAlignmentCoordGeomSpiral))]
    public object[] Items
    {
        get
        {
            return itemsField;
        }
        set
        {
            itemsField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignmentsAlignmentCoordGeomCurve
{

    private string startField;

    private string centerField;

    private string endField;

    private string piField;

    private string rotField;

    private decimal chordField;

    private string crvTypeField;

    private decimal deltaField;

    private decimal dirEndField;

    private decimal dirStartField;

    private decimal externalField;

    private decimal lengthField;

    private decimal midOrdField;

    private decimal radiusField;

    private decimal tangentField;

    /// <remarks/>
    public string Start
    {
        get
        {
            return startField;
        }
        set
        {
            startField = value;
        }
    }

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
    public string rot
    {
        get
        {
            return rotField;
        }
        set
        {
            rotField = value;
        }
    }

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
    public string crvType
    {
        get
        {
            return crvTypeField;
        }
        set
        {
            crvTypeField = value;
        }
    }

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
    public decimal dirEnd
    {
        get
        {
            return dirEndField;
        }
        set
        {
            dirEndField = value;
        }
    }

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
    public decimal external
    {
        get
        {
            return externalField;
        }
        set
        {
            externalField = value;
        }
    }

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
    public decimal midOrd
    {
        get
        {
            return midOrdField;
        }
        set
        {
            midOrdField = value;
        }
    }

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
    public decimal tangent
    {
        get
        {
            return tangentField;
        }
        set
        {
            tangentField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignmentsAlignmentCoordGeomLine
{

    private string startField;

    private string endField;

    private decimal dirField;

    private decimal lengthField;

    /// <remarks/>
    public string Start
    {
        get
        {
            return startField;
        }
        set
        {
            startField = value;
        }
    }

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
    public decimal dir
    {
        get
        {
            return dirField;
        }
        set
        {
            dirField = value;
        }
    }

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
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignmentsAlignmentCoordGeomSpiral
{

    private string startField;

    private string piField;

    private string endField;

    private decimal lengthField;

    private string radiusEndField;

    private string radiusStartField;

    private string rotField;

    private string spiTypeField;

    private decimal thetaField;

    private decimal totalYField;

    private decimal totalXField;

    private decimal tanLongField;

    private decimal tanShortField;

    /// <remarks/>
    public string Start
    {
        get
        {
            return startField;
        }
        set
        {
            startField = value;
        }
    }

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
    public string radiusEnd
    {
        get
        {
            return radiusEndField;
        }
        set
        {
            radiusEndField = value;
        }
    }

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
    public string rot
    {
        get
        {
            return rotField;
        }
        set
        {
            rotField = value;
        }
    }

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
    public decimal theta
    {
        get
        {
            return thetaField;
        }
        set
        {
            thetaField = value;
        }
    }

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
    public decimal totalX
    {
        get
        {
            return totalXField;
        }
        set
        {
            totalXField = value;
        }
    }

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
    public decimal tanShort
    {
        get
        {
            return tanShortField;
        }
        set
        {
            tanShortField = value;
        }
    }
}

/// <remarks/>
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignmentsAlignmentProfile
{

    private LandXMLAlignmentsAlignmentProfileProfAlign profAlignField;

    private string nameField;

    /// <remarks/>
    public LandXMLAlignmentsAlignmentProfileProfAlign ProfAlign
    {
        get
        {
            return profAlignField;
        }
        set
        {
            profAlignField = value;
        }
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
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignmentsAlignmentProfileProfAlign
{

    private object[] itemsField;

    private string nameField;

    /// <remarks/>
    [XmlElement("CircCurve", typeof(LandXMLAlignmentsAlignmentProfileProfAlignCircCurve))]
    [XmlElement("PVI", typeof(string))]
    [XmlElement("ParaCurve", typeof(LandXMLAlignmentsAlignmentProfileProfAlignParaCurve))]
    public object[] Items
    {
        get
        {
            return itemsField;
        }
        set
        {
            itemsField = value;
        }
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
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignmentsAlignmentProfileProfAlignCircCurve
{

    private decimal lengthField;

    private decimal radiusField;

    private string valueField;

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
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignmentsAlignmentProfileProfAlignParaCurve
{

    private decimal lengthField;

    private string valueField;

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
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public class LandXMLAlignmentsAlignmentProfileProfAlignPVI
{

    private decimal lengthField;

    private string valueField;

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