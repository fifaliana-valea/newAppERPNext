using System.Text.Json;
using MonProjetErpnext.Models;
using MonProjetErpnext.Services.Login;
using MonProjetErpnext.Models.Utiles;
using MonProjetErpnext.Models.Suppliers;


namespace MonProjetErpnext.Services
{
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

        public async Task<List<SupplierQuotation>> GetSupplierQuotations(string supplierId)
        {
            if (string.IsNullOrWhiteSpace(supplierId))
            {
                throw new ArgumentNullException(nameof(supplierId));
            }

            var encodedSupplierId = Uri.EscapeDataString(supplierId);
            var url = $"/api/resource/Supplier%20Quotation?fields=[\"name\",\"transaction_date\",\"status\",\"total\",\"supplier\",\"supplier_name\"]&filters=[[\"supplier\",\"=\",\"{encodedSupplierId}\"]]";
            
            var response = await _loginService.MakeAuthenticatedRequest(HttpMethod.Get, url);
            
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<ErpListResponse<SupplierQuotation>>(content, options);
            
            return result?.Data ?? new List<SupplierQuotation>();
        }
    }
}