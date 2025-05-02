using System.Text.Json;
using MonProjetErpnext.Models;
using MonProjetErpnext.Services.Login;


namespace MonProjetErpnext.Services;

public class ErpNextService
{
    private readonly LoginService _loginService;

    public ErpNextService(LoginService loginService)
    {
        _loginService = loginService;
    }

    public async Task<List<Supplier>> GetSuppliers()
    {
        var response = await _loginService.MakeAuthenticatedRequest(
            HttpMethod.Get, 
            "/api/resource/Supplier?fields=[\"name\",\"supplier_name\"]");
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ErpListResponse<Supplier>>(content);
        
        return result?.Data ?? new List<Supplier>();
    }
}