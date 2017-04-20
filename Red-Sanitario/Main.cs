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
    void Connect3Pipes(Document doc, XYZ offset, Pipe p1, Pipe p2, Pipe p3)
    {
        FamilySymbol accesorioSymbol = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_PipeFitting)
            .Cast<FamilySymbol>()
            .Where<FamilySymbol>(e => e.Family.Name.Contains("Tee - Plain - PVC-C"))
            .FirstOrDefault();

        FamilyInstance tee = doc.Create.NewFamilyInstance(offset, accesorioSymbol, StructuralType.NonStructural);
        Line axis = Line.CreateBound(offset, offset + offset.CrossProduct(XYZ.BasisY));
        ElementTransformUtils.RotateElement(doc, tee.Id, axis, Math.PI / 2.0);

        ConnectorManager cmpvc1 = p1.ConnectorManager;
        ConnectorManager cmpvc2 = p2.ConnectorManager;
        ConnectorManager cmpvc3 = p3.ConnectorManager;

        Connector pc11 = cmpvc1.Lookup(0);
        Connector pc12 = cmpvc1.Lookup(1);

        Connector pc21 = cmpvc2.Lookup(0);
        Connector pc22 = cmpvc2.Lookup(1);

        Connector pc31 = cmpvc3.Lookup(0);
        Connector pc32 = cmpvc3.Lookup(1);

        ConnectorManager cmtee = tee.MEPModel.ConnectorManager;
        Connector tc1 = cmtee.Lookup(1);
        Connector tc2 = cmtee.Lookup(2);
        Connector tc3 = cmtee.Lookup(3);
        
        tee.LookupParameter("Angle").Set(135.0 * Math.PI / 180.0);
        Parameter radius = tee.LookupParameter("Nominal Radius");
        radius.Set(p1.Diameter / 2.0);

        tc1.ConnectTo(pc12);
        tc2.ConnectTo(pc21);
        tc3.ConnectTo(pc32);
    }
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        //Get application and document objects
        UIApplication uiApp = commandData.Application;
        Document doc = uiApp.ActiveUIDocument.Document;

        Transaction trans = new Transaction(doc);
        trans.Start("Lab");

        Family sifonFamily = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Family)))
            .Cast<Family>()
            .Where(e => e.Name.Equals("sifon"))
            .FirstOrDefault();

        FamilySymbol sifonSymbol = new FilteredElementCollector(doc)
            .WherePasses(new FamilySymbolFilter(sifonFamily.Id))
            .Cast<FamilySymbol>()
            .Where(e => e.Name.Equals("sifon 3x2"))
            .FirstOrDefault();

        CurveElement guia = new FilteredElementCollector(doc)
            .WherePasses(new CurveElementFilter(CurveElementType.DetailCurve))
            .Cast<CurveElement>()
            .Where(e => e.GeometryCurve.GetType() == typeof(Line))
            .FirstOrDefault();

        TextElement bajante = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(TextElement)))
            .Cast<TextElement>()
            .Where(e => e.Text.Equals("bajante"))
            .FirstOrDefault();

        IList<FamilyInstance> sifones = new FilteredElementCollector(doc)
            .WherePasses(new FamilyInstanceFilter(doc, sifonSymbol.Id))
            .Cast<FamilyInstance>().ToList();

        Element pvc = new FilteredElementCollector(doc)
            .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves))
            .Where(e => e.Name.Equals("f pvc sanitaria 4"))
            .FirstOrDefault();

        Element systemTypes = new FilteredElementCollector(doc)
          .WherePasses(new ElementClassFilter(typeof(PipingSystemType)))
          .Where(e => e.Name.Equals("Hydronic Return"))
          .FirstOrDefault();

        XYZ s0 = guia.GeometryCurve.GetEndPoint(0);
        XYZ s1 = guia.GeometryCurve.GetEndPoint(1);
        double m = (s0.Y - s1.Y) / (s0.X - s1.X);
        double b = s1.Y - (m * s1.X);


        foreach (FamilyInstance sifon in sifones)
        {
            XYZ p = ((LocationPoint)sifon.Location).Point;
            XYZ p0 = new XYZ(p.X, p.Y, s0.Z);

            XYZ v = s1 - s0;
            XYZ z = v.CrossProduct(p0 - s0).Normalize();
            XYZ w = z.CrossProduct(v).Normalize();
            double d = w.DotProduct(p0 - s0);
            XYZ a = p0 - (d * w);
            XYZ offset = (Math.Tan(Math.PI * 0.25) * d * (s1 - a).Normalize()) + a;

            Pipe sup = Pipe.Create(doc, systemTypes.Id, pvc.Id, sifones[0].LevelId, s0, offset);
            Pipe rama = Pipe.Create(doc, systemTypes.Id, pvc.Id, sifon.LevelId, p0, offset);
            Pipe inf = Pipe.Create(doc, systemTypes.Id, pvc.Id, sifones[0].LevelId, offset, s1);

            Connect3Pipes(doc, offset, sup, inf, rama);
            break;
        }

        /*
        // Esto de aqui abajo duplica las vistas y las pone como "Red Sanitaria"
        IList<View> floorPlans = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(View)))
            .Cast<View>()
            .Where(e => e.ViewType.Equals(ViewType.FloorPlan) && e.Title.Contains("Floor Plan") && (!e.Name.Equals("Site")))
            .ToList();
        foreach (View floorPlan in floorPlans)
        {
            View sanitarioPlan = (View)doc.GetElement(floorPlan.Duplicate(ViewDuplicateOption.WithDetailing));
            sanitarioPlan.Name = floorPlan.Name + " Red Sanitaria";
            sanitarioPlan.Discipline = ViewDiscipline.Plumbing;
        }
        */
        
        trans.Commit();
        return Result.Succeeded;
    }
}

