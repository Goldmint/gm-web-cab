using Microsoft.EntityFrameworkCore;

namespace Goldmint.DAL {

	public class ScannerDbContext : DbContext {

		public virtual DbSet<ScannerModels.Transaction> Transaction { get; set; }
		public virtual DbSet<ScannerModels.Block> Block { get; set; }

		public ScannerDbContext(DbContextOptions<ScannerDbContext> options) : base(options) {
		}
	}
}
