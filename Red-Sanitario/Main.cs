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
    class ParSifonOffset
    {
        public FamilyInstance sifon;
        public XYZ offset;
        public ParSifonOffset(FamilyInstance sifon, XYZ offset)
        {
            this.sifon = sifon;
            this.offset = offset;
        }
    }
    class UnidadCanalizacion
    {
        public CurveElement guia;
        public XYZ bajante;
        public List<ParSifonOffset> pares = new List<ParSifonOffset>();
    }
    class UnionTuberia
    {
        public Pipe rama;
        public XYZ offset;
        public Pipe sup;
        public Pipe inf;
        public UnionTuberia(Pipe rama, XYZ offset)
        {
            this.rama = rama;
            this.offset = offset;
        }
    }
    void Connect3Pipes(Document doc, XYZ offset, Pipe p1, Pipe p2, Pipe p3)
    {
        FamilySymbol accesorioSymbol = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_PipeFitting)
            .Cast<FamilySymbol>()
            .Where<FamilySymbol>(e => e.Family.Name.Contains("Tee - Plain - PVC-C"))
            .FirstOrDefault();

        FamilyInstance tee = doc.Create.NewFamilyInstance(offset, accesorioSymbol, StructuralType.NonStructural);

        ConnectorManager cmpvc1 = p1.ConnectorManager;
        ConnectorManager cmpvc2 = p2.ConnectorManager;
        ConnectorManager cmpvc3 = p3.ConnectorManager;

        List<Connector> verticales = new List<Connector>();
        verticales.Add(cmpvc1.Lookup(0));
        verticales.Add(cmpvc1.Lookup(1));

        verticales.Add(cmpvc2.Lookup(0));
        verticales.Add(cmpvc2.Lookup(1));

        List<Connector> ramas = new List<Connector>();
        ramas.Add(cmpvc3.Lookup(0));
        ramas.Add(cmpvc3.Lookup(1));
        
        ConnectorManager cmtee = tee.MEPModel.ConnectorManager;
        Connector tc1 = cmtee.Lookup(1);
        Connector tc2 = cmtee.Lookup(2);
        Connector tc3 = cmtee.Lookup(3);
        
        double angle = (tc2.Origin - tc1.Origin).AngleTo(verticales[3].Origin - verticales[0].Origin);
        Line t0 = Line.CreateBound(tc1.Origin, tc2.Origin);
        Line t1 = Line.CreateBound(verticales[3].Origin, verticales[0].Origin);
        Line t2 = Line.CreateBound(ramas[1].Origin, ramas[0].Origin);
        double cp = t2.Direction.DotProduct(t1.Direction.CrossProduct(XYZ.BasisZ));

        double angleBranch = (verticales[3].Origin - verticales[0].Origin).AngleTo(ramas[1].Origin - ramas[0].Origin);
        Parameter radius = tee.LookupParameter("Nominal Radius");
        radius.Set(p1.Diameter / 2.0);

        Line axis = Line.CreateBound(offset, offset + offset.CrossProduct(verticales[0].Origin - verticales[3].Origin));
        if (cp > 0)
        {
            tee.LookupParameter("Angle").Set(angleBranch);
            ElementTransformUtils.RotateElement(doc, tee.Id, axis, angle + Math.PI);
        } else
        {
            tee.LookupParameter("Angle").Set(angleBranch - Math.PI / 2.0);
            ElementTransformUtils.RotateElement(doc, tee.Id, axis, angle);
        }

        Connector pc1 = null;
        double minDist = 99999999;
        foreach(Connector w in verticales)
        {
            double d = tc1.Origin.DistanceTo(w.Origin);
            if (minDist > d)
            {
                minDist = d;
                pc1 = w;
            }
        }

        Connector pc2 = null;
        minDist = 99999999;
        foreach (Connector w in verticales)
        {
            double d = tc2.Origin.DistanceTo(w.Origin);
            if (minDist > d)
            {
                minDist = d;
                pc2 = w;
            }
        }

        Connector pc3 = null;
        minDist = 99999999;
        foreach (Connector w in ramas)
        {
            double d = tc3.Origin.DistanceTo(w.Origin);
            if (minDist > d)
            {
                minDist = d;
                pc3 = w;
            }
        }

        tc1.ConnectTo(pc1);
        tc2.ConnectTo(pc2);
        tc3.ConnectTo(pc3);

        radius = p3.LookupParameter("Diameter");
        radius.Set(p3.Diameter / 2.0);
    }
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
    void ConnectTubos(Document doc, Pipe p1, Pipe p2, Pipe p3)
    {
        double epsilon = 0.0001;
        FamilySymbol accesorioSymbol = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_PipeFitting)
            .Cast<FamilySymbol>()
            .Where<FamilySymbol>(e => e.Family.Name.Contains("Tee - Plain - PVC-C"))
            .FirstOrDefault();
        
        ConnectorManager cmpvc1 = p1.ConnectorManager;
        ConnectorManager cmpvc2 = p2.ConnectorManager;
        ConnectorManager cmpvc3 = p3.ConnectorManager;

        XYZ s1 = null, s2 = null, s3 = null;

        XYZ p1start = cmpvc1.Lookup(0).Origin;
        XYZ p1end = cmpvc1.Lookup(1).Origin;

        XYZ p2start = cmpvc2.Lookup(0).Origin;
        XYZ p2end = cmpvc2.Lookup(1).Origin;

        XYZ p3start = cmpvc3.Lookup(0).Origin;
        XYZ p3end = cmpvc3.Lookup(1).Origin;

        XYZ offset = null;
        if ((p1end.DistanceTo(p2start) < epsilon) && (p1end.DistanceTo(p3start) < epsilon))
        {
            offset = p1end;
            s1 = p1start;
            s2 = p2end;
        }
        if ((p1start.DistanceTo(p2end) < epsilon) && (p1start.DistanceTo(p3start) < epsilon))
        {
            offset = p1start;
            s1 = p1start;
            s2 = p2start;
        }

        FamilyInstance tee = doc.Create.NewFamilyInstance(offset, accesorioSymbol, StructuralType.NonStructural);
        ConnectorManager cmtee = tee.MEPModel.ConnectorManager;
        Connector tc1 = cmtee.Lookup(1);
        Connector tc2 = cmtee.Lookup(2);
        Connector tc3 = cmtee.Lookup(3);

        double angle = (tc2.Origin - tc1.Origin).AngleTo(s2 - s1);
        Line t0 = Line.CreateBound(tc1.Origin, tc2.Origin);
        Line t1 = Line.CreateBound(s2, s1);
        Line t2 = Line.CreateBound(p3end, p3start);
        double cp = t2.Direction.DotProduct(t1.Direction.CrossProduct(XYZ.BasisZ));

        double angleBranch = (s2 - s1).AngleTo(p3end - p3start);
        Parameter radius = tee.LookupParameter("Nominal Radius");
        radius.Set(p1.Diameter / 2.0);
        
        tee.LookupParameter("Angle").Set(angleBranch);

        double cc = t0.Direction.DotProduct(t1.Direction);

        if (cc < 0)
        {

        } else
        {
            Line axis = Line.CreateBound(offset, offset + offset.CrossProduct(s2 - s1));
            ElementTransformUtils.RotateElement(doc, tee.Id, axis, -angle);
        }
        if (cp < 0)
        {
            ElementTransformUtils.RotateElement(doc, tee.Id, t1, Math.PI);
        }

        tc1.ConnectTo(cmpvc1.Lookup(1));
        tc2.ConnectTo(cmpvc2.Lookup(0));
        tc3.ConnectTo(cmpvc3.Lookup(0));

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
                ConnectTubos(doc, union.s1.tubo, union.s2.tubo, union.s3.tubo);
            }
        }
        
        trans.Commit();
        return Result.Succeeded;
        /*
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

        Family bajanteSanitariaFamily = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Family)))
            .Cast<Family>()
            .Where(e => e.Name.Equals("bajante sanitaria"))
            .FirstOrDefault();

        FamilySymbol bajanteSanitariaSymbol = new FilteredElementCollector(doc)
            .WherePasses(new FamilySymbolFilter(bajanteSanitariaFamily.Id))
            .Cast<FamilySymbol>()
            .Where(e => e.Name.Equals("bajante sanitaria"))
            .FirstOrDefault();

        IList<FamilyInstance> bajantes = new FilteredElementCollector(doc)
            .WherePasses(new FamilyInstanceFilter(doc, bajanteSanitariaSymbol.Id))
            .Cast<FamilyInstance>().ToList();

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
        

        /*
        List<UnidadCanalizacion> unidadCanalizaciones = new List<UnidadCanalizacion>();
        foreach (CurveElement guia in guias)
        {
            UnidadCanalizacion uc = new UnidadCanalizacion();
            uc.guia = guia;
            XYZ bajanteMasCercana = null;
            double distanciaBajanteMasCercana = 99999999999;
            foreach (FamilyInstance bajante in bajantes)
            {
                double d = ((LocationPoint)bajante.Location).Point.DistanceTo(guia.GeometryCurve.GetEndPoint(0));
                if (distanciaBajanteMasCercana > d)
                {
                    distanciaBajanteMasCercana = d;
                    bajanteMasCercana = ((LocationPoint)bajante.Location).Point;
                }
            }
            uc.bajante = bajanteMasCercana;

            foreach (FamilyInstance sifon in sifones)
            {
                CurveElement guiaMasCercana = null;
                double distanciaGuiaMasCercana = 999999999;
                XYZ offsetMasCercano = null;
                foreach (CurveElement guiaCercana in guias)
                {
                    XYZ s_0 = guiaCercana.GeometryCurve.GetEndPoint(0);
                    XYZ s_1 = guiaCercana.GeometryCurve.GetEndPoint(1);

                    XYZ p = ((LocationPoint)sifon.Location).Point;
                    XYZ p0 = new XYZ(p.X, p.Y, s_1.Z);

                    XYZ v = s_1 - s_0;
                    XYZ z = v.CrossProduct(p0 - s_0).Normalize();
                    XYZ w = z.CrossProduct(v).Normalize();
                    double d = w.DotProduct(p0 - s_0);
                    XYZ a = p0 - (d * w);
                    double distanciaCabo0 = a.DistanceTo(s_0);
                    double distanciaCabo1 = a.DistanceTo(s_1);
                    double largor = s_0.DistanceTo(s_1);
                    if (distanciaCabo0 <= largor && distanciaCabo1 <= largor)
                    {
                        d = w.DotProduct(p0 - s_0);
                    } else
                    {

                    }

                    if (distanciaGuiaMasCercana > d)
                    {
                        distanciaGuiaMasCercana = d;
                        guiaMasCercana = guiaCercana;
                        offsetMasCercano = a;
                    }
                }

                if (guiaMasCercana == guia)
                {
                    uc.pares.Add(new ParSifonOffset(sifon, offsetMasCercano));
                }
            }
            unidadCanalizaciones.Add(uc);
        }

        foreach (UnidadCanalizacion unidad in unidadCanalizaciones)
        {
            XYZ s0 = unidad.guia.GeometryCurve.GetEndPoint(0);
            XYZ s1 = unidad.guia.GeometryCurve.GetEndPoint(1);
            double m = (s0.Y - s1.Y) / (s0.X - s1.X);
            double b = s1.Y - (m * s1.X);
            
            Boolean bajanteArriba = true;
            double s0bajante = unidad.bajante.DistanceTo(s0);
            double s1bajante = unidad.bajante.DistanceTo(s1);

            if (s1bajante < s0bajante)
            {
                bajanteArriba = false;
            }

            List<UnionTuberia> uniones = new List<UnionTuberia>();
            foreach (ParSifonOffset par in unidad.pares)
            {
                FamilyInstance sifon = par.sifon;
                XYZ p = ((LocationPoint)sifon.Location).Point;
                XYZ p0 = new XYZ(p.X, p.Y, s0.Z);

                XYZ v = s1 - s0;
                XYZ z = v.CrossProduct(p0 - s0).Normalize();
                XYZ w = z.CrossProduct(v).Normalize();
                double d = w.DotProduct(p0 - s0);
                XYZ a = p0 - (d * w);

                XYZ offset;
                if (bajanteArriba)
                {
                    offset = (Math.Tan(Math.PI / 4.0) * d * (s0 - a).Normalize()) + a;
                }
                else
                {
                    offset = (Math.Tan(Math.PI / 4.0) * d * (s1 - a).Normalize()) + a;
                }
                Pipe rama = Pipe.Create(doc, systemTypes.Id, pvc.Id, sifon.LevelId, p0, offset - (0.16 * (offset - p0).Normalize()));
                uniones.Add(new UnionTuberia(rama, offset));
            }
        
            XYZ inicio = s1;
            XYZ fin;
            UnionTuberia lastUnion = null;
            foreach (UnionTuberia union in uniones) {
                XYZ offset = union.offset;

                fin = offset - (0.16 * (offset - inicio).Normalize());
                Pipe pipe = Pipe.Create(doc, systemTypes.Id, pvc.Id, sifones[0].LevelId, inicio, fin);
                union.inf = pipe;

                if (lastUnion != null)
                {
                    lastUnion.sup = pipe;
                }
                lastUnion = union;
                inicio = offset + (0.16 * (fin - inicio).Normalize());
            }

            UnionTuberia ultima = uniones.Last<UnionTuberia>();
            ultima.sup = Pipe.Create(doc, systemTypes.Id, pvc.Id, sifones[0].LevelId, lastUnion.offset - (0.16 * (lastUnion.offset - inicio).Normalize()), s0);

            foreach (UnionTuberia union in uniones)
            {
                if (bajanteArriba)
                {
                    Connect3Pipes(doc, union.offset, union.sup, union.inf, union.rama);
                } else
                {
                    Connect3Pipes(doc, union.offset, union.inf, union.sup, union.rama);
                }
            }
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

        trans.Commit();
        return Result.Succeeded;
        */
    }
}

