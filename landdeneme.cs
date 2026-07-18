
// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.landxml.org/schema/LandXML-1.2", IsNullable = false)]
public partial class LandXML
{

    private LandXMLUnits unitsField;

    private LandXMLProject projectField;

    private LandXMLApplication applicationField;

    private LandXMLAlignments alignmentsField;

    private System.DateTime dateField;

    private System.DateTime timeField;

    private decimal versionField;

    private string languageField;

    private bool readOnlyField;

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
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
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
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLUnitsMetric
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

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string diameterUnit
    {
        get
        {
            return this.diameterUnitField;
        }
        set
        {
            this.diameterUnitField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string angularUnit
    {
        get
        {
            return this.angularUnitField;
        }
        set
        {
            this.angularUnitField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string directionUnit
    {
        get
        {
            return this.directionUnitField;
        }
        set
        {
            this.directionUnitField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
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
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLApplication
{

    private string nameField;

    private string descField;

    private string manufacturerField;

    private ushort versionField;

    private string manufacturerURLField;

    private System.DateTime timeStampField;

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
    public ushort version
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

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public System.DateTime timeStamp
    {
        get
        {
            return this.timeStampField;
        }
        set
        {
            this.timeStampField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignments
{

    private LandXMLAlignmentsAlignment alignmentField;

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
public partial class LandXMLAlignmentsAlignment
{

    private LandXMLAlignmentsAlignmentCoordGeom coordGeomField;

    private LandXMLAlignmentsAlignmentStaEquation[] staEquationField;

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
            return this.coordGeomField;
        }
        set
        {
            this.coordGeomField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("StaEquation")]
    public LandXMLAlignmentsAlignmentStaEquation[] StaEquation
    {
        get
        {
            return this.staEquationField;
        }
        set
        {
            this.staEquationField = value;
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
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
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
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCoordGeomCurve
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
    [System.Xml.Serialization.XmlAttributeAttribute()]
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
    [System.Xml.Serialization.XmlAttributeAttribute()]
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
    [System.Xml.Serialization.XmlAttributeAttribute()]
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
    [System.Xml.Serialization.XmlAttributeAttribute()]
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
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCoordGeomLine
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
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentCoordGeomSpiral
{

    private string startField;

    private string piField;

    private string endField;

    private decimal lengthField;

    private double radiusEndField;

    private double radiusStartField;

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
    [System.Xml.Serialization.XmlAttributeAttribute()]
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
    [System.Xml.Serialization.XmlAttributeAttribute()]
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
    [System.Xml.Serialization.XmlAttributeAttribute()]
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
    [System.Xml.Serialization.XmlAttributeAttribute()]
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
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentStaEquation
{

    private decimal staAheadField;

    private decimal staBackField;

    private decimal staInternalField;

    private string staIncrementField;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal staAhead
    {
        get
        {
            return this.staAheadField;
        }
        set
        {
            this.staAheadField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal staBack
    {
        get
        {
            return this.staBackField;
        }
        set
        {
            this.staBackField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal staInternal
    {
        get
        {
            return this.staInternalField;
        }
        set
        {
            this.staInternalField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string staIncrement
    {
        get
        {
            return this.staIncrementField;
        }
        set
        {
            this.staIncrementField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.landxml.org/schema/LandXML-1.2")]
public partial class LandXMLAlignmentsAlignmentProfile
{

    private LandXMLAlignmentsAlignmentProfileProfAlign profAlignField;

    private string nameField;

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
public partial class LandXMLAlignmentsAlignmentProfileProfAlign
{

    private object[] itemsField;

    private string nameField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("CircCurve", typeof(LandXMLAlignmentsAlignmentProfileProfAlignCircCurve))]
    [System.Xml.Serialization.XmlElementAttribute("PVI", typeof(string))]
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
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
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


