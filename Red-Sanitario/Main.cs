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

        List<CurveElement> guias = new FilteredElementCollector(doc)
            .WherePasses(new CurveElementFilter(CurveElementType.DetailCurve))
            .Cast<CurveElement>()
            .Where(e => e.GeometryCurve.GetType() == typeof(Line))
            .ToList();

        List<TextElement> bajantes = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(TextElement)))
            .Cast<TextElement>()
            .Where(e => e.Text.Equals("bajante"))
            .ToList();

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

        List<UnidadCanalizacion> unidadCanalizaciones = new List<UnidadCanalizacion>();
        foreach (CurveElement guia in guias)
        {
            UnidadCanalizacion uc = new UnidadCanalizacion();
            uc.guia = guia;
            XYZ bajanteMasCercana = null;
            double distanciaBajanteMasCercana = 99999999999;
            foreach (TextNote bajante in bajantes)
            {
                double d = bajante.Coord.DistanceTo(guia.GeometryCurve.GetEndPoint(0));
                if (distanciaBajanteMasCercana > d)
                {
                    distanciaBajanteMasCercana = d;
                    bajanteMasCercana = bajante.Coord;
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
        */

        trans.Commit();
        return Result.Succeeded;
    }
}

