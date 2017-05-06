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
        public bool startConnected = false;
        public XYZ end;
        public bool endConnected = false;
        public Element lineStyle;
        public Level level;
        public Pipe tubo;
        public Tuberia(XYZ start, XYZ end, Element lineStyle, Level level)
        {
            this.start = start;
            this.end = end;
            this.lineStyle = lineStyle;
            this.level = level;
        }
    }
    public class UnionXYZ
    {
        public Tuberia s1;
        public Tuberia s2;
        public Tuberia s3;
    }
    void Connect2Tubos(Document doc, Tuberia t1, Tuberia t2)
    {
        if (t1 == null || t2 == null)
        {
            return;
        }
        Pipe p1 = t1.tubo;
        Pipe p2 = t2.tubo;

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

            t1.startConnected = true;
            t2.startConnected = true;
        }
        else if (p1end.DistanceTo(p2start) < epsilon)
        {
            s11 = cmpvc1.Lookup(0);
            s12 = cmpvc1.Lookup(1);

            s21 = cmpvc2.Lookup(0);
            s22 = cmpvc2.Lookup(1);

            t1.endConnected = true;
            t2.startConnected = true;
        }
        else if (p1end.DistanceTo(p2end) < epsilon)
        {
            s11 = cmpvc1.Lookup(0);
            s12 = cmpvc1.Lookup(1);

            s21 = cmpvc2.Lookup(1);
            s22 = cmpvc2.Lookup(0);

            t1.endConnected = true;
            t2.endConnected = true;
        }
        else if (p1start.DistanceTo(p2end) < epsilon)
        {
            s11 = cmpvc1.Lookup(1);
            s12 = cmpvc1.Lookup(0);

            s21 = cmpvc2.Lookup(1);
            s22 = cmpvc2.Lookup(0);

            t1.startConnected = true;
            t2.endConnected = true;
        }
        else 
        {
            return;
        }
        
        FamilyInstance tee = doc.Create.NewElbowFitting(s12, s21);
    }
    void Connect3Tubos(Document doc, Tuberia t1, Tuberia t2, Tuberia t3)
    {
        if (t1 == null || t2 == null)
        {
            return;
        }
        Pipe p1 = t1.tubo;
        Pipe p2 = t2.tubo;
        Pipe p3 = t3.tubo;

        Parameter r1 = p1.LookupParameter("Diameter");
        Parameter r2 = p2.LookupParameter("Diameter");
        Parameter r3 = p3.LookupParameter("Diameter");

        double rr1 = r1.AsDouble();
        double rr2 = r2.AsDouble();
        double rr3 = r3.AsDouble();

        r1.Set(0.1);
        r2.Set(0.1);
        r3.Set(0.1);
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
            // s12, s21, s31
            s11 = cmpvc1.Lookup(0); s12 = cmpvc1.Lookup(1);
            s21 = cmpvc2.Lookup(0); s22 = cmpvc2.Lookup(1);
            s31 = cmpvc3.Lookup(0); s32 = cmpvc3.Lookup(1);

            t1.endConnected = true;
            t2.startConnected = true;
            t3.startConnected = true;
        }
        else if (p3end.DistanceTo(p2start) < epsilon && p3end.DistanceTo(p1end) < epsilon)
        {
            // s12, s21, s31
            s11 = cmpvc1.Lookup(0); s12 = cmpvc1.Lookup(1);
            s21 = cmpvc2.Lookup(0); s22 = cmpvc2.Lookup(1);
            s31 = cmpvc3.Lookup(1); s32 = cmpvc3.Lookup(0);

            t1.endConnected = true;
            t2.startConnected = true;
            t3.endConnected = true;
        }
        else if (p3start.DistanceTo(p1start) < epsilon && p3start.DistanceTo(p2end) < epsilon)
        {
            // s12, s21, s31
            s11 = cmpvc1.Lookup(1); s12 = cmpvc1.Lookup(0);
            s21 = cmpvc2.Lookup(1); s22 = cmpvc2.Lookup(0);
            s31 = cmpvc3.Lookup(0); s32 = cmpvc3.Lookup(1);

            t1.startConnected = true;
            t2.endConnected = true;
            t3.startConnected = true;
        }
        else if (p3end.DistanceTo(p1start) < epsilon && p3end.DistanceTo(p2end) < epsilon)
        {
            // s12, s21, s31
            s11 = cmpvc1.Lookup(1); s12 = cmpvc1.Lookup(0);
            s21 = cmpvc2.Lookup(1); s22 = cmpvc2.Lookup(0);
            s31 = cmpvc3.Lookup(1); s32 = cmpvc3.Lookup(0);

            t1.startConnected = true;
            t2.endConnected = true;
            t3.endConnected = true;
        }
        else if (p3end.DistanceTo(p1start) < epsilon && p3end.DistanceTo(p2start) < epsilon)
        {
            // s12, s21, s31
            s11 = cmpvc1.Lookup(1); s12 = cmpvc1.Lookup(0);
            s21 = cmpvc2.Lookup(0); s22 = cmpvc2.Lookup(1);
            s31 = cmpvc3.Lookup(1); s32 = cmpvc3.Lookup(0);

            t1.startConnected = true;
            t2.startConnected = true;
            t3.endConnected = true;
        }
        else if (p3start.DistanceTo(p1start) < epsilon && p3start.DistanceTo(p2start) < epsilon)
        {
            // s12, s21, s31
            s11 = cmpvc1.Lookup(1); s12 = cmpvc1.Lookup(0);
            s21 = cmpvc2.Lookup(0); s22 = cmpvc2.Lookup(1);
            s31 = cmpvc3.Lookup(0); s32 = cmpvc3.Lookup(1);

            t1.startConnected = true;
            t2.startConnected = true;
            t3.startConnected = true;
        }
        else
        {
            return;
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

        r1.Set(rr1);
        r2.Set(rr2);
        r3.Set(rr3);
        tee.LookupParameter("Nominal Radius").Set(Math.Max(Math.Max(rr1, rr2), rr3) / 2.0);
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
        
        Group grupo = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Group)))
            .Cast<Group>()
            .Where(e => e.Name.Equals("sanitario"))
            .FirstOrDefault();

        List<CurveElement> guides = new FilteredElementCollector(doc)
            .WherePasses(new CurveElementFilter(CurveElementType.ModelCurve))
            .Cast<CurveElement>()
            .Where(e => e.GeometryCurve.GetType() == typeof(Line) && e.GroupId == grupo.Id)
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
            tuberias.Add(new Tuberia(
                new XYZ(start.X, start.Y, start.Z - UnitUtils.ConvertToInternalUnits(0.6, DisplayUnitType.DUT_METERS)),
                new XYZ(end.X, end.Y, end.Z - UnitUtils.ConvertToInternalUnits(0.6, DisplayUnitType.DUT_METERS)),
                guide.LineStyle, currentLevel));
        }

        double epsilon = 0.0001;
        for (int i = 0; i < tuberias.Count; i++)
        {
            Tuberia currentTube = tuberias[i];
            XYZ start = currentTube.start;
            XYZ end = currentTube.end;

            Tuberia lastTube = null;
            foreach (Tuberia tubo in tuberias)
            {
                if (currentTube.level != tubo.level)
                {
                    continue;
                }
                if (currentTube == tubo)
                {
                    continue;
                }
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
                    tuberias.Add(new Tuberia(s0, start, tubo.lineStyle, tubo.level));
                    tuberias.Add(new Tuberia(start, s1, tubo.lineStyle, tubo.level));
                    i = -1;
                    break;
                }
                if ((Math.Abs(sv2 + sv3) < 0.01) && (ds2 < d) && (ds3 < d) && (ds2 * ds3 > epsilon))
                {
                    lastTube = tubo;
                    tuberias.Add(new Tuberia(end, s1, tubo.lineStyle, tubo.level));
                    tuberias.Add(new Tuberia(s0, end, tubo.lineStyle, tubo.level));
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

            Parameter radius = null;
            string tn1 = tubo.lineStyle.Name;


            if (tn1 == "<Overhead>")
            {
                radius = tubo.tubo.LookupParameter("Diameter");
                radius.Set(1.0/6.0);
            }
            else
            {
                radius = tubo.tubo.LookupParameter("Diameter");
                radius.Set(2.0 / 6.0);
            }
        }
        
        foreach (Tuberia tuboA in tuberias)
        {
            List<Tuberia> unionStart = new List<Tuberia>();
            List<Tuberia> unionEnd = new List<Tuberia>();

            foreach (Tuberia tuboB in tuberias)
            {
                if (tuboB == tuboA)
                {
                    continue;
                }
                double d1 = tuboA.start.DistanceTo(tuboB.start);
                double d2 = tuboA.start.DistanceTo(tuboB.end);

                double d3 = tuboA.end.DistanceTo(tuboB.start);
                double d4 = tuboA.end.DistanceTo(tuboB.end);
                if (d1 < epsilon || d2 < epsilon)
                {
                    unionStart.Add(tuboB);
                }
                if (d3 < epsilon || d4 < epsilon)
                {
                    unionEnd.Add(tuboB);
                }
                if (unionStart.Count == 2 && unionEnd.Count == 2)
                {
                    break;
                }
            }
            if (unionStart.Count > 0)
            {
                unionStart.Add(tuboA);

                UnionXYZ u = new UnionXYZ();
                if (unionStart.Count > 2)
                {
                    XYZ v0 = unionStart[0].end - unionStart[0].start;
                    XYZ v1 = unionStart[1].end - unionStart[1].start;
                    XYZ v2 = unionStart[2].end - unionStart[2].start;

                    double av01 = v0.AngleTo(v1);
                    double av02 = v0.AngleTo(v2);
                    double av12 = v1.AngleTo(v2);

                    if (av01 < epsilon || Math.Abs(av01 - Math.PI) < epsilon)
                    {
                        u.s1 = unionStart[0];
                        u.s2 = unionStart[1];
                        u.s3 = unionStart[2];
                    }
                    else if (av02 < epsilon || Math.Abs(av02 - Math.PI) < epsilon)
                    {
                        u.s1 = unionStart[0];
                        u.s2 = unionStart[2];
                        u.s3 = unionStart[1];
                    }
                    else if (av12 < epsilon || Math.Abs(av12 - Math.PI) < epsilon)
                    {
                        u.s1 = unionStart[1];
                        u.s2 = unionStart[2];
                        u.s3 = unionStart[0];
                    }
                }
                else
                {
                    u.s1 = unionStart[0];
                    u.s2 = unionStart[1];
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

                    }
                    else
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
            if (unionEnd.Count > 0)
            {
                unionEnd.Add(tuboA);

                UnionXYZ u = new UnionXYZ();
                if (unionEnd.Count > 2)
                {
                    XYZ v0 = unionEnd[0].end - unionEnd[0].start;
                    XYZ v1 = unionEnd[1].end - unionEnd[1].start;
                    XYZ v2 = unionEnd[2].end - unionEnd[2].start;

                    double av01 = v0.AngleTo(v1);
                    double av02 = v0.AngleTo(v2);
                    double av12 = v1.AngleTo(v2);

                    if (av01 < epsilon || Math.Abs(av01 - Math.PI) < epsilon)
                    {
                        u.s1 = unionEnd[0];
                        u.s2 = unionEnd[1];
                        u.s3 = unionEnd[2];
                    }
                    else if (av02 < epsilon || Math.Abs(av02 - Math.PI) < epsilon)
                    {
                        u.s1 = unionEnd[0];
                        u.s2 = unionEnd[2];
                        u.s3 = unionEnd[1];
                    }
                    else if (av12 < epsilon || Math.Abs(av12 - Math.PI) < epsilon)
                    {
                        u.s1 = unionEnd[1];
                        u.s2 = unionEnd[2];
                        u.s3 = unionEnd[0];
                    }
                }
                else
                {
                    u.s1 = unionEnd[0];
                    u.s2 = unionEnd[1];
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
                Connect3Tubos(doc, union.s1, union.s2, union.s3);
            } else
            {
                Connect2Tubos(doc, union.s1, union.s2);
            }
        }
        
        foreach (Tuberia tubo in tuberias)
        {
            if (!tubo.startConnected)
            {
                XYZ start = tubo.start;
                XYZ end = new XYZ(start.X, start.Y, start.Z + UnitUtils.ConvertToInternalUnits(0.6, DisplayUnitType.DUT_METERS));
                Pipe tube = Pipe.Create(doc, systemTypes.Id, pvc.Id, tubo.level.Id, start, end);

                Parameter radius = tube.LookupParameter("Diameter");
                radius.Set(UnitUtils.ConvertToInternalUnits(1.5, DisplayUnitType.DUT_DECIMAL_INCHES));

                ConnectorManager cmpvc1 = tubo.tubo.ConnectorManager;
                ConnectorManager cmpvc2 = tube.ConnectorManager;

                doc.Create.NewElbowFitting(cmpvc1.Lookup(0), cmpvc2.Lookup(0));
            }
            if (!tubo.endConnected)
            {
                XYZ start = tubo.end;
                XYZ end = new XYZ(start.X, start.Y, start.Z + UnitUtils.ConvertToInternalUnits(0.6, DisplayUnitType.DUT_METERS));
                Pipe tube = Pipe.Create(doc, systemTypes.Id, pvc.Id, tubo.level.Id, start, end);

                Parameter radius = tube.LookupParameter("Diameter");
                radius.Set(UnitUtils.ConvertToInternalUnits(1.5, DisplayUnitType.DUT_DECIMAL_INCHES));

                ConnectorManager cmpvc1 = tubo.tubo.ConnectorManager;
                ConnectorManager cmpvc2 = tube.ConnectorManager;

                doc.Create.NewElbowFitting(cmpvc1.Lookup(1), cmpvc2.Lookup(0));
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

