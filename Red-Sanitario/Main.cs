using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Mechanical;
using StructuralType = Autodesk.Revit.DB.Structure.StructuralType;

[TransactionAttribute(TransactionMode.Manual)]
[RegenerationAttribute(RegenerationOption.Manual)]
public class RedSanitario : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        //Get application and document objects
        UIApplication uiApp = commandData.Application;
        Document doc = uiApp.ActiveUIDocument.Document;

        Transaction trans = new Transaction(doc);
        trans.Start("Lab");

        FamilySymbol accesorioSymbol = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_PipeFitting)
            .Cast<FamilySymbol>()
            .Where<FamilySymbol>(e => e.Family.Name.Contains("Tee - Plain - PVC-C"))
            .FirstOrDefault();

        FamilyInstance mytee = new FilteredElementCollector(doc)
            .WherePasses(new FamilyInstanceFilter(doc, accesorioSymbol.Id))
            .Cast<FamilyInstance>()
            .FirstOrDefault();

        IList<Pipe> pvc = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Pipe)))
            .Cast<Pipe>()
            .ToList();
        
        ConnectorManager cmpvc1 = pvc[0].ConnectorManager;
        ConnectorManager cmpvc2 = pvc[1].ConnectorManager;
        ConnectorManager cmpvc3 = pvc[2].ConnectorManager;

        Connector pc11 = cmpvc1.Lookup(0);
        Connector pc12 = cmpvc1.Lookup(1);

        Connector pc21 = cmpvc2.Lookup(0);
        Connector pc22 = cmpvc2.Lookup(1);

        Connector pc31 = cmpvc3.Lookup(0);
        Connector pc32 = cmpvc3.Lookup(1);
        
        ConnectorManager cmtee = mytee.MEPModel.ConnectorManager;
        Connector tc1 = cmtee.Lookup(1);
        Connector tc2 = cmtee.Lookup(2);
        Connector tc3 = cmtee.Lookup(3);

        mytee.LookupParameter("Angle").Set(135.0 * Math.PI / 180.0);
        Parameter radius = mytee.LookupParameter("Nominal Radius");
        radius.Set(pvc[0].Diameter / 2.0);
        
        tc1.ConnectTo(pc22);
        tc2.ConnectTo(pc12);
        tc3.ConnectTo(pc31);

        trans.Commit();
        return Result.Succeeded;
    }
}

