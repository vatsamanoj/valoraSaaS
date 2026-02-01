namespace Valora.Api.Application.Schemas.TemplateConfig;

/// <summary>
/// Demo complex calculation expressions for Sales Order Template Extension.
/// These examples demonstrate C# template-driven calculations for various business scenarios.
/// </summary>
public static class ComplexCalculationDemos
{
    // ==================== VOLUME DISCOUNT CALCULATION ====================
    
    /// <summary>
    /// Volume Discount Calculation
    /// Applies tiered discounts based on quantity purchased.
    /// </summary>
    public static readonly ComplexCalculation VolumeDiscountDemo = new()
    {
        Id = "volume-discount-demo",
        Name = "Volume Discount Calculator",
        Description = "Calculates tiered discounts based on quantity: 5-9: 5%, 10-24: 10%, 25-49: 15%, 50+: 20%",
        TargetField = "LineDiscountPercent",
        Scope = CalculationScope.LineItem,
        Trigger = "onChange",
        Expression = @"
            // Get the quantity from the line item
            var qty = GetParameter<decimal>(""Qty"");
            
            // Apply tiered discount logic
            decimal discountPercent = qty switch
            {
                >= 50 => 20m,    // 20% for 50+ items
                >= 25 => 15m,   // 15% for 25-49 items
                >= 10 => 10m,   // 10% for 10-24 items
                >= 5 => 5m,     // 5% for 5-9 items
                _ => 0m         // No discount for less than 5
            };
            
            return discountPercent;
        ",
        Parameters = new List<CalculationParameter>
        {
            new() { Name = "Qty", Source = "Qty", DataType = "decimal", IsRequired = true },
            new() { Name = "UnitPrice", Source = "UnitPrice", DataType = "decimal", IsRequired = false }
        },
        AssemblyReferences = new List<string> { "System", "System.Linq" }
    };

    // ==================== TIERED PRICING CALCULATION ====================
    
    /// <summary>
    /// Tiered Pricing Calculation
    /// Different unit prices based on quantity tiers.
    /// </summary>
    public static readonly ComplexCalculation TieredPricingDemo = new()
    {
        Id = "tiered-pricing-demo",
        Name = "Tiered Pricing Calculator",
        Description = "Applies different unit prices based on quantity tiers",
        TargetField = "UnitPrice",
        Scope = CalculationScope.LineItem,
        Trigger = "onChange",
        Expression = @"
            var basePrice = GetParameter<decimal>(""BasePrice"");
            var qty = GetParameter<decimal>(""Qty"");
            
            // Tiered pricing multipliers
            decimal multiplier = qty switch
            {
                >= 100 => 0.70m,  // 30% off for 100+
                >= 50 => 0.80m,   // 20% off for 50-99
                >= 25 => 0.90m,   // 10% off for 25-49
                >= 10 => 0.95m,   // 5% off for 10-24
                _ => 1.00m        // Standard price
            };
            
            return Math.Round(basePrice * multiplier, 2);
        ",
        Parameters = new List<CalculationParameter>
        {
            new() { Name = "BasePrice", Source = "BasePrice", DataType = "decimal", IsRequired = true },
            new() { Name = "Qty", Source = "Qty", DataType = "decimal", IsRequired = true }
        },
        AssemblyReferences = new List<string> { "System" }
    };

    // ==================== WEIGHTED AVERAGE DISCOUNT ====================
    
    /// <summary>
    /// Weighted Average Discount for Document Level
    /// Calculates overall discount percentage based on line item values.
    /// </summary>
    public static readonly ComplexCalculation WeightedAverageDiscountDemo = new()
    {
        Id = "weighted-avg-discount-demo",
        Name = "Weighted Average Discount",
        Description = "Calculates document-level weighted average discount based on line items",
        TargetField = "OverallDiscountPercent",
        Scope = CalculationScope.Document,
        Trigger = "onLineChange",
        Expression = @"
            var lineItems = GetParameter<List<LineItem>>(""LineItems"");
            
            if (lineItems == null || !lineItems.Any())
                return 0m;
            
            decimal totalValue = 0m;
            decimal totalDiscount = 0m;
            
            foreach (var item in lineItems)
            {
                var lineValue = item.Qty * item.UnitPrice;
                var lineDiscount = lineValue * (item.DiscountPercent / 100);
                
                totalValue += lineValue;
                totalDiscount += lineDiscount;
            }
            
            if (totalValue == 0)
                return 0m;
            
            return Math.Round((totalDiscount / totalValue) * 100, 2);
        ",
        Parameters = new List<CalculationParameter>
        {
            new() { Name = "LineItems", Source = "Items", DataType = "List<LineItem>", IsRequired = true }
        },
        AssemblyReferences = new List<string> { "System", "System.Linq", "System.Collections.Generic" }
    };

