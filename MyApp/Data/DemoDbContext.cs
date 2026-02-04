using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MyApp;

public partial class DemoDbContext : DbContext
{
    public DemoDbContext()
    {
    }

    public DemoDbContext(DbContextOptions<DemoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=trade;Username=postgres;Password=123");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Orderid).HasName("orders_pkey");

            entity.ToTable("orders");

            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Orderdeliverydate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("orderdeliverydate");
            entity.Property(e => e.Orderpickuppoint).HasColumnName("orderpickuppoint");
            entity.Property(e => e.Orderstatus).HasColumnName("orderstatus");

            entity.HasMany(d => d.Productarticlenumbers).WithMany(p => p.Orders)
                .UsingEntity<Dictionary<string, object>>(
                    "Orderproduct",
                    r => r.HasOne<Product>().WithMany()
                        .HasForeignKey("Productarticlenumber")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("orderproduct_productarticlenumber_fkey"),
                    l => l.HasOne<Order>().WithMany()
                        .HasForeignKey("Orderid")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("orderproduct_orderid_fkey"),
                    j =>
                    {
                        j.HasKey("Orderid", "Productarticlenumber").HasName("orderproduct_pkey");
                        j.ToTable("orderproduct");
                        j.IndexerProperty<int>("Orderid").HasColumnName("orderid");
                        j.IndexerProperty<string>("Productarticlenumber")
                            .HasMaxLength(100)
                            .HasColumnName("productarticlenumber");
                    });
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Productarticlenumber).HasName("product_pkey");

            entity.ToTable("product");

            entity.Property(e => e.Productarticlenumber)
                .HasMaxLength(100)
                .HasColumnName("productarticlenumber");
            entity.Property(e => e.Productcategory).HasColumnName("productcategory");
            entity.Property(e => e.Productcost)
                .HasPrecision(19, 4)
                .HasColumnName("productcost");
            entity.Property(e => e.Productdescription).HasColumnName("productdescription");
            entity.Property(e => e.Productdiscountamount).HasColumnName("productdiscountamount");
            entity.Property(e => e.Productmanufacturer).HasColumnName("productmanufacturer");
            entity.Property(e => e.Productname).HasColumnName("productname");
            entity.Property(e => e.Productphoto).HasColumnName("productphoto");
            entity.Property(e => e.Productquantityinstock).HasColumnName("productquantityinstock");
            entity.Property(e => e.Productstatus).HasColumnName("productstatus");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Roleid).HasName("role_pkey");

            entity.ToTable("role");

            entity.Property(e => e.Roleid).HasColumnName("roleid");
            entity.Property(e => e.Rolename)
                .HasMaxLength(100)
                .HasColumnName("rolename");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Userlogin).HasColumnName("userlogin");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");
            entity.Property(e => e.Userpassword).HasColumnName("userpassword");
            entity.Property(e => e.Userpatronymic)
                .HasMaxLength(100)
                .HasColumnName("userpatronymic");
            entity.Property(e => e.Userrole).HasColumnName("userrole");
            entity.Property(e => e.Usersurname)
                .HasMaxLength(100)
                .HasColumnName("usersurname");

            entity.HasOne(d => d.UserroleNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.Userrole)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("users_userrole_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
