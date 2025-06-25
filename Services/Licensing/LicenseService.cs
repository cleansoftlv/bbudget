using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Services.DAL;
using Services.DAL.Entities;
using Services.Licensing.Revolut;
using Services.Licensing.Revolut.Dto;
using Services.Options;
using Shared.License;
using System.Text.Json;

namespace Services.Licensing
{
    public class LicenseService(IOptions<ServicesOptions> options,
            IServiceProvider serviceProvider,
            IDbContextFactory<CommonContext> dbFactory)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ServicesOptions _options = options.Value;
        private readonly IDbContextFactory<CommonContext> _dbFactory = dbFactory;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<LicensingInfoResponse> GetUserLicensingInfo(int userId)
        {
            var licenses = await GetUserPaidLicenses(userId);
            var orders = await GetCompletedAndRecentOrders(userId);
            var res = new LicensingInfoResponse
            {
                Licenses = licenses,
                Orders = orders
            };
            return res;
        }

        public async Task<List<PaidLicenseInfo>> GetUserPaidLicenses(int userId)
        {
            using var db = _dbFactory.CreateDbContext();
            var licenses = await db.PaidLicenses
            .Where(x => x.user_id == userId)
                .Select(x => new PaidLicenseInfo
                {
                    Id = x.id,
                    CreatedUtc = x.created,
                    FromUtc = x.date_from,
                    ToUtc = x.date_to,
                    Status = x.is_cancelled ? PaidLicenseStatus.Cancelled : PaidLicenseStatus.Active
                })
                .ToListAsync();

            foreach (var license in licenses.Where(x => x.Status == PaidLicenseStatus.Active))
            {
                if (license.ToUtc < DateTime.UtcNow)
                {
                    license.Status = PaidLicenseStatus.Expired;
                }
            }
            return licenses;
        }

        public async Task<List<OrderInfo>> GetCompletedAndRecentOrders(int userId)
        {
            var createdBorder = DateTime.UtcNow.AddDays(-1); 
            using var db = _dbFactory.CreateDbContext();
            var orders = await (from o in db.Orders
                                join p in db.Products on o.product_id equals p.id
                                where o.user_id == userId
                                    && (o.status == DAL.Entities.OrderStatus.Completed
                                    || o.status == DAL.Entities.OrderStatus.Refunded
                                    || (o.status == DAL.Entities.OrderStatus.Created && o.created > createdBorder))
                                select new
                                {
                                    o.id,
                                    o.created,
                                    o.amount,
                                    o.currency,
                                    o.status,
                                    product_days = p.days
                                }).ToListAsync();

            var res = orders.Select(x => new OrderInfo
            {
                Id = x.id,
                CreatedUtc = x.created,
                Amount = x.amount,
                Currency = x.currency,
                Status = GetOrderStatus(x.status),
                ProductDays = x.product_days
            }).ToList();

            return res;
        }


        public async Task<List<ProductInfo>> GetActiveProducts()
        {
            using var db = _dbFactory.CreateDbContext();

            var products = await db.Products
                .Where(x => !x.is_archived)
                .Select(x => new ProductInfo
                {
                    Id = x.id,
                    Name = x.name,
                    Days = x.days
                })
                .ToListAsync();

            var prices = await (from pp in db.ProductPrices
                                join p in db.Products
                                    on pp.product_id equals p.id
                                where !p.is_archived
                                    && !pp.is_archived
                                select pp).ToListAsync();

            var lookup = prices
                .ToLookup(db => db.product_id, db => new ProductPriceInfo
                {
                    Id = db.id,
                    Price = db.price,
                    Currency = db.currency
                });

            products.ForEach(product =>
            {
                product.Prices = lookup[product.Id].ToList();
            });

            return products;
        }