    // ==================== TAX CALCULATION WITH JURISDICTION ====================
    
    /// <summary>
    /// Tax Calculation with Multiple Jurisdictions
    /// Calculates tax based on customer location and product category.
    /// </summary>
    public static readonly ComplexCalculation TaxWithJurisdictionDemo = new()
    {
        Id = "tax-jurisdiction-demo",
        Name = "Multi-Jurisdiction Tax Calculator",
        Description = "Calculates tax based on customer state and product tax category",
        TargetField = "TaxAmount",
        Scope = CalculationScope.LineItem,
        Trigger = "onChange",
        Expression = @"
            var state = GetParameter<string>(""CustomerState"");
            var productCategory = GetParameter<string>(""ProductCategory"");
            var lineAmount = GetParameter<decimal>(""LineAmount"");
            
            // Tax rates by state (simplified example)
            var stateTaxRate = state?.ToUpper() switch
            {
                ""CA"" => 0.0725m,    // California
                ""NY"" => 0.08m,      // New York
                ""TX"" => 0.0625m,    // Texas
                ""FL"" => 0.06m,      // Florida
                _ => 0.05m            // Default
            };
            
            // Category modifiers
            var categoryModifier = productCategory?.ToLower() switch
            {
                ""food"" => 0m,           // No tax on food
                ""luxury"" => 0.03m,      // Additional 3% luxury tax
                ""electronics"" => 0m,    // Standard rate
                _ => 0m
            };
            
            var totalTaxRate = stateTaxRate + categoryModifier;
            return Math.Round(lineAmount * totalTaxRate, 2);
        ",
        Parameters = new List<CalculationParameter>
        {
            new() { Name = "CustomerState", Source = "Customer.State", DataType = "string", IsRequired = true },
            new() { Name = "ProductCategory", Source = "Product.Category", DataType = "string", IsRequired = true },
            new() { Name = "LineAmount", Source = "LineAmount", DataType = "decimal", IsRequired = true }
        },
        AssemblyReferences = new List<string> { "System" }
    };

    // ==================== SHIPPING COST CALCULATION ====================
    
    /// <summary>
    /// Shipping Cost Calculator
    /// Calculates shipping based on weight, dimensions, and destination zone.
    /// </summary>
    public static readonly ComplexCalculation ShippingCostDemo = new()
    {
        Id = "shipping-cost-demo",
        Name = "Shipping Cost Calculator",
        Description = "Calculates shipping cost based on weight, dimensions, and zone",
        TargetField = "ShippingCost",
        Scope = CalculationScope.Document,
        Trigger = "onLineChange",
        Expression = @"
            var totalWeight = GetParameter<decimal>(""TotalWeight"");
            var zone = GetParameter<int>(""ShippingZone"");
            var isExpress = GetParameter<bool>(""IsExpressShipping"");
            
            // Base rate per pound by zone
            decimal baseRatePerLb = zone switch
            {
                1 => 0.50m,   // Local
                2 => 0.75m,   // Regional
                3 => 1.00m,   // National
                4 => 2.00m,   // International
                _ => 1.50m    // Default
            };
            
            decimal shippingCost = totalWeight * baseRatePerLb;
            
            // Express shipping multiplier
            if (isExpress)
            {
                shippingCost *= 1.5m;  // 50% extra for express
            }
            
            // Minimum shipping charge
            if (shippingCost < 5.00m)
            {
                shippingCost = 5.00m;
            }
            
            return Math.Round(shippingCost, 2);
        ",
        Parameters = new List<CalculationParameter>
        {
            new() { Name = "TotalWeight", Source = "TotalWeight", DataType = "decimal", IsRequired = true },
            new() { Name = "ShippingZone", Source = "Customer.ShippingZone", DataType = "int", IsRequired = true },
            new() { Name = "IsExpressShipping", Source = "IsExpress", DataType = "bool", IsRequired = false }
        },
        AssemblyReferences = new List<string> { "System" }
    };

