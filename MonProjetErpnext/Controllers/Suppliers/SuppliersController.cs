using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MonProjetErpnext.Models.Request;
using MonProjetErpnext.Services.Login;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using MonProjetErpnext.Services;

namespace MonProjetErpnext.Controllers.Login;
public class SuppliersController : Controller
{
    private readonly ErpNextService _erpNextService;

    public SuppliersController(ErpNextService erpNextService)
    {
        _erpNextService = erpNextService;
    }

    public async Task<IActionResult> Index()
    {
        var suppliers = await _erpNextService.GetSuppliers();
        return View(suppliers);
    }
}