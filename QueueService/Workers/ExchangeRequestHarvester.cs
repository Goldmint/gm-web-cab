using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public class ExchangeRequestHarvester : BaseWorker {

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethereumReader;

		private BigInteger _lastIndex;
		private BigInteger _lastSavedIndex;
		
		public ExchangeRequestHarvester() {
			_lastIndex = BigInteger.Zero;
			_lastSavedIndex = BigInteger.Zero;
		}

		protected override async Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethereumReader = services.GetRequiredService<IEthereumReader>();

			// get from db
			var last = await _dbContext.GetDBSetting(DbSetting.LastExchangeIndex, "0");
			if (BigInteger.TryParse(last, out _lastIndex)) {
				Logger.Info($"Using last exchange request index #{_lastIndex} (DB)");
			}
			_lastSavedIndex = _lastIndex;
		}

		protected override async Task Loop() {

			_dbContext.DetachEverything();

			var currentCount = await _ethereumReader.GetExchangeRequestsCount();

			Logger.Info(
				$"Current exchange requests count is {currentCount}. " + (
					_lastIndex < currentCount
					? $"{currentCount - _lastIndex} new request(s)"
					: $"Got nothing. {currentCount} (eth count) <= {_lastIndex} (last processed)"
				)
			);

			while (_lastIndex < currentCount) {

				if (IsCancelled()) break;

				var data = await _ethereumReader.GetExchangeRequestByIndex(_lastIndex);
				var userId = CoreLogic.User.ExtractId(data.UserId);

				// is pending
				if (data.IsPending) {

					Logger.Info($"Processing pending request #{_lastIndex}, userid {userId}, buying {data.IsBuyRequest}");

					_dbContext.DetachEverything();

					if (data.IsBuyRequest) {
						await CoreLogic.Finance.Tokens.GoldToken.PrepareEthBuyingRequest(
							services: _services,
							userId: userId,
							payload: data.Payload,
							address: data.Address,
							requestIndex: data.RequestIndex
						);
					} else {
						await CoreLogic.Finance.Tokens.GoldToken.PrepareEthSellingRequest(
							services: _services,
							userId: userId,
							payload: data.Payload,
							address: data.Address,
							requestIndex: data.RequestIndex
						);
					}
				}

				_lastIndex++;
			}

			// save last index to settings
			if (_lastSavedIndex != _lastIndex) {
				if (await _dbContext.SaveDbSetting(DbSetting.LastExchangeIndex, _lastIndex.ToString())) {
					_lastSavedIndex = _lastIndex;
					Logger.Info($"Last exchange request #{_lastIndex} saved to DB");
				}
			}
		}
	}
}
