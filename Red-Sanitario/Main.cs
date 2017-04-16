using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Linq;
using System.Collections.Generic;

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
        Family columnFamily = new FilteredElementCollector(doc)
            .WherePasses(new ElementClassFilter(typeof(Family)))
            .Cast<Family>()
            .Where(e => e.Name.Equals("M_Concrete-Rectangular-Column"))
            .FirstOrDefault();

        if (columnFamily == null)
        {
            doc.LoadFamily("C:\\GitHub\\WallsSetup\\M_Concrete-Rectangular-Column.rfa", out columnFamily);
        }
        */

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
    }
}

