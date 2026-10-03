using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// Single context for all entities
/// </summary>
/// <remarks>
/// <see cref="SaveChangesAsync(bool, CancellationToken)"/> calls every <see cref="IEntityChangedHandler{TEntity}"/> first,
/// until handlers make no new changes, then saves everything in one transaction.
/// </remarks>
public class AppDbContext(DbContextOptions<AppDbContext> options, IServiceProvider services) : DbContext(options)
{
	private const int MaxHandlerRounds = 10;

	private readonly IServiceProvider _services = services;


	public DbSet<StockItem> StockItems => this.Set<StockItem>();

	public DbSet<Transaction> Transactions => this.Set<Transaction>();

	public DbSet<Customer> Customers => this.Set<Customer>();

	public DbSet<Invoice> Invoices => this.Set<Invoice>();

	public DbSet<CreditNote> CreditNotes => this.Set<CreditNote>();

	public DbSet<AppSettings> AppSettings => this.Set<AppSettings>();

	public DbSet<User> Users => this.Set<User>();

	public DbSet<ImportBatch> ImportBatches => this.Set<ImportBatch>();

	public DbSet<Supplier> Suppliers => this.Set<Supplier>();

	public DbSet<PendingTransmission> PendingTransmissions => this.Set<PendingTransmission>();

	public DbSet<PaymentLink> PaymentLinks => this.Set<PaymentLink>();

	public DbSet<ContingencyCdcRange> ContingencyCdcRanges => this.Set<ContingencyCdcRange>();

	public DbSet<RemissionNote> RemissionNotes => this.Set<RemissionNote>();

	public DbSet<PendingRemisionTransmission> PendingRemisionTransmissions => this.Set<PendingRemisionTransmission>();

	public DbSet<GoodsImportDocument> GoodsImportDocuments => this.Set<GoodsImportDocument>();


	public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		await this.RunChangedHandlersAsync(cancellationToken);
		return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
	}

	/// <summary>
	/// Not supported: handlers are async
	/// </summary>
	/// <exception cref="NotSupportedException">Always</exception>
	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		throw new NotSupportedException("Use SaveChangesAsync.");
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
	}

	private async Task RunChangedHandlersAsync(CancellationToken cancellationToken)
	{
		Dictionary<object, EntityState> handled = new(ReferenceEqualityComparer.Instance);

		for (var round = 0; ; round++)
		{
			var pending = this.ChangeTracker.Entries()
				.Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
				.Where(entry => !handled.TryGetValue(entry.Entity, out var state) || state != entry.State)
				.ToList();

			if (pending.Count == 0) return;
			if (round == MaxHandlerRounds) throw new InvalidOperationException($"Change handlers still changing entities after {MaxHandlerRounds} rounds.");

			foreach (var entry in pending)
			{
				handled[entry.Entity] = entry.State;
				await this.RunChangedHandlersAsync(entry, cancellationToken);
			}
		}
	}

	private Task RunChangedHandlersAsync(EntityEntry entry, CancellationToken cancellationToken)
	{
		var changeType = entry.State switch
		{
			EntityState.Added => EntityChangeType.Added,
			EntityState.Deleted => EntityChangeType.Deleted,
			_ => EntityChangeType.Modified
		};
		var runner = (HandlerRunner)Activator.CreateInstance(typeof(HandlerRunner<>).MakeGenericType(entry.Metadata.ClrType))!;
		return runner.RunAsync(_services, entry.Entity, changeType, cancellationToken);
	}


	private abstract class HandlerRunner
	{
		public abstract Task RunAsync(IServiceProvider services, object entity, EntityChangeType changeType, CancellationToken cancellationToken);
	}

	private sealed class HandlerRunner<TEntity> : HandlerRunner where TEntity : class
	{
		public override async Task RunAsync(IServiceProvider services, object entity, EntityChangeType changeType, CancellationToken cancellationToken)
		{
			foreach (var handler in services.GetServices<IEntityChangedHandler<TEntity>>())
			{
				await handler.OnChangedAsync((TEntity)entity, changeType, cancellationToken);
			}
		}
	}
}
