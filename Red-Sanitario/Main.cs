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
        else if (p3end.DistanceTo(p1end) < epsilon && p3end.DistanceTo(p2end) < epsilon)
        {
            // s12, s21, s31
            s11 = cmpvc1.Lookup(0); s12 = cmpvc1.Lookup(1);
            s21 = cmpvc2.Lookup(1); s22 = cmpvc2.Lookup(0);
            s31 = cmpvc3.Lookup(1); s32 = cmpvc3.Lookup(0);

            t1.endConnected = true;
            t2.startConnected = true;
            t3.endConnected = true;
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
    public void GenerarTuberias(List<CurveElement> guides, List<Level> levels, List<Tuberia> tuberias, double offsetInMeters)
    {
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
                new XYZ(start.X, start.Y, start.Z - UnitUtils.ConvertToInternalUnits(offsetInMeters, DisplayUnitType.DUT_METERS)),
                new XYZ(end.X, end.Y, end.Z - UnitUtils.ConvertToInternalUnits(offsetInMeters, DisplayUnitType.DUT_METERS)),
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
    }
    public void CrearTubos(Document doc, Element systemTypes, Element pvc, List<Tuberia> tuberias)
    {
        foreach (Tuberia tubo in tuberias)
        {
            tubo.tubo = Pipe.Create(doc, systemTypes.Id, pvc.Id, tubo.level.Id, tubo.start, tubo.end);

            Parameter radius = null;
            string tn1 = tubo.lineStyle.Name;


            if (tn1 == "<Overhead>")
            {
                radius = tubo.tubo.LookupParameter("Diameter");
                radius.Set(1.0 / 6.0);
            }
            else
            {
                radius = tubo.tubo.LookupParameter("Diameter");
                radius.Set(2.0 / 6.0);
            }
        }
    }
    public void GenerarUniones(List<Tuberia> tuberias, List<UnionXYZ> uniones)
    {
        double epsilon = 0.001;
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
        }
    }
    public void CrearUniones(Document doc, List<UnionXYZ> uniones)
    {
        foreach (UnionXYZ union in uniones)
        {
            if (union.s3 != null)
            {
                Connect3Tubos(doc, union.s1, union.s2, union.s3);
            }
            else
            {
                Connect2Tubos(doc, union.s1, union.s2);
            }
        }
    }
    public void CrearPuntosACobrarVentilacion(Document doc, Element systemTypes, Element pvc, List<Tuberia> tuberias,
        Element puntoVentilacionFamily, IList<FamilyInstance> bajantes, List<Tuberia> conexionBajantes)
    {
        foreach (Tuberia tubo in tuberias)
        {
            Pipe tube = null;
            FamilyInstance puntoACobrar = null;
            XYZ salienteXYZ = null;
            Connector connect = null;

            if (!tubo.startConnected)
            {
                XYZ start = tubo.start;
                salienteXYZ = start;
                XYZ v = (tubo.start - tubo.end).Normalize();
                XYZ end = start + 0.3 * v;
                tube = Pipe.Create(doc, systemTypes.Id, pvc.Id, tubo.level.Id, start, end);

                Parameter radius = tube.LookupParameter("Diameter");
                radius.Set(tubo.tubo.LookupParameter("Diameter").AsDouble());

                ConnectorManager cmpvc1 = tubo.tubo.ConnectorManager;
                ConnectorManager cmpvc2 = tube.ConnectorManager;

                connect = cmpvc1.Lookup(0);
                puntoACobrar = doc.Create.NewUnionFitting(cmpvc1.Lookup(0), cmpvc2.Lookup(0));
                doc.Regenerate();
                puntoACobrar.ChangeTypeId(puntoVentilacionFamily.Id);
                //doc.Delete(tube.Id);
            }
            if (!tubo.endConnected)
            {
                XYZ start = tubo.end;
                salienteXYZ = start;
                XYZ v = (tubo.end - tubo.start).Normalize();
                XYZ end = start + 0.3 * v;
                tube = Pipe.Create(doc, systemTypes.Id, pvc.Id, tubo.level.Id, start, end);

                Parameter radius = tube.LookupParameter("Diameter");
                radius.Set(tubo.tubo.LookupParameter("Diameter").AsDouble());

                ConnectorManager cmpvc1 = tubo.tubo.ConnectorManager;
                ConnectorManager cmpvc2 = tube.ConnectorManager;

                connect = cmpvc1.Lookup(1);
                puntoACobrar = doc.Create.NewUnionFitting(cmpvc1.Lookup(1), cmpvc2.Lookup(0));
                doc.Regenerate();
                puntoACobrar.ChangeTypeId(puntoVentilacionFamily.Id);
                //doc.Delete(tube.Id);
            }

            if (puntoACobrar != null)
            {
                double distanciaMinima = 999999;
                FamilyInstance bajanteMinimo = null;
                foreach (FamilyInstance bajante in bajantes)
                {
                    double d = ((LocationPoint)(bajante.Location)).Point.DistanceTo(salienteXYZ);
                    if (distanciaMinima > d)
                    {
                        distanciaMinima = d;
                        bajanteMinimo = bajante;
                    }
                }
                double ww = UnitUtils.ConvertToInternalUnits(0.6, DisplayUnitType.DUT_METERS);
                if (distanciaMinima < ww)
                {
                    doc.Delete(puntoACobrar.Id);
                    connect.Origin = salienteXYZ;
                    conexionBajantes.Add(tubo);
                    continue;
                }
            }
        }
    }
    public void CrearPuntosACobrarSanitario(Document doc, Element systemTypes, Element pvc, List<Tuberia> tuberias,
        Element puntoSifonFamily, Element puntoSanitarioFamily, Element puntoLavamanosFamily,
        IList<FamilyInstance> sifones, IList<FamilyInstance> sanitarios, IList<FamilyInstance> lavamanos,
        IList<FamilyInstance> bajantes, List<Tuberia> conexionBajantes)
    {
        foreach (Tuberia tubo in tuberias)
        {
            Pipe tube = null;
            FamilyInstance elbow = null;
            FamilyInstance puntoACobrar = null;
            XYZ salienteXYZ = null;
            Pipe salienteTube = null;

            if (!tubo.startConnected)
            {
                XYZ start = tubo.start;
                XYZ end = new XYZ(start.X, start.Y, start.Z + UnitUtils.ConvertToInternalUnits(0.3, DisplayUnitType.DUT_METERS));
                tube = Pipe.Create(doc, systemTypes.Id, pvc.Id, tubo.level.Id, start, end);

                Parameter radius = tube.LookupParameter("Diameter");

                string tn = tubo.lineStyle.Name;
                if (tn == "<Overhead>")
                {
                    radius.Set(UnitUtils.ConvertToInternalUnits(1.5, DisplayUnitType.DUT_DECIMAL_INCHES));
                }
                else
                {
                    radius.Set(tubo.tubo.LookupParameter("Diameter").AsDouble());
                }

                ConnectorManager cmpvc1 = tubo.tubo.ConnectorManager;
                ConnectorManager cmpvc2 = tube.ConnectorManager;

                elbow = doc.Create.NewElbowFitting(cmpvc1.Lookup(0), cmpvc2.Lookup(0));
                if (tn == "<Overhead>")
                {
                    elbow.LookupParameter("Nominal Radius").Set(tubo.tubo.LookupParameter("Diameter").AsDouble() / 2.0);
                    doc.Regenerate();
                    XYZ originInferior = elbow.MEPModel.ConnectorManager.Lookup(2).Origin;
                    cmpvc2.Lookup(0).Origin = new XYZ(originInferior.X, originInferior.Y, cmpvc2.Lookup(0).Origin.Z);
                    cmpvc2.Lookup(1).Origin = new XYZ(originInferior.X, originInferior.Y, cmpvc2.Lookup(1).Origin.Z);
                }

                salienteXYZ = new XYZ(cmpvc2.Lookup(1).Origin.X, cmpvc2.Lookup(1).Origin.Y, cmpvc2.Lookup(1).Origin.Z + UnitUtils.ConvertToInternalUnits(0.3, DisplayUnitType.DUT_METERS));
                salienteTube = Pipe.Create(doc, systemTypes.Id, pvc.Id, tubo.level.Id, cmpvc2.Lookup(1).Origin, salienteXYZ);
                salienteTube.LookupParameter("Diameter").Set(tube.LookupParameter("Diameter").AsDouble());

                puntoACobrar = doc.Create.NewUnionFitting(cmpvc2.Lookup(1), salienteTube.ConnectorManager.Lookup(0));
                puntoACobrar.ChangeTypeId(puntoSanitarioFamily.Id);
            }
            if (!tubo.endConnected)
            {
                XYZ start = tubo.end;
                XYZ end = new XYZ(start.X, start.Y, start.Z + UnitUtils.ConvertToInternalUnits(0.3, DisplayUnitType.DUT_METERS));
                tube = Pipe.Create(doc, systemTypes.Id, pvc.Id, tubo.level.Id, start, end);

                Parameter radius = tube.LookupParameter("Diameter");

                string tn = tubo.lineStyle.Name;
                if (tn == "<Overhead>")
                {
                    radius.Set(UnitUtils.ConvertToInternalUnits(1.5, DisplayUnitType.DUT_DECIMAL_INCHES));
                }
                else
                {
                    radius.Set(tubo.tubo.LookupParameter("Diameter").AsDouble());
                }

                ConnectorManager cmpvc1 = tubo.tubo.ConnectorManager;
                ConnectorManager cmpvc2 = tube.ConnectorManager;

                elbow = doc.Create.NewElbowFitting(cmpvc1.Lookup(1), cmpvc2.Lookup(0));
                if (tn == "<Overhead>")
                {
                    elbow.LookupParameter("Nominal Radius").Set(tubo.tubo.LookupParameter("Diameter").AsDouble() / 2.0);
                    doc.Regenerate();
                    XYZ originInferior = elbow.MEPModel.ConnectorManager.Lookup(2).Origin;
                    cmpvc2.Lookup(0).Origin = new XYZ(originInferior.X, originInferior.Y, cmpvc2.Lookup(0).Origin.Z);
                    cmpvc2.Lookup(1).Origin = new XYZ(originInferior.X, originInferior.Y, cmpvc2.Lookup(1).Origin.Z);
                }

                salienteXYZ = new XYZ(cmpvc2.Lookup(1).Origin.X, cmpvc2.Lookup(1).Origin.Y, cmpvc2.Lookup(1).Origin.Z + UnitUtils.ConvertToInternalUnits(0.3, DisplayUnitType.DUT_METERS));
                salienteTube = Pipe.Create(doc, systemTypes.Id, pvc.Id, tubo.level.Id, cmpvc2.Lookup(1).Origin, salienteXYZ);
                salienteTube.LookupParameter("Diameter").Set(tube.LookupParameter("Diameter").AsDouble());

                puntoACobrar = doc.Create.NewUnionFitting(cmpvc2.Lookup(1), salienteTube.ConnectorManager.Lookup(0));
                puntoACobrar.ChangeTypeId(puntoSanitarioFamily.Id);
            }

            if (salienteXYZ != null && salienteTube != null)
            {
                double distanciaMinima = 999999;
                FamilyInstance bajanteMinimo = null;
                foreach (FamilyInstance bajante in bajantes)
                {
                    double d = ((LocationPoint)(bajante.Location)).Point.DistanceTo(salienteXYZ);
                    if (distanciaMinima > d)
                    {
                        distanciaMinima = d;
                        bajanteMinimo = bajante;
                    }
                }
                double ww = UnitUtils.ConvertToInternalUnits(0.3, DisplayUnitType.DUT_METERS);
                if (distanciaMinima < ww)
                {
                    doc.Delete(elbow.Id);
                    doc.Delete(puntoACobrar.Id);
                    doc.Delete(tube.Id);
                    if (!tubo.endConnected)
                    {
                        XYZ punto = tubo.tubo.ConnectorManager.Lookup(1).Origin;
                        tubo.tubo.ConnectorManager.Lookup(1).Origin = new XYZ(salienteXYZ.X, salienteXYZ.Y, punto.Z);
                    }
                    else if (!tubo.startConnected)
                    {
                        XYZ punto = tubo.tubo.ConnectorManager.Lookup(0).Origin;
                        tubo.tubo.ConnectorManager.Lookup(0).Origin = new XYZ(salienteXYZ.X, salienteXYZ.Y, punto.Z);
                    }
                    conexionBajantes.Add(tubo);
                    continue;
                }

                distanciaMinima = 999999;
                FamilyInstance sifonMinimo = null;
                foreach (FamilyInstance sifon in sifones)
                {
                    double d = ((LocationPoint)(sifon.Location)).Point.DistanceTo(salienteXYZ);
                    if (distanciaMinima > d)
                    {
                        distanciaMinima = d;
                        sifonMinimo = sifon;
                    }
                }
                ww = UnitUtils.ConvertToInternalUnits(0.3, DisplayUnitType.DUT_METERS);
                if (distanciaMinima < ww)
                {
                    XYZ punto = ((LocationPoint)(sifonMinimo.Location)).Point;
                    XYZ puntoSifon = salienteTube.ConnectorManager.Lookup(1).Origin;
                    salienteTube.ConnectorManager.Lookup(1).Origin = new XYZ(puntoSifon.X, puntoSifon.Y, punto.Z);

                    puntoACobrar.ChangeTypeId(puntoSifonFamily.Id);
                    continue;
                }

                distanciaMinima = 999999;
                FamilyInstance lavamanoMinimo = null;
                foreach (FamilyInstance lavamano in lavamanos)
                {
                    double d = ((LocationPoint)(lavamano.Location)).Point.DistanceTo(salienteXYZ);
                    if (distanciaMinima > d)
                    {
                        distanciaMinima = d;
                        lavamanoMinimo = lavamano;
                    }
                }
                ww = UnitUtils.ConvertToInternalUnits(1.0, DisplayUnitType.DUT_METERS);
                if (distanciaMinima < ww)
                {
                    XYZ punto = ((LocationPoint)(lavamanoMinimo.Location)).Point;
                    XYZ puntoLavamanos = salienteTube.ConnectorManager.Lookup(1).Origin;
                    salienteTube.ConnectorManager.Lookup(1).Origin = new XYZ(puntoLavamanos.X, puntoLavamanos.Y, punto.Z);

                    puntoACobrar.ChangeTypeId(puntoLavamanosFamily.Id);
                    continue;
                }

                distanciaMinima = 999999;
                FamilyInstance sanitarioMinimo = null;
                foreach (FamilyInstance sanitario in sanitarios)
                {
                    double d = ((LocationPoint)(sanitario.Location)).Point.DistanceTo(salienteXYZ);
                    if (distanciaMinima > d)
                    {
                        distanciaMinima = d;
                        sanitarioMinimo = sanitario;
                    }
                }
                ww = UnitUtils.ConvertToInternalUnits(0.8, DisplayUnitType.DUT_METERS);
                if (distanciaMinima < ww)
                {
                    XYZ punto = ((LocationPoint)(sanitarioMinimo.Location)).Point;
                    XYZ puntoSanitario = salienteTube.ConnectorManager.Lookup(1).Origin;
                    salienteTube.ConnectorManager.Lookup(1).Origin = new XYZ(puntoSanitario.X, puntoSanitario.Y, punto.Z);

                    puntoACobrar.ChangeTypeId(puntoSanitarioFamily.Id);
                    continue;
                }
            }
        }
    }
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        //Get application and document objects
        UIApplication uiApp = commandData.Application;
        Document doc = uiApp.ActiveUIDocument.Document;

        List<Tuberia> tuberiasSanitarias = new List<Tuberia>();
        List<UnionXYZ> unionesSanitarias = new List<UnionXYZ>();
        List<Tuberia> conexionBajantesSanitarias = new List<Tuberia>();
        
        Element pvcSanitaria = new FilteredElementCollector(doc)
            .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves))
            .Where(e => e.Name.Equals("pvc sanitaria"))
            .FirstOrDefault();
        Element sanitarioType = new FilteredElementCollector(doc)
          .WherePasses(new ElementClassFilter(typeof(PipingSystemType)))
          .Where(e => e.Name.Equals("Sanitario"))
          .FirstOrDefault();

        Group grupoSanitario = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Group)))
            .Cast<Group>()
            .Where(e => e.Name.Equals("sanitario"))
            .FirstOrDefault();

        List<CurveElement> guidesSanitario = new FilteredElementCollector(doc)
            .WherePasses(new CurveElementFilter(CurveElementType.ModelCurve))
            .Cast<CurveElement>()
            .Where(e => e.GeometryCurve.GetType() == typeof(Line) && e.GroupId == grupoSanitario.Id)
            .ToList();



        List<Tuberia> tuberiasVentilacion = new List<Tuberia>();
        List<UnionXYZ> unionesVentilacion = new List<UnionXYZ>();
        List<Tuberia> conexionBajantesVentilacion = new List<Tuberia>();

        Element pvcVentilacion = new FilteredElementCollector(doc)
            .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves))
            .Where(e => e.Name.Equals("pvc ventilacion"))
            .FirstOrDefault();
        Element ventilacionType = new FilteredElementCollector(doc)
          .WherePasses(new ElementClassFilter(typeof(PipingSystemType)))
          .Where(e => e.Name.Equals("Ventilacion"))
          .FirstOrDefault();

        Group grupoVentilacion = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Group)))
            .Cast<Group>()
            .Where(e => e.Name.Equals("ventilacion"))
            .FirstOrDefault();

        List<CurveElement> guidesVentilacion = new FilteredElementCollector(doc)
            .WherePasses(new CurveElementFilter(CurveElementType.ModelCurve))
            .Cast<CurveElement>()
            .Where(e => e.GeometryCurve.GetType() == typeof(Line) && e.GroupId == grupoVentilacion.Id)
            .ToList();



        List<Level> levels = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Level)))
            .Cast<Level>()
            .OrderBy(e => e.Elevation)
            .ToList();
        
        Element puntoSifonFamily = new FilteredElementCollector(doc)
            .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting))
            .Where(e => e.Name.Equals("punto sifon"))
            .FirstOrDefault();

        Element puntoSanitarioFamily = new FilteredElementCollector(doc)
            .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting))
            .Where(e => e.Name.Equals("punto sanitario"))
            .FirstOrDefault();

        Element puntoLavamanosFamily = new FilteredElementCollector(doc)
            .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting))
            .Where(e => e.Name.Equals("punto lavamanos"))
            .FirstOrDefault();

        Element puntoVentilacionFamily = new FilteredElementCollector(doc)
            .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting))
            .Where(e => e.Name.Equals("punto ventilacion"))
            .FirstOrDefault();

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

        IList<FamilyInstance> sifones = new FilteredElementCollector(doc)
            .WherePasses(new FamilyInstanceFilter(doc, sifonSymbol.Id))
            .Cast<FamilyInstance>().ToList();


        Family sanitarioFamily = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Family)))
            .Cast<Family>()
            .Where(e => e.Name.Equals("sanitario fmly"))
            .FirstOrDefault();

        FamilySymbol sanitarioSymbol = new FilteredElementCollector(doc)
            .WherePasses(new FamilySymbolFilter(sanitarioFamily.Id))
            .Cast<FamilySymbol>()
            .Where(e => e.Name.Equals("Snt1"))
            .FirstOrDefault();

        IList<FamilyInstance> sanitarios = new FilteredElementCollector(doc)
            .WherePasses(new FamilyInstanceFilter(doc, sanitarioSymbol.Id))
            .Cast<FamilyInstance>().ToList();
        

        Family lavamanosFamily = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Family)))
            .Cast<Family>()
            .Where(e => e.Name.Equals("lavamanos monserrat"))
            .FirstOrDefault();

        FamilySymbol lavamanosSymbol = new FilteredElementCollector(doc)
            .WherePasses(new FamilySymbolFilter(lavamanosFamily.Id))
            .Cast<FamilySymbol>()
            .Where(e => e.Name.Equals("LM1"))
            .FirstOrDefault();

        IList<FamilyInstance> lavamanos = new FilteredElementCollector(doc)
            .WherePasses(new FamilyInstanceFilter(doc, lavamanosSymbol.Id))
            .Cast<FamilyInstance>().ToList();


        Family bajanteSanitarioFamily = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Family)))
            .Cast<Family>()
            .Where(e => e.Name.Equals("bajante sanitario"))
            .FirstOrDefault();

        FamilySymbol bajanteSanitarioSymbol = new FilteredElementCollector(doc)
            .WherePasses(new FamilySymbolFilter(bajanteSanitarioFamily.Id))
            .Cast<FamilySymbol>()
            .Where(e => e.Name.Equals("bajante sanitario"))
            .FirstOrDefault();

        IList<FamilyInstance> bajantesSanitario = new FilteredElementCollector(doc)
            .WherePasses(new FamilyInstanceFilter(doc, bajanteSanitarioSymbol.Id))
            .Cast<FamilyInstance>().ToList();

        
        FamilySymbol bajanteVentilacionSymbol = new FilteredElementCollector(doc)
            .WherePasses(new FamilySymbolFilter(bajanteSanitarioFamily.Id))
            .Cast<FamilySymbol>()
            .Where(e => e.Name.Equals("bajante ventilacion"))
            .FirstOrDefault();

        IList<FamilyInstance> bajantesVentilacion = new FilteredElementCollector(doc)
            .WherePasses(new FamilyInstanceFilter(doc, bajanteSanitarioSymbol.Id))
            .Cast<FamilyInstance>().ToList();



        Transaction trans = new Transaction(doc);
        trans.Start("Lab");

        GenerarTuberias(guidesSanitario, levels, tuberiasSanitarias, 0.6);
        CrearTubos(doc, sanitarioType, pvcSanitaria, tuberiasSanitarias);
        GenerarUniones(tuberiasSanitarias, unionesSanitarias);
        CrearUniones(doc, unionesSanitarias);
        CrearPuntosACobrarSanitario(doc, sanitarioType, pvcSanitaria, tuberiasSanitarias,
            puntoSifonFamily, puntoSanitarioFamily, puntoLavamanosFamily,
            sifones, sanitarios, lavamanos,
            bajantesSanitario, conexionBajantesSanitarias);


        GenerarTuberias(guidesVentilacion, levels, tuberiasVentilacion, 0.495);
        CrearTubos(doc, ventilacionType, pvcVentilacion, tuberiasVentilacion);
        GenerarUniones(tuberiasVentilacion, unionesVentilacion);
        CrearUniones(doc, unionesVentilacion);
        CrearPuntosACobrarVentilacion(doc, ventilacionType, pvcVentilacion, tuberiasVentilacion,
            puntoVentilacionFamily,
            bajantesVentilacion, conexionBajantesVentilacion);
        
        List<UnionXYZ> unionesBajantesSanitariasVerticales = new List<UnionXYZ>();
        foreach (Tuberia tubo in conexionBajantesSanitarias)
        {
            Level level = tubo.level;
            Tuberia tuboMasCercano1 = null;
            Tuberia tuboMasCercano2 = null;
            double distancia = 999999999;

            Connector conector1 = null;
            XYZ XYZconector1 = null;

            Connector conector2 = null;
            XYZ XYZconector2 = null;

            Connector conector3 = null;
            XYZ XYZconector3 = null;
            if (!tubo.endConnected)
            {
                conector3 = tubo.tubo.ConnectorManager.Lookup(1);
                XYZconector3 = conector3.Origin;
            }
            if (!tubo.startConnected)
            {
                conector3 = tubo.tubo.ConnectorManager.Lookup(0);
                XYZconector3 = conector3.Origin;
            }
            foreach (Tuberia t in conexionBajantesSanitarias)
            {
                if (t == tubo) continue;
                Connector c = null;
                XYZ xyz = null;
                if (!t.endConnected)
                {
                    c = t.tubo.ConnectorManager.Lookup(1);
                    xyz = c.Origin;
                }
                if (!t.startConnected)
                {
                    c = t.tubo.ConnectorManager.Lookup(0);
                    xyz = c.Origin;
                }

                double d = xyz.DistanceTo(XYZconector3);
                double a = (xyz - XYZconector3).Normalize().AngleTo(XYZ.BasisZ);
                if (distancia > d && (Math.Abs(a - Math.PI) < 0.0001 || Math.Abs(a) < 0.0001))
                {
                    tuboMasCercano1 = t;
                    distancia = d;

                    conector1 = c;
                    XYZconector1 = xyz;
                }
            }

            distancia = 999999999;
            foreach (Tuberia t in conexionBajantesSanitarias)
            {
                if (t == tubo) continue;
                if (t == tuboMasCercano1) continue;
                Connector c = null;
                XYZ xyz = null;
                if (!t.endConnected)
                {
                    c = t.tubo.ConnectorManager.Lookup(1);
                    xyz = c.Origin;
                }
                if (!t.startConnected)
                {
                    c = t.tubo.ConnectorManager.Lookup(0);
                    xyz = c.Origin;
                }

                double d = xyz.DistanceTo(XYZconector3);
                double a = (xyz - XYZconector3).Normalize().AngleTo(XYZ.BasisZ);
                if (distancia > d && (Math.Abs(a - Math.PI) < 0.0001 || Math.Abs(a) < 0.0001))
                {
                    tuboMasCercano2 = t;
                    distancia = d;

                    conector2 = c;
                    XYZconector2 = xyz;
                }
            }
            
            XYZ d1 = (XYZconector3 - XYZconector1).Normalize();
            XYZ d2 = (XYZconector3 - XYZconector2).Normalize();
            double w = d1.AngleTo(d2);
            if ((Math.Abs(d1.AngleTo(d2) - Math.PI) < 0.0001 || Math.Abs(d1.AngleTo(d2)) < 0.0001) && d1.Z * d2.Z < 0)
            {
                bool hacerTuboUp = true;
                Pipe tubeUp = null;
                Tuberia tubeUpTuberia = null;
                foreach (UnionXYZ unionRevisar in unionesBajantesSanitariasVerticales)
                {
                    if (Math.Abs(unionRevisar.s1.end.DistanceTo(XYZconector3)) < 0.001 &&
                        Math.Abs(unionRevisar.s1.start.DistanceTo(XYZconector1)) < 0.001)
                    {
                        hacerTuboUp = false;
                        tubeUpTuberia = unionRevisar.s1;
                        break;
                    }
                    if (Math.Abs(unionRevisar.s2.end.DistanceTo(XYZconector3)) < 0.001 &&
                        Math.Abs(unionRevisar.s2.start.DistanceTo(XYZconector1)) < 0.001)
                    {
                        hacerTuboUp = false;
                        tubeUpTuberia = unionRevisar.s2;
                        break;
                    }
                }

                UnionXYZ union = new UnionXYZ();
                if (hacerTuboUp)
                {
                    tubeUp = Pipe.Create(doc, sanitarioType.Id, pvcSanitaria.Id, tubo.level.Id, XYZconector3, XYZconector1);
                    tubeUp.LookupParameter("Diameter").Set(tubo.tubo.LookupParameter("Diameter").AsDouble());

                    union.s1 = new Tuberia(XYZconector3, XYZconector1, tubo.lineStyle, tubo.level);
                    union.s1.tubo = tubeUp;
                }
                else
                {
                    union.s1 = tubeUpTuberia;
                }


                bool hacerTuboDown = true;
                Pipe tubeDown = null;
                Tuberia tubeDownTuberia = null;
                foreach (UnionXYZ unionRevisar in unionesBajantesSanitariasVerticales)
                {
                    if (Math.Abs(unionRevisar.s2.end.DistanceTo(XYZconector3)) < 0.001 &&
                        Math.Abs(unionRevisar.s2.start.DistanceTo(XYZconector2)) < 0.001)
                    {
                        hacerTuboDown = false;
                        tubeDownTuberia = unionRevisar.s2;
                        break;
                    }
                    if (Math.Abs(unionRevisar.s1.end.DistanceTo(XYZconector2)) < 0.001 &&
                        Math.Abs(unionRevisar.s1.start.DistanceTo(XYZconector3)) < 0.001)
                    {
                        hacerTuboDown = false;
                        tubeDownTuberia = unionRevisar.s1;
                        break;
                    }
                }
                
                if (hacerTuboDown)
                {
                    tubeDown = Pipe.Create(doc, sanitarioType.Id, pvcSanitaria.Id, tubo.level.Id, XYZconector3, XYZconector2);
                    tubeDown.LookupParameter("Diameter").Set(tubo.tubo.LookupParameter("Diameter").AsDouble());

                    union.s2 = new Tuberia(XYZconector3, XYZconector2, tubo.lineStyle, tubo.level);
                    union.s2.tubo = tubeDown;
                }
                else
                {
                    union.s2 = tubeDownTuberia;
                }
                
                union.s3 = tubo;
                unionesBajantesSanitariasVerticales.Add(union);
            }
            else
            {
                bool hacerTubo = true;
                Pipe tubeUp = null;
                Tuberia tubeUpTuberia = null;
                foreach (UnionXYZ unionRevisar in unionesBajantesSanitariasVerticales)
                {
                    if (Math.Abs(unionRevisar.s1.end.DistanceTo(XYZconector1)) < 0.001 &&
                        Math.Abs(unionRevisar.s1.start.DistanceTo(XYZconector3)) < 0.001)
                    {
                        hacerTubo = false;
                        tubeUpTuberia = unionRevisar.s1;
                        break;
                    }
                    if (Math.Abs(unionRevisar.s2.end.DistanceTo(XYZconector3)) < 0.001 &&
                        Math.Abs(unionRevisar.s2.start.DistanceTo(XYZconector1)) < 0.001)
                    {
                        hacerTubo = false;
                        tubeUpTuberia = unionRevisar.s2;
                        break;
                    }
                }
                UnionXYZ union = new UnionXYZ();
                if (hacerTubo)
                {
                    tubeUp = Pipe.Create(doc, sanitarioType.Id, pvcSanitaria.Id, tubo.level.Id, XYZconector3, XYZconector1);
                    tubeUp.LookupParameter("Diameter").Set(tubo.tubo.LookupParameter("Diameter").AsDouble());
                    
                    union.s1 = new Tuberia(XYZconector3, XYZconector1, tubo.lineStyle, tubo.level);
                    union.s1.tubo = tubeUp;
                }
                else
                {
                    union.s1 = tubeUpTuberia;
                }

                union.s2 = tubo;
                unionesBajantesSanitariasVerticales.Add(union);
            }
        }


        CrearUniones(doc, unionesBajantesSanitariasVerticales);

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