        public async Task<bool> ProcessOrderStatusUpdate(OrderWebhookEvent orderEvent)
        {
            int orderId = int.Parse(orderEvent.merchant_order_ext_ref);
            using var db = _dbFactory.CreateDbContext();
            var order = await db.Orders
                .FirstOrDefaultAsync(x => x.external_id == orderEvent.order_id && x.id == orderId);

            if (order == null)
            {
                throw new ArgumentException($"Order not found: {orderId}");
            }

            if (order.status == DAL.Entities.OrderStatus.Completed
                || order.status == DAL.Entities.OrderStatus.Refunded
                || order.status == DAL.Entities.OrderStatus.Cancelled
                || order.status == DAL.Entities.OrderStatus.Failed)
            {
                return false;
            }

            var revService = _serviceProvider.GetRequiredService<RevolutService>();
            var updatedOrder = await revService.GetOrder(order.external_id);
            var newStatus = ConverRevOrderStatus(updatedOrder.state);

            var now = DateTime.UtcNow;

            if (newStatus == DAL.Entities.OrderStatus.Completed)
            {
                var product = await db.Products
                    .FirstOrDefaultAsync(x => x.id == order.product_id && !x.is_archived);

                if (product == null)
                {
                    throw new ArgumentException($"Product not found: {order.product_id}");
                }

                var lastLiceseDate = db.PaidLicenses.Where(x => x.user_id == order.user_id)
                    .Max(x => (DateTime?)x.date_to);

                var dateFrom = lastLiceseDate > DateTime.UtcNow
                    ? lastLiceseDate.Value
                    : DateTime.UtcNow;

                var dateTo = AddProductDays(dateFrom, product.days);

                var newLicense = new PaidLicense
                {
                    user_id = order.user_id,
                    order_id = order.id,
                    date_from = dateFrom,
                    date_to = dateTo,
                    created = DateTime.UtcNow,
                    is_cancelled = false
                };
                db.PaidLicenses.Add(newLicense);
                await db.SaveChangesAsync();

                var accounts = await db.LMAccounts
                    .Where(x => x.user_id == order.user_id)
                    .ToListAsync();

                var accountLicenses = await (from a in db.LMAccounts
                                             join al in db.LMAccountLicenses
                                               on a.lm_account_id equals al.lm_account_id
                                             where a.user_id == order.user_id
                                             select al).ToListAsync();

                var licensesLookup = accountLicenses
                    .DistinctBy(x => x.lm_account_id)
                    .ToDictionary(x => x.lm_account_id, x => x);

                foreach (var acc in accounts.DistinctBy(x => x.lm_account_id))
                {
                    var license = licensesLookup.GetValueOrDefault(acc.lm_account_id);
                    if (license == null)
                    {
                        license = new LMAccountLicense
                        {
                            lm_account_id = acc.lm_account_id,
                            date_from = dateFrom,
                            date_to = dateTo,
                            is_paid = true,
                            created = DateTime.UtcNow,
                            modified = DateTime.UtcNow,
                            by_user_id = order.user_id,
                            paid_license_id = newLicense.id,
                        };
                        db.LMAccountLicenses.Add(license);
                    }
                    else if ((newLicense.date_to - license.date_to).TotalSeconds > 1)
                    {
                        license.date_to = newLicense.date_to;
                        license.is_paid = true;
                        license.by_user_id = order.user_id;
                        license.paid_license_id = newLicense.id;
                        license.modified = now;
                        license.date_from = now;
                    }
                }
            }

            order.external_status = JsonSerializer.Serialize(updatedOrder, _jsonOptions);
            order.status = newStatus;
            order.modified = now;
            await db.SaveChangesAsync();
            return true;
        }

        private static DAL.Entities.OrderStatus ConverRevOrderStatus(string revOrderState)
        {
            return revOrderState switch
            {
                OrderStates.Pending => DAL.Entities.OrderStatus.Created,
                OrderStates.Processing => DAL.Entities.OrderStatus.InProgress,
                OrderStates.Authorized => DAL.Entities.OrderStatus.InProgress,
                OrderStates.Cancelled => DAL.Entities.OrderStatus.Cancelled,
                OrderStates.Failed => DAL.Entities.OrderStatus.Failed,
                OrderStates.Completed => DAL.Entities.OrderStatus.Completed,
                _ => throw new ArgumentOutOfRangeException(nameof(RevOrder.state), revOrderState, null)
            };
        }