    // ==================== COMMISSION CALCULATION ====================
    
    /// <summary>
    /// Sales Commission Calculator
    /// Tiered commission based on salesperson performance.
    /// </summary>
    public static readonly ComplexCalculation CommissionDemo = new()
    {
        Id = "commission-demo",
        Name = "Sales Commission Calculator",
        Description = "Calculates tiered commission for sales representatives",
        TargetField = "CommissionAmount",
        Scope = CalculationScope.Document,
        Trigger = "onSave",
        Expression = @"
            var netAmount = GetParameter<decimal>(""NetAmount"");
            var salespersonId = GetParameter<string>(""SalespersonId"");
            var ytdSales = GetExternalData<decimal>(""YtdSales"", salespersonId);
            
            // Commission tiers based on YTD performance
            decimal commissionRate = ytdSales switch
            {
                >= 1000000m => 0.15m,  // 15% for $1M+ YTD
                >= 500000m => 0.12m,   // 12% for $500K-$1M
                >= 100000m => 0.10m,   // 10% for $100K-$500K
                _ => 0.05m              // 5% base rate
            };
            
            // Bonus for large orders
            if (netAmount > 50000)
            {
                commissionRate += 0.02m;  // Extra 2% for big deals
            }
            
            return Math.Round(netAmount * commissionRate, 2);
        ",
        Parameters = new List<CalculationParameter>
        {
            new() { Name = "NetAmount", Source = "NetAmount", DataType = "decimal", IsRequired = true },
            new() { Name = "SalespersonId", Source = "SalespersonId", DataType = "string", IsRequired = true }
        },
        ExternalDataSources = new List<ExternalDataSource>
        {
            new()
            {
                Name = "YtdSales",
                SourceType = "Database",
                QueryOrEndpoint = "SELECT SUM(NetAmount) FROM SalesOrders WHERE SalespersonId = @SalespersonId AND Year = @CurrentYear",
                Parameters = new Dictionary<string, string>
                {
                    { "SalespersonId", "{{SalespersonId}}" },
                    { "CurrentYear", "{{CurrentYear}}" }
                }
            }
        },
        AssemblyReferences = new List<string> { "System", "System.Linq" }
    };

    // ==================== CURRENCY CONVERSION ====================
    
    /// <summary>
    /// Real-time Currency Conversion
    /// Converts amounts using external exchange rates.
    /// </summary>
    public static readonly ComplexCalculation CurrencyConversionDemo = new()
    {
        Id = "currency-conversion-demo",
        Name = "Currency Converter",
        Description = "Converts line amounts to customer currency using real-time rates",
        TargetField = "ConvertedAmount",
        Scope = CalculationScope.LineItem,
        Trigger = "onChange",
        Expression = @"
            var amount = GetParameter<decimal>(""Amount"");
            var fromCurrency = GetParameter<string>(""DocumentCurrency"");
            var toCurrency = GetParameter<string>(""CustomerCurrency"");
            
            if (fromCurrency == toCurrency)
                return amount;
            
            // Get exchange rate from external API
            var rate = GetExternalData<decimal>(""ExchangeRate"", $""{fromCurrency}_{toCurrency}"");
            
            if (rate == 0)
            {
                // Fallback to default rate
                rate = GetFallbackRate(fromCurrency, toCurrency);
            }
            
            return Math.Round(amount * rate, 2);
        ",
        CodeBlock = @"
            private decimal GetFallbackRate(string from, string to)
            {
                // Fallback rates when API is unavailable
                var rates = new Dictionary<string, decimal>
                {
                    { ""USD_EUR"", 0.85m },
                    { ""EUR_USD"", 1.18m },
                    { ""USD_GBP"", 0.73m },
                    { ""GBP_USD"", 1.37m }
                };
                
                var key = $""{from}_{to}"";
                return rates.ContainsKey(key) ? rates[key] : 1m;
            }
        ",
        Parameters = new List<CalculationParameter>
        {
            new() { Name = "Amount", Source = "LineAmount", DataType = "decimal", IsRequired = true },
            new() { Name = "DocumentCurrency", Source = "Currency", DataType = "string", IsRequired = true },
            new() { Name = "CustomerCurrency", Source = "Customer.Currency", DataType = "string", IsRequired = true }
        },
        ExternalDataSources = new List<ExternalDataSource>
        {
            new()
            {
                Name = "ExchangeRate",
                SourceType = "API",
                QueryOrEndpoint = "https://api.exchangerate-api.com/v4/latest/{fromCurrency}",
                Parameters = new Dictionary<string, string>()
            }
        },
        AssemblyReferences = new List<string> { "System", "System.Collections.Generic", "System.Net.Http" }
    };

