# newAppERPNext_C-

PayerFacture(string factureId)
        {
            try
            {
                var sid = _httpContextAccessor.HttpContext?.Session.GetString("sid");
                if (string.IsNullOrEmpty(sid))
                {
                    _logger.LogWarning("No SID found in session");
                    return false;
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Cookie", $"sid={sid}");

                // Retrieve invoice details
                var invoiceResponse = await _httpClient.GetAsync($"{API_URL}/{factureId}");
                var invoiceContent = await invoiceResponse.Content.ReadAsStringAsync();

                if (!invoiceResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to retrieve invoice details: {invoiceContent}");
                    return false;
                }

                var invoice = JsonSerializer.Deserialize<JsonElement>(invoiceContent).GetProperty("data");

                // Create Payment Entry
                var paymentEntry = new
                {
                    docstatus = 1,
                    payment_type = "Pay",
                    party_type = "Supplier",
                    party = invoice.GetProperty("supplier").GetString(),
                    paid_amount = invoice.GetProperty("grand_total").GetDecimal(),
                    received_amount = invoice.GetProperty("grand_total").GetDecimal(),
                    source_exchange_rate = 1.0,
                    paid_from = "Bank Account - MTRM",
                    paid_from_account_currency = "EUR",
                    paid_to = "2110 - Créditeurs - MTRM",
                    
                    paid_to_account_currency = "EUR",
                    mode_of_payment = "Cash",
                    reference_no = $"REF-{DateTime.Now:yyyyMMdd}-{factureId}",
                    reference_date = DateTime.Now.ToString("yyyy-MM-dd"),
                    references = new[]
                    {
                        new
                        {
                            reference_doctype = "Purchase Invoice",
                            reference_name = factureId,
                            total_amount = invoice.GetProperty("grand_total").GetDecimal(),
                            outstanding_amount = invoice.GetProperty("grand_total").GetDecimal(),
                            allocated_amount = invoice.GetProperty("grand_total").GetDecimal()
                        }
                    }
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(paymentEntry), Encoding.UTF8, "application/json");
                var paymentResponse = await _httpClient.PostAsync("http://172.25.36.0:8000/api/resource/Payment Entry", jsonContent);
                var paymentContent = await paymentResponse.Content.ReadAsStringAsync();

                if (paymentResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Payment for invoice {factureId} created successfully");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to create payment for invoice {factureId}: {paymentContent}");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error paying invoice {factureId}");
                return false;
            }
        }
Arisoa
public async Task<IActionResult> PayerFacture(string factureId)
        {
            var result = await _factureAchatService.PayerFacture(factureId);
            if (result)
            {
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = "Erreur lors du paiement de la facture.";
                return RedirectToAction("Index");
            }
        }
Arisoa
view:
  <tbody>
            @foreach (var facture in Model)
            {
                <tr>
                    <td>@facture.name</td>
                    <td>@facture.supplier</td>
                    <td>@facture.posting_date?.ToString("dd/MM/yyyy")</td>
                    <td>@facture.grand_total.ToString("N2")</td>
                    <td>@facture.currency</td>
                    <td>
                        @if (facture.status == "Paid")
                        {
                            <span class="status status-paid">Payée</span>
                        }
                        else
                        {
                            <span class="status status-unpaid">Non Payée</span>
                        }
                    </td>
                    <td>
                        @if (facture.status != "Paid")
                        {
                            <a href="/FactureAchat/PayerFacture?factureId=@facture.name" class="btn-payer">Payer</a>
                        }
                    </td>
                </tr>
            }
        </tbody>