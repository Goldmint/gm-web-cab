﻿using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.Ethereum {

	public sealed class BuyRequestsHarvester : BaseWorker {

		private readonly int _blocksPerRound;
		private readonly int _confirmationsRequired;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethereumReader;

		private BigInteger _lastBlock;
		private BigInteger _lastSavedBlock;

		public BuyRequestsHarvester(int blocksPerRound, int confirmationsRequired) {
			_blocksPerRound = Math.Max(1, blocksPerRound);
			_confirmationsRequired = Math.Max(2, confirmationsRequired);
			_lastBlock = BigInteger.Zero;
			_lastSavedBlock = BigInteger.Zero;
		}

		protected override async Task OnInit(IServiceProvider services) {

			Logger.Info($"{_confirmationsRequired} confirmations required for buying at conract");

			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethereumReader = services.GetRequiredService<IEthereumReader>();
			var appConfig = services.GetRequiredService<AppConfig>();

			// get last block from config
			if (BigInteger.TryParse(appConfig.Services.Ethereum.CryptoExchangeRequest.FromBlock ?? "0", out var lbCfg) && lbCfg >= 0) {
				_lastBlock = lbCfg;

				Logger.Info($"Using last block #{lbCfg} (appsettings)");
			}

			// get last block from db; remember last saved block
			if (BigInteger.TryParse(await _dbContext.GetDBSetting(DbSetting.GoldEthBuyHarvLastBlock, "0"), out var lbDb) && lbDb >= 0 && lbDb >= lbCfg) {
				_lastBlock = lbDb;
				_lastSavedBlock = lbDb;

				Logger.Info($"Using last block #{lbDb} (DB)");
			}
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			// get events
			var log = await _ethereumReader.GatherTokenBuyRequestEvents(_lastBlock - 1, _lastBlock + _blocksPerRound, _confirmationsRequired);
			_lastBlock = log.ToBlock;

			Logger.Debug(
				(log.Events.Length > 0
					? $"{log.Events.Length} request(s) found"
					: "Nothing found"
				) + $" in blocks [{log.FromBlock} - {log.ToBlock}]"
			);

			foreach (var v in log.Events) {

				_dbContext.DetachEverything();

				Logger.Debug($"Trying to prepare request #{v.Reference}");

				if (v.Reference <= 0 || v.Reference > long.MaxValue) {
					Logger.Warn($"Invalid reference specified by user: {v.Reference}");
					continue;
				}
				if (v.RequestIndex < 0) {
					Logger.Warn($"Invalid request index: {v.RequestIndex}");
					continue;
				}

				/*var reqUserId = CoreLogic.User.ExtractId(v.UserId);
				if (reqUserId == null) {
					Logger.Warn($"Invalid user id: {v.UserId}");
					continue;
				}*/

				var pdResult = await CoreLogic.Finance.GoldToken.ProcessContractBuyRequest(
					services: _services,
					requestIndex: v.RequestIndex,
					internalRequestId: (long)v.Reference,
					address: v.Address,
					amountEth: v.EthAmount,
					txId: v.TransactionId,
					txConfirmationsRequired: _confirmationsRequired
				);

				Logger.Info(
					$"Request #{v.Reference} result is {pdResult.ToString()}"
				);
			}

			// save last index to settings
			if (_lastSavedBlock != _lastBlock) {
				if (await _dbContext.SaveDbSetting(DbSetting.GoldEthBuyHarvLastBlock, _lastBlock.ToString())) {
					_lastSavedBlock = _lastBlock;
					Logger.Info($"Last block #{_lastBlock} saved to DB");
				}
			}

		}
	}
}