    // ==================== PROMOTIONAL PRICING ====================
    
    /// <summary>
    /// Promotional Pricing with Time Windows
    /// Applies promotional prices during specific date ranges.
    /// </summary>
    public static readonly ComplexCalculation PromotionalPricingDemo = new()
    {
        Id = "promo-pricing-demo",
        Name = "Promotional Pricing Calculator",
        Description = "Applies promotional discounts based on active promotions",
        TargetField = "PromoPrice",
        Scope = CalculationScope.LineItem,
        Trigger = "onChange",
        Expression = @"
            var basePrice = GetParameter<decimal>(""BasePrice"");
            var productId = GetParameter<string>(""ProductId"");
            var orderDate = GetParameter<DateTime>(""OrderDate"");
            var customerTier = GetParameter<string>(""CustomerTier"");
            
            // Check active promotions
            var activePromos = GetExternalData<List<Promotion>>(""ActivePromotions"", productId);
            
            decimal bestDiscount = 0m;
            
            foreach (var promo in activePromos)
            {
                if (promo.StartDate <= orderDate && promo.EndDate >= orderDate)
                {
                    // Check customer tier eligibility
                    if (promo.EligibleTiers.Contains(customerTier) || promo.EligibleTiers.Contains(""All""))
                    {
                        if (promo.DiscountPercent > bestDiscount)
                        {
                            bestDiscount = promo.DiscountPercent;
                        }
                    }
                }
            }
            
            return Math.Round(basePrice * (1 - bestDiscount / 100), 2);
        ",
        Parameters = new List<CalculationParameter>
        {
            new() { Name = "BasePrice", Source = "BasePrice", DataType = "decimal", IsRequired = true },
            new() { Name = "ProductId", Source = "ProductId", DataType = "string", IsRequired = true },
            new() { Name = "OrderDate", Source = "DocumentDate", DataType = "DateTime", IsRequired = true },
            new() { Name = "CustomerTier", Source = "Customer.Tier", DataType = "string", IsRequired = true }
        },
        ExternalDataSources = new List<ExternalDataSource>
        {
            new()
            {
                Name = "ActivePromotions",
                SourceType = "Database",
                QueryOrEndpoint = "SELECT * FROM Promotions WHERE ProductId = @ProductId AND IsActive = 1",
                Parameters = new Dictionary<string, string>
                {
                    { "ProductId", "{{ProductId}}" }
                }
            }
        },
        AssemblyReferences = new List<string> { "System", "System.Linq", "System.Collections.Generic" }
    };

    // ==================== UTILITY METHODS ====================

    /// <summary>
    /// Get all demo calculations for reference
    /// </summary>
    public static List<ComplexCalculation> GetAllDemos()
    {
        return new List<ComplexCalculation>
        {
            VolumeDiscountDemo,
            TieredPricingDemo,
            WeightedAverageDiscountDemo,
            TaxWithJurisdictionDemo,
            ShippingCostDemo,
            CommissionDemo,
            CurrencyConversionDemo,
            PromotionalPricingDemo
        };
    }

    /// <summary>
    /// Get demo by ID
    /// </summary>
    public static ComplexCalculation? GetDemoById(string id)
    {
        return GetAllDemos().FirstOrDefault(d => d.Id == id);
    }
}

/// <summary>
/// Helper class for line items in calculations
/// </summary>
public class LineItem
{
    public string ProductId { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineAmount => Qty * UnitPrice * (1 - DiscountPercent / 100);
}

/// <summary>
/// Helper class for promotions
/// </summary>
public class Promotion
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DiscountPercent { get; set; }
    public List<string> EligibleTiers { get; set; } = new();
    public bool IsActive { get; set; }
}
