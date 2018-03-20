using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public class CryptoExchangeRequestHarvester : BaseWorker {

		private readonly BigInteger _blocksPerRound;
		private readonly BigInteger _confirmationsRequired;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethereumReader;

		private BigInteger _lastBlock;
		private BigInteger _lastSavedBlock;
		
		public CryptoExchangeRequestHarvester(int blocksPerRound, int confirmationsRequired) {
			_blocksPerRound = new BigInteger(Math.Max(1, blocksPerRound));
			_confirmationsRequired = new BigInteger(Math.Max(1, confirmationsRequired));
			_lastBlock = BigInteger.Zero;
			_lastSavedBlock = BigInteger.Zero;
		}

		protected override async Task OnInit(IServiceProvider services) {

			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethereumReader = services.GetRequiredService<IEthereumReader>();
			var appConfig = services.GetRequiredService<AppConfig>();

			// get last block from config
			if (BigInteger.TryParse(appConfig.Services.Ethereum.CryptoExchangeRequest.FromBlock ?? "0", out var lbCfg) && lbCfg >= 0) {
				_lastBlock = lbCfg;
			}

			// get last block from db; remember last saved block
			if (BigInteger.TryParse(await _dbContext.GetDBSetting(DbSetting.LastCryptoExchangeBlockChecked, "0"), out var lbDb) && lbDb >= 0 && lbDb >= lbCfg) {
				_lastBlock = lbDb;
				_lastSavedBlock = lbDb;
			}
		}

		protected override async Task Loop() {

			var getFrom = _lastBlock;
			var getTo = _lastBlock + _blocksPerRound;
			_lastBlock = getTo;

			// get events
			var events = await _ethereumReader.GetEthDepositedEvent(getFrom, getTo, _confirmationsRequired);
			Logger.Trace($"{events.Count} eth payments gathered while checking blocks {getFrom} - {getTo}");

			foreach (var v in events) {

				// TODO: put record to the queue

				if (_lastBlock > v.BlockchainLatestBlock) {
					_lastBlock = v.BlockchainLatestBlock;
				}
			}

			// save last index to settings
			if (_lastSavedBlock != _lastBlock) {
				if (await _dbContext.SaveDbSetting(DbSetting.LastCryptoExchangeBlockChecked, _lastBlock.ToString())) {
					_lastSavedBlock = _lastBlock;
				}
			}
		}
	}
}
