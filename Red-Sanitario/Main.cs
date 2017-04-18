using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB.Plumbing;

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

        /*
        if (columnFamily == null)
        {
            doc.LoadFamily("C:\\GitHub\\WallsSetup\\M_Concrete-Rectangular-Column.rfa", out columnFamily);
        }
        */
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

        Pipe.Create(doc, systemTypes.Id, pvc.Id, sifones[0].LevelId, s0, s1);

        foreach (FamilyInstance sifon in sifones) {
            XYZ p = ((LocationPoint)sifon.Location).Point;
            XYZ p0 = new XYZ(p.X, p.Y, s0.Z);

            XYZ v = s1 - s0;
            XYZ z = v.CrossProduct(p0 - s0).Normalize();
            XYZ w = z.CrossProduct(v).Normalize();
            double d = w.DotProduct(p0 - s0);
            XYZ a = p0 - (d * w);
            XYZ offset = (Math.Tan(Math.PI * 0.25) * d * (s0 - a).Normalize()) + a;

            Pipe.Create(doc, systemTypes.Id, pvc.Id, sifon.LevelId, p0, offset);
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

