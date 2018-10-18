
// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.landxml.org/schema/LandXML-1.2", IsNullable = false)]
public partial class LandXML
{

    private LandXMLApplication applicationField;

    private LandXMLProject projectField;

    private LandXMLUnits unitsField;

    private LandXMLAlignments alignmentsField;

    private decimal versionField;

    private System.DateTime dateField;

    private System.DateTime timeField;

    private bool readOnlyField;

    private string languageField;

    /// <remarks/>
    public LandXMLApplication Application
    {
        get
        {
            return this.applicationField;
        }
        set
        {
            this.applicationField = value;
        }
    }

    /// <remarks/>
    public LandXMLProject Project
    {
        get
        {
            return this.projectField;
        }
        set
        {
            this.projectField = value;
        }
    }

    /// <remarks/>
    public LandXMLUnits Units
    {
        get
        {
            return this.unitsField;
        }
        set
        {
            this.unitsField = value;
        }
    }

    /// <remarks/>
    public LandXMLAlignments Alignments
    {
        get
        {
            return this.alignmentsField;
        }
        set
        {
            this.alignmentsField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal version
    {
        get
        {
            return this.versionField;
        }
        set
        {
            this.versionField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
    public System.DateTime date
    {
        get
        {
            return this.dateField;
        }
        set
        {
            this.dateField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "time")]
    public System.DateTime time
    {
        get
        {
            return this.timeField;
        }
        set
        {
            this.timeField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public bool readOnly
    {
        get
        {
            return this.readOnlyField;
        }
        set
        {
            this.readOnlyField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string language
    {
        get
        {
            return this.languageField;
        }
        set
        {
            this.languageField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLApplication
{

    private LandXMLApplicationAuthor authorField;

    private string nameField;

    private string versionField;

    private string manufacturerField;

    private string manufacturerURLField;

    /// <remarks/>
    public LandXMLApplicationAuthor Author
    {
        get
        {
            return this.authorField;
        }
        set
        {
            this.authorField = value;
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
    public string version
    {
        get
        {
            return this.versionField;
        }
        set
        {
            this.versionField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string manufacturer
    {
        get
        {
            return this.manufacturerField;
        }
        set
        {
            this.manufacturerField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string manufacturerURL
    {
        get
        {
            return this.manufacturerURLField;
        }
        set
        {
            this.manufacturerURLField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLApplicationAuthor
{

    private string createdByField;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string createdBy
    {
        get
        {
            return this.createdByField;
        }
        set
        {
            this.createdByField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLProject
{

    private string nameField;

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
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLUnits
{

    private LandXMLUnitsMetric metricField;

    /// <remarks/>
    public LandXMLUnitsMetric Metric
    {
        get
        {
            return this.metricField;
        }
        set
        {
            this.metricField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLUnitsMetric
{

    private string linearUnitField;

    private string areaUnitField;

    private string volumeUnitField;

    private string temperatureUnitField;

    private string pressureUnitField;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string linearUnit
    {
        get
        {
            return this.linearUnitField;
        }
        set
        {
            this.linearUnitField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string areaUnit
    {
        get
        {
            return this.areaUnitField;
        }
        set
        {
            this.areaUnitField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string volumeUnit
    {
        get
        {
            return this.volumeUnitField;
        }
        set
        {
            this.volumeUnitField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string temperatureUnit
    {
        get
        {
            return this.temperatureUnitField;
        }
        set
        {
            this.temperatureUnitField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string pressureUnit
    {
        get
        {
            return this.pressureUnitField;
        }
        set
        {
            this.pressureUnitField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignments
{

    private LandXMLAlignmentsAlignment alignmentField;

    private LandXMLAlignmentsFeature featureField;

    private string nameField;

    /// <remarks/>
    public LandXMLAlignmentsAlignment Alignment
    {
        get
        {
            return this.alignmentField;
        }
        set
        {
            this.alignmentField = value;
        }
    }

    /// <remarks/>
    public LandXMLAlignmentsFeature Feature
    {
        get
        {
            return this.featureField;
        }
        set
        {
            this.featureField = value;
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
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignment
{

    private LandXMLAlignmentsAlignmentCoordGeom coordGeomField;

    private LandXMLAlignmentsAlignmentProfile profileField;

    private string nameField;

    private decimal lengthField;

    private decimal staStartField;

    /// <remarks/>
    public LandXMLAlignmentsAlignmentCoordGeom CoordGeom
    {
        get
        {
            return this.coordGeomField;
        }
        set
        {
            this.coordGeomField = value;
        }
    }

    /// <remarks/>
    public LandXMLAlignmentsAlignmentProfile Profile
    {
        get
        {
            return this.profileField;
        }
        set
        {
            this.profileField = value;
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
    public decimal length
    {
        get
        {
            return this.lengthField;
        }
        set
        {
            this.lengthField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal staStart
    {
        get
        {
            return this.staStartField;
        }
        set
        {
            this.staStartField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCoordGeom
{

    private object[] itemsField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("Curve", typeof(LandXMLAlignmentsAlignmentCoordGeomCurve))]
    [System.Xml.Serialization.XmlElementAttribute("Line", typeof(LandXMLAlignmentsAlignmentCoordGeomLine))]
    [System.Xml.Serialization.XmlElementAttribute("Spiral", typeof(LandXMLAlignmentsAlignmentCoordGeomSpiral))]
    public object[] Items
    {
        get
        {
            return this.itemsField;
        }
        set
        {
            this.itemsField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCoordGeomCurve
{

    private string startField;

    private string centerField;

    private string endField;

    private decimal staStartField;

    private string rotField;

    private string crvTypeField;

    private decimal radiusField;

    private decimal chordField;

    private decimal deltaField;

    private decimal dirEndField;

    private decimal dirStartField;

    private decimal externalField;

    private decimal lengthField;

    private decimal midOrdField;

    private decimal tangentField;

    private string PIField;

    /// <remarks/>
    public string PI
    {
        get
        {
            return this.PIField;
        }
        set
        {
            this.PIField = value;
        }
    }
    /// <remarks/>
    public decimal tangent
    {
        get
        {
            return this.tangentField;
        }
        set
        {
            this.tangentField = value;
        }
    }
    /// <remarks/>
    public decimal midOrd
    {
        get
        {
            return this.midOrdField;
        }
        set
        {
            this.midOrdField = value;
        }
    }
    /// <remarks/>
    public decimal length
    {
        get
        {
            return this.lengthField;
        }
        set
        {
            this.lengthField = value;
        }
    }
    /// <remarks/>
    public decimal external
    {
        get
        {
            return this.externalField;
        }
        set
        {
            this.externalField = value;
        }
    }
    /// <remarks/>
    public decimal dirStart
    {
        get
        {
            return this.dirStartField;
        }
        set
        {
            this.dirStartField = value;
        }
    }
    /// <remarks/>
    public decimal dirEnd
    {
        get
        {
            return this.dirEndField;
        }
        set
        {
            this.dirEndField = value;
        }
    }
    /// <remarks/>
    public decimal delta
    {
        get
        {
            return this.deltaField;
        }
        set
        {
            this.deltaField = value;
        }
    }


    /// <remarks/>
    public decimal chord
    {
        get
        {
            return this.chordField;
        }
        set
        {
            this.chordField = value;
        }
    }

    /// <remarks/>
    public string Start
    {
        get
        {
            return this.startField;
        }
        set
        {
            this.startField = value;
        }
    }

    /// <remarks/>
    public string Center
    {
        get
        {
            return this.centerField;
        }
        set
        {
            this.centerField = value;
        }
    }

    /// <remarks/>
    public string End
    {
        get
        {
            return this.endField;
        }
        set
        {
            this.endField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal staStart
    {
        get
        {
            return this.staStartField;
        }
        set
        {
            this.staStartField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string rot
    {
        get
        {
            return this.rotField;
        }
        set
        {
            this.rotField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string crvType
    {
        get
        {
            return this.crvTypeField;
        }
        set
        {
            this.crvTypeField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal radius
    {
        get
        {
            return this.radiusField;
        }
        set
        {
            this.radiusField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCoordGeomLine
{

    private string startField;

    private string endField;

    private decimal staStartField;

    private decimal dirField;

    private decimal LengthField;

    /// <remarks/>
    public decimal dir
    {
        get
        {
            return this.dirField;
        }
        set
        {
            this.dirField = value;
        }
    }
    /// <remarks/>
    public decimal Length
    {
        get
        {
            return this.LengthField;
        }
        set
        {
            this.LengthField = value;
        }
    }
    /// <remarks/>
    public string Start
    {
        get
        {
            return this.startField;
        }
        set
        {
            this.startField = value;
        }
    }

    /// <remarks/>
    public string End
    {
        get
        {
            return this.endField;
        }
        set
        {
            this.endField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal staStart
    {
        get
        {
            return this.staStartField;
        }
        set
        {
            this.staStartField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCoordGeomSpiral
{

    private string startField;

    private string piField;

    private string endField;

    private decimal staStartField;

    private string descField;

    private decimal lengthField;

    private double radiusStartField;

    private double radiusEndField;

    private string rotField;

    private string spiTypeField;

    private decimal constantField;

    private bool constantFieldSpecified;

    private decimal thetaField;

    private decimal totalYField;

    private decimal totalXField;

    private decimal tanLongField;

    private decimal tanShortField;

    /// <remarks/>
    public decimal tanLong
    {
        get
        {
            return this.tanLongField;
        }
        set
        {
            this.tanLongField = value;
        }
    }
    /// <remarks/>
    public decimal tanShort
    {
        get
        {
            return this.tanShortField;
        }
        set
        {
            this.tanShortField = value;
        }
    }
    /// <remarks/>
    public decimal totalY
    {
        get
        {
            return this.totalYField;
        }
        set
        {
            this.totalYField = value;
        }
    }
    /// <remarks/>
    public decimal totalX
    {
        get
        {
            return this.totalXField;
        }
        set
        {
            this.totalXField = value;
        }
    }
    /// <remarks/>
    public decimal theta
    {
        get
        {
            return this.thetaField;
        }
        set
        {
            this.thetaField = value;
        }
    }
    /// <remarks/>
    public string Start
    {
        get
        {
            return this.startField;
        }
        set
        {
            this.startField = value;
        }
    }

    /// <remarks/>
    public string PI
    {
        get
        {
            return this.piField;
        }
        set
        {
            this.piField = value;
        }
    }

    /// <remarks/>
    public string End
    {
        get
        {
            return this.endField;
        }
        set
        {
            this.endField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal staStart
    {
        get
        {
            return this.staStartField;
        }
        set
        {
            this.staStartField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string desc
    {
        get
        {
            return this.descField;
        }
        set
        {
            this.descField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal length
    {
        get
        {
            return this.lengthField;
        }
        set
        {
            this.lengthField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double radiusStart
    {
        get
        {
            return this.radiusStartField;
        }
        set
        {
            this.radiusStartField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double radiusEnd
    {
        get
        {
            return this.radiusEndField;
        }
        set
        {
            this.radiusEndField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string rot
    {
        get
        {
            return this.rotField;
        }
        set
        {
            this.rotField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string spiType
    {
        get
        {
            return this.spiTypeField;
        }
        set
        {
            this.spiTypeField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal constant
    {
        get
        {
            return this.constantField;
        }
        set
        {
            this.constantField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool constantSpecified
    {
        get
        {
            return this.constantFieldSpecified;
        }
        set
        {
            this.constantFieldSpecified = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentProfile
{

    private LandXMLAlignmentsAlignmentProfileProfAlign profAlignField;

    /// <remarks/>
    public LandXMLAlignmentsAlignmentProfileProfAlign ProfAlign
    {
        get
        {
            return this.profAlignField;
        }
        set
        {
            this.profAlignField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentProfileProfAlign
{

    private object[] itemsField;

    private string nameField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("CircCurve", typeof(LandXMLAlignmentsAlignmentProfileProfAlignCircCurve))]
    [System.Xml.Serialization.XmlElementAttribute("PVI", typeof(string))]
    [System.Xml.Serialization.XmlElementAttribute("ParaCurve", typeof(LandXMLAlignmentsAlignmentProfileProfAlignParaCurve))]
    public object[] Items
    {
        get
        {
            return this.itemsField;
        }
        set
        {
            this.itemsField = value;
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
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentProfileProfAlignCircCurve
{

    private decimal lengthField;

    private decimal radiusField;

    private string valueField;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal length
    {
        get
        {
            return this.lengthField;
        }
        set
        {
            this.lengthField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal radius
    {
        get
        {
            return this.radiusField;
        }
        set
        {
            this.radiusField = value;
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
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentProfileProfAlignParaCurve
{

    private decimal lengthField;

    private decimal descField;

    private string valueField;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal length
    {
        get
        {
            return this.lengthField;
        }
        set
        {
            this.lengthField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal desc
    {
        get
        {
            return this.descField;
        }
        set
        {
            this.descField = value;
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
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsFeature
{

    private LandXMLAlignmentsFeatureFeature featureField;

    private string codeField;

    /// <remarks/>
    public LandXMLAlignmentsFeatureFeature Feature
    {
        get
        {
            return this.featureField;
        }
        set
        {
            this.featureField = value;
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
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsFeatureFeature
{

    private LandXMLAlignmentsFeatureFeatureProperty[] propertyField;

    private string[] textField;

    private string codeField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("Property")]
    public LandXMLAlignmentsFeatureFeatureProperty[] Property
    {
        get
        {
            return this.propertyField;
        }
        set
        {
            this.propertyField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTextAttribute()]
    public string[] Text
    {
        get
        {
            return this.textField;
        }
        set
        {
            this.textField = value;
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
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsFeatureFeatureProperty
{

    private string labelField;

    private string valueField;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string label
    {
        get
        {
            return this.labelField;
        }
        set
        {
            this.labelField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string value
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

