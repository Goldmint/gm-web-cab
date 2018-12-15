using Goldmint.Common;
using Goldmint.DAL;
using NLog;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.CoreLogic.Services.RuntimeConfig.Impl {

	public sealed class DbRuntimeConfigLoader : IRuntimeConfigLoader {

		private readonly ApplicationDbContext _dbContext;
		private readonly ILogger _logger;

		public DbRuntimeConfigLoader(ApplicationDbContext dbContext, LogFactory logFactory) {
			_dbContext = dbContext;
			_logger = logFactory.GetLoggerFor(this);
		}

		public async Task<string> Load() {
			_logger.Info("Loading runtime config from DB");
			return await _dbContext.GetDbSetting(DbSetting.RuntimeConfig, "{}");
		}

		public async Task<bool> Save(string json) {
			_logger.Info("Saving runtime config to DB");
			return await _dbContext.SaveDbSetting(DbSetting.RuntimeConfig, json);
		}
	}
}
