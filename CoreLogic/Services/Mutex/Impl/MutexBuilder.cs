using FluentValidation;
using Goldmint.Common;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Mutex.Impl {

	public sealed class MutexBuilder {

		private readonly IMutexHolder _holder;
		private string _mutex;
		private string _locker;
		private TimeSpan _timeout;

		public MutexBuilder(IMutexHolder holder) {
			_holder = holder;
			_timeout = TimeSpan.FromMinutes(10);
			_locker = Guid.NewGuid().ToString("N");
		}

		public MutexBuilder Mutex(string mutex) {
			_mutex = mutex;
			return this;
		}

		public MutexBuilder Mutex(MutexEntity entity, string id) {
			_mutex = string.Format("{0}_{1}", entity.ToString(), id);
			return this;
		}

		public MutexBuilder Mutex(MutexEntity entity, int id) {
			_mutex = string.Format("{0}_{1}", entity.ToString(), id);
			return this;
		}

		public MutexBuilder Mutex(MutexEntity entity, long id) {
			_mutex = string.Format("{0}_{1}", entity.ToString(), id);
			return this;
		}

		public MutexBuilder Timeout(TimeSpan timeout) {
			_timeout = timeout;
			return this;
		}

		public MutexBuilder Timeout(int seconds) {
			_timeout = TimeSpan.FromSeconds(seconds);
			return this;
		}

		public async Task LockAsync(Action<bool> callback) {
			validate();

			var result = await _holder.SetMutexAsync(_mutex, _locker, _timeout);

			if (result) {
				try {
					callback(true);
					return;
				}
				finally {
					await _holder.UnsetMutexAsync(_mutex, _locker);
				}
			}

			callback(false);
		}

		public async Task LockAsync(Func<bool, Task> callback) {
			validate();

			var result = await _holder.SetMutexAsync(_mutex, _locker, _timeout);

			if (result) {
				try {
					await callback(true);
					return;
				}
				finally {
					await _holder.UnsetMutexAsync(_mutex, _locker);
				}
			}

			await callback(false);
		}

		public async Task<T> LockAsync<T>(Func<bool, Task<T>> callback) {
			validate();

			var result = await _holder.SetMutexAsync(_mutex, _locker, _timeout);

			if (result) {
				try {
					return await callback(true);
				}
				finally {
					await _holder.UnsetMutexAsync(_mutex, _locker);
				}
			}

			return await callback(false);
		}

		// ---

		private void validate() {
			_mutex = _mutex?.ToLower().Trim('_', ' ');
			_locker = _locker?.ToLower().Trim('_', ' ');

			new Validator().ValidateAndThrow(this);
		}

		internal class Validator : AbstractValidator<MutexBuilder> {

			public Validator() {
				RuleFor(x => x._mutex).Length(1, 64);
				RuleFor(x => x._locker).Length(1, 64);
				RuleFor(x => x._timeout).NotNull().Must(x => x.TotalSeconds > 0d);
			}
		}
	}
}
