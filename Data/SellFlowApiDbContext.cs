using Microsoft.EntityFrameworkCore;
using SellFlowApi.Models;

namespace SellFlowApi.Data
{
public class SellFlowApiDbContext(DbContextOptions<SellFlowApiDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();
    public DbSet<WhatsAppAccount> WhatsAppAccounts => Set<WhatsAppAccount>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
      public DbSet<PaymentProof> PaymentProofs => Set<PaymentProof>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    protected override void ConfigureConventions(ModelConfigurationBuilder cb)
    {
        cb.Properties<decimal>().HavePrecision(18, 2);   // naira with 2 decimal places
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<PaymentProof>().HasIndex(x => new { x.BusinessId, x.OrderId });
        b.Entity<ProductImage>().HasIndex(x => x.BusinessId);

        // Routing key: webhook phone_number_id -> business
        b.Entity<WhatsAppAccount>().HasIndex(x => x.PhoneNumberId).IsUnique();
        b.Entity<WhatsAppAccount>().HasIndex(x => x.BusinessId).IsUnique();

        b.Entity<Business>().HasOne(x => x.Settings).WithOne().HasForeignKey<BusinessSettings>(x => x.BusinessId);
        b.Entity<Business>().HasOne(x => x.WhatsApp).WithOne().HasForeignKey<WhatsAppAccount>(x => x.BusinessId);

        // A customer is unique per business + WhatsApp number
        b.Entity<Customer>().HasIndex(x => new { x.BusinessId, x.WhatsAppNumber }).IsUnique();
        b.Entity<Conversation>().HasIndex(x => new { x.BusinessId, x.CustomerId }).IsUnique();

        // Idempotency keys
        b.Entity<Message>().HasIndex(x => x.WhatsAppMessageId).IsUnique().HasFilter("\"WhatsAppMessageId\" IS NOT NULL");
        b.Entity<Payment>().HasIndex(x => x.Reference).IsUnique();
        b.Entity<Order>().HasIndex(x => x.OrderNumber).IsUnique();

        b.Entity<Product>().HasIndex(x => new { x.BusinessId, x.IsActive });
        b.Entity<Product>().ToTable(t => t.HasCheckConstraint("CK_Product_Stock", "\"StockQuantity\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"StockQuantity\""));

        b.Entity<Order>().HasOne(x => x.Delivery).WithOne().HasForeignKey<Delivery>(x => x.OrderId);
        b.Entity<Order>().HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId);
        b.Entity<Order>().HasMany(x => x.Reservations).WithOne().HasForeignKey(x => x.OrderId);
        b.Entity<Cart>().HasMany(x => x.Items).WithOne().HasForeignKey(x => x.CartId);
        b.Entity<OrderItem>().Ignore(x => x.LineTotal);
        b.Entity<Product>().Ignore(x => x.Available);

        // Seed the testing plan: change price here later, no code changes needed
        b.Entity<SubscriptionPlan>().HasData(new SubscriptionPlan
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Starter Plan", Price = 200m, DurationInDays = 30
        });
    }
}
}