        public async Task<PaymentStatusInfo> GetOrderPaymentStatus(int orderId, int userId)
        {
            using var db = _dbFactory.CreateDbContext();
            var order = await db.Orders
                .FirstOrDefaultAsync(x => x.id == orderId && x.user_id == userId);

            if (order == null)
            {
                return null;
            }

            if (order.external_id == null
                || order.status == DAL.Entities.OrderStatus.Completed
                || order.status == DAL.Entities.OrderStatus.Failed
                || order.status == DAL.Entities.OrderStatus.Refunded
                || order.status == DAL.Entities.OrderStatus.Cancelled)
            {
                return new PaymentStatusInfo
                {
                    OrderId = order.id,
                    OrderStatus = GetOrderStatus(order.status),
                    CanRetry = false,
                    CheckoutUrl = null
                };
            }


            var revService = _serviceProvider.GetRequiredService<RevolutService>();

            RevOrder revOrder;
            try
            {
                revOrder = await revService.GetOrder(order.external_id);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Order not found, return the current status
                return new PaymentStatusInfo
                {
                    OrderId = order.id,
                    OrderStatus = GetOrderStatus(order.status),
                    CanRetry = false,
                    CheckoutUrl = null
                };
            }

            var status = ConverRevOrderStatus(revOrder.state);
            //Status update was not processed yet
            if (status == DAL.Entities.OrderStatus.Completed)
            {
                status = DAL.Entities.OrderStatus.InProgress;
            }

            var lastPayment = revOrder.payments?
                .OrderByDescending(x => x.created_at)
                .FirstOrDefault();

            return new PaymentStatusInfo
            {
                OrderId = order.id,
                OrderStatus = GetOrderStatus(status),
                CanRetry = status == DAL.Entities.OrderStatus.Created,
                CheckoutUrl = revOrder.checkout_url,
                LastPaymentStatus = lastPayment != null
                    ? PaymentStates.ToRevPaymentStatus(lastPayment.state)
                    : null,
                LastPaymentDeclineReason = lastPayment?.decline_reason,
                LastPaymentBankMessage = lastPayment?.bank_message
            };
        }


        public async Task<CreateOrderResponse> CreateOrder(int userId, int priceId)
        {
            using var db = _dbFactory.CreateDbContext();
            var price = await db.ProductPrices
                .FirstOrDefaultAsync(x => x.id == priceId && !x.is_archived);

            if (price == null)
            {
                throw new ArgumentException("Price not found or is archived");
            }

            var product = await db.Products
                .FirstOrDefaultAsync(x => x.id == price.product_id && !x.is_archived);

            if (product == null)
            {
                throw new ArgumentException("Product not found or is archived");
            }

            var now = DateTime.UtcNow;
            var order = new DAL.Entities.Order
            {
                user_id = userId,
                product_id = price.product_id,
                product_price_id = price.id,
                amount = price.price,
                currency = price.currency,
                status = DAL.Entities.OrderStatus.New,
                created = now,
                modified = now
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var service = _serviceProvider.GetRequiredService<RevolutService>();
            var revOrder = await service.CreateOrder(GetRevolutCreateOrderRequest(order, product.days));

            order.external_id = revOrder.id;
            order.status = DAL.Entities.OrderStatus.Created;
            order.modified = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return new CreateOrderResponse
            {
                Id = order.id,
                CheckoutUrl = revOrder.checkout_url
            };
        }
        private CreateOrderRequest GetRevolutCreateOrderRequest(Order order, int days)
        {
            var req = new CreateOrderRequest
            {
                amount = (int)Math.Round(order.amount * 100),
                currency = order.currency.ToUpper(),
                description = $"BBudget license - {LicenseCheckService.FormatProductInterval(days)}",
                statement_descriptor_suffix = _options.Revolut.StatementSufix,
                metadata = new Dictionary<string, string>
                {
                    { "user_id", order.id.ToString() },
                    { "price_id", order.product_price_id.ToString() },
                },
                merchant_order_data = new MerchantOrderData
                {
                    reference = order.id.ToString()
                },
                redirect_url = _options.Revolut.RedirectUrlPrefix + order.id
            };

            if (_options.Revolut.SettlementCurrency != null)
            {
                req.settlement_currency = _options.Revolut.SettlementCurrency.ToUpper();
            }

            return req;
        }

        private DateTime AddProductDays(DateTime date, int days)
        {
            if (days == -1)
            {
                return date.AddMonths(1);
            }
            else if (days == -2)
            {
                return date.AddYears(1);
            }
            return date.AddDays(days);
        }

        private static Shared.License.OrderStatus GetOrderStatus(DAL.Entities.OrderStatus status)
        {
            return status switch
            {
                DAL.Entities.OrderStatus.New => Shared.License.OrderStatus.New,
                DAL.Entities.OrderStatus.Created => Shared.License.OrderStatus.Created,
                DAL.Entities.OrderStatus.InProgress => Shared.License.OrderStatus.InProgress,
                DAL.Entities.OrderStatus.Refunded => Shared.License.OrderStatus.Refunded,
                DAL.Entities.OrderStatus.Failed => Shared.License.OrderStatus.Failed,
                DAL.Entities.OrderStatus.Completed => Shared.License.OrderStatus.Completed,
                DAL.Entities.OrderStatus.Cancelled => Shared.License.OrderStatus.Cancelled,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }


    }
}
