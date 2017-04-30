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
    public class Tuberia
    {
        public XYZ start;
        public XYZ end;
        public Level level;
        public Pipe tubo;
        public Tuberia(XYZ start, XYZ end, Level level)
        {
            this.start = start;
            this.end = end;
            this.level = level;
        }
    }
    public class UnionXYZ
    {
        public Tuberia s1;
        public Tuberia s2;
        public Tuberia s3;
        public XYZ offset;
    }
    void Connect2Tubos(Document doc, Pipe p1, Pipe p2)
    {
        double epsilon = 0.001;
        ConnectorManager cmpvc1 = p1.ConnectorManager;
        ConnectorManager cmpvc2 = p2.ConnectorManager;
        
        XYZ p1start = cmpvc1.Lookup(0).Origin;
        XYZ p1end = cmpvc1.Lookup(1).Origin;

        XYZ p2start = cmpvc2.Lookup(0).Origin;
        XYZ p2end = cmpvc2.Lookup(1).Origin;

        Connector s11 = null, s12 = null, s21 = null, s22 = null;
        
        if (p1start.DistanceTo(p2start) < epsilon)
        {
            s11 = cmpvc1.Lookup(1);
            s12 = cmpvc1.Lookup(0);
            s21 = cmpvc2.Lookup(0);
            s22 = cmpvc2.Lookup(1);
        }
        else if (p1end.DistanceTo(p2start) < epsilon)
        {
            s11 = cmpvc1.Lookup(0);
            s12 = cmpvc1.Lookup(1);
            s21 = cmpvc2.Lookup(0);
            s22 = cmpvc2.Lookup(1);
        }
        else
        {

        }
        FamilyInstance tee = doc.Create.NewElbowFitting(s12, s21);
    }
    void Connect3Tubos(Document doc, Pipe p1, Pipe p2, Pipe p3)
    {
        double epsilon = 0.001;
        ConnectorManager cmpvc1 = p1.ConnectorManager;
        ConnectorManager cmpvc2 = p2.ConnectorManager;
        ConnectorManager cmpvc3 = p3.ConnectorManager;

        XYZ p1start = cmpvc1.Lookup(0).Origin;
        XYZ p1end = cmpvc1.Lookup(1).Origin;

        XYZ p2start = cmpvc2.Lookup(0).Origin;
        XYZ p2end = cmpvc2.Lookup(1).Origin;

        XYZ p3start = cmpvc3.Lookup(0).Origin;
        XYZ p3end = cmpvc3.Lookup(1).Origin;

        Connector s11 = null, s12 = null, s21 = null, s22 = null, s31 = null, s32 = null;
        XYZ old = null;

        if (p3start.DistanceTo(p2start) < epsilon && p3start.DistanceTo(p1end) < epsilon)
        {
            s11 = cmpvc1.Lookup(0); s12 = cmpvc1.Lookup(1);
            s21 = cmpvc2.Lookup(0); s22 = cmpvc2.Lookup(1);
            s31 = cmpvc3.Lookup(0); s32 = cmpvc3.Lookup(1);
        }
        else if (p3end.DistanceTo(p2start) < epsilon && p3end.DistanceTo(p1end) < epsilon)
        {
            s11 = cmpvc1.Lookup(0); s12 = cmpvc1.Lookup(1);
            s21 = cmpvc2.Lookup(0); s22 = cmpvc2.Lookup(1);
            s31 = cmpvc3.Lookup(1); s32 = cmpvc3.Lookup(0);
        }
        else
        {
            old = null;
        }
        old = new XYZ(s32.Origin.X, s32.Origin.Y, s32.Origin.Z);
        XYZ k1 = s12.Origin - s11.Origin;
        XYZ k2 = s32.Origin - s31.Origin;
        XYZ v = k1.CrossProduct(k2).Normalize();
        XYZ vn = v.CrossProduct(k1).Normalize();

        Line rama = Line.CreateBound(s32.Origin, s31.Origin);
        s32.Origin = s31.Origin + vn.DotProduct(k2) * vn;
        FamilyInstance tee = doc.Create.NewTeeFitting(s12, s21, s31);
        Connector teecm3 = tee.MEPModel.ConnectorManager.Lookup(3);

        XYZ v1 = tee.MEPModel.ConnectorManager.Lookup(1).Origin;
        XYZ v2 = tee.MEPModel.ConnectorManager.Lookup(2).Origin;
        Line guia = Line.CreateBound(v2, v1);
        double angle = guia.Direction.AngleTo(rama.Direction);
        
        teecm3.DisconnectFrom(s31);
        s32.Origin = new XYZ(old.X, old.Y, old.Z);

        tee.LookupParameter("Angle").Set(angle);
        doc.Regenerate();
        s31.Origin = teecm3.Origin;
        teecm3.ConnectTo(s31);
    }
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        //Get application and document objects
        UIApplication uiApp = commandData.Application;
        Document doc = uiApp.ActiveUIDocument.Document;

        Transaction trans = new Transaction(doc);
        trans.Start("Lab");

        List<Tuberia> tuberias = new List<Tuberia>();
        List<UnionXYZ> uniones = new List<UnionXYZ>();
        
        Element pvc = new FilteredElementCollector(doc)
            .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves))
            .Where(e => e.Name.Equals("f pvc sanitaria 4"))
            .FirstOrDefault();
        Element systemTypes = new FilteredElementCollector(doc)
          .WherePasses(new ElementClassFilter(typeof(PipingSystemType)))
          .Where(e => e.Name.Equals("Hydronic Return"))
          .FirstOrDefault();

        List<CurveElement> guides = new FilteredElementCollector(doc)
            .WherePasses(new CurveElementFilter(CurveElementType.DetailCurve))
            .Cast<CurveElement>()
            .Where(e => e.GeometryCurve.GetType() == typeof(Line))
            .ToList();

        List<Level> levels = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Level)))
            .Cast<Level>()
            .OrderBy(e => e.Elevation)
            .ToList();

        foreach (CurveElement guide in guides)
        {
            XYZ start = guide.GeometryCurve.GetEndPoint(0);
            XYZ end = guide.GeometryCurve.GetEndPoint(1);
            double h = (start.Z + end.Z) / 2.0 + 0.001;
            Level currentLevel = levels[0];
            double currentH = currentLevel.Elevation;
            for (int i = 1; i < levels.Count; i++)
            {
                Level nextLevel = levels[i];
                double nextH = nextLevel.Elevation;
                if (currentH <= h && h <= nextH)
                {
                    break;
                }
                currentLevel = nextLevel;
                currentH = nextLevel.Elevation;
            }
            tuberias.Add(new Tuberia(start, end, currentLevel));
        }

        double epsilon = 0.0001;
        for (var i = 0; i < tuberias.Count; i++)
        {
            Tuberia currentTube = tuberias[i];
            XYZ start = currentTube.start;
            XYZ end = currentTube.end;

            Tuberia lastTube = null;
            foreach (Tuberia tubo in tuberias)
            {
                XYZ s0 = tubo.start;
                XYZ s1 = tubo.end;
                double d = s0.DistanceTo(s1);

                XYZ v = (s0 - s1).CrossProduct(XYZ.BasisZ).Normalize();
                double sv0 = v.DotProduct(start - s0);
                double sv1 = v.DotProduct(start - s1);

                double ds0 = start.DistanceTo(s0);
                double ds1 = start.DistanceTo(s1);

                double sv2 = v.DotProduct(end - s0);
                double sv3 = v.DotProduct(end - s1);

                double ds2 = end.DistanceTo(s0);
                double ds3 = end.DistanceTo(s1);
                
                if ((Math.Abs(sv0 + sv1) < 0.01) && (ds0 < d) && (ds1 < d) && (ds0 * ds1 > epsilon))
                {
                    lastTube = tubo;
                    tuberias.Add(new Tuberia(s0, start, tubo.level));
                    tuberias.Add(new Tuberia(start, s1, tubo.level));
                    i = -1;
                    break;
                }
                if ((Math.Abs(sv2 + sv3) < 0.01) && (ds2 < d) && (ds3 < d) && (ds2 * ds3 > epsilon))
                {
                    lastTube = tubo;
                    tuberias.Add(new Tuberia(end, s1, tubo.level));
                    tuberias.Add(new Tuberia(s0, end, tubo.level));
                    i = -1;
                    break;
                }
            }
            if (lastTube != null)
            {
                tuberias.Remove(lastTube);
            }
        }

        foreach (Tuberia tubo in tuberias)
        {
            tubo.tubo = Pipe.Create(doc, systemTypes.Id, pvc.Id, tubo.level.Id, tubo.start, tubo.end);
        }
        
        foreach (Tuberia tuboA in tuberias)
        {
            List<Tuberia> union = new List<Tuberia>();
            foreach (Tuberia tuboB in tuberias)
            {
                if (tuboB == tuboA)
                {
                    continue;
                }
                double d1 = tuboA.start.DistanceTo(tuboB.start);
                double d2 = tuboA.start.DistanceTo(tuboB.end);

                if (d1 < epsilon || d2 < epsilon)
                {
                    union.Add(tuboB);
                }
            }
            if (union.Count > 0)
            {
                union.Add(tuboA);
                UnionXYZ u = new UnionXYZ();
                if (union.Count > 2)
                {
                    XYZ v0 = union[0].end - union[0].start;
                    XYZ v1 = union[1].end - union[1].start;
                    XYZ v2 = union[2].end - union[2].start;

                    double av01 = v0.AngleTo(v1);
                    double av02 = v0.AngleTo(v2);
                    double av12 = v1.AngleTo(v2);

                    if (av01 < epsilon)
                    {
                        u.s1 = union[0];
                        u.s2 = union[1];
                        u.s3 = union[2];
                    } else if (av02 < epsilon)
                    {
                        u.s1 = union[0];
                        u.s2 = union[2];
                        u.s3 = union[1];
                    } else if (av12 < epsilon)
                    {
                        u.s1 = union[1];
                        u.s2 = union[2];
                        u.s3 = union[0];
                    }
                } else
                {
                    u.s1 = union[0];
                    u.s2 = union[1];
                }

                bool agregar = true;
                foreach (UnionXYZ un in uniones)
                {
                    if (un.s3 == null && u.s3 != null)
                    {
                        continue;
                    }
                    if ((un.s1 != u.s1 && un.s1 != u.s2 && un.s1 != u.s3) ||
                        (un.s2 != u.s1 && un.s2 != u.s2 && un.s2 != u.s3) ||
                        (un.s3 != u.s1 && un.s3 != u.s2 && un.s3 != u.s3))
                    {

                    } else
                    {
                        agregar = false;
                        break;
                    }
                }
                if (agregar)
                {
                    uniones.Add(u);
                }
            }
        }

        foreach (UnionXYZ union in uniones)
        {
            if (union.s3 != null)
            {
                Connect3Tubos(doc, union.s1.tubo, union.s2.tubo, union.s3.tubo);
            } else
            {
                Connect2Tubos(doc, union.s1.tubo, union.s2.tubo);
            }
        }
        
        trans.Commit();
        return Result.Succeeded;

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

        trans.Commit();
        return Result.Succeeded;
        */
    }
}

