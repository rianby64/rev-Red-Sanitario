using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;

namespace PipesSetup
{
    [Transaction(TransactionMode.Manual)]
    public class PipesSetup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Result.Cancelled;
        }
    }
}
