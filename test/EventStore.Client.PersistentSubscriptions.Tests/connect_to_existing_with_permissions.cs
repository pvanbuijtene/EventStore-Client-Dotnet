using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class connect_to_existing_with_permissions
		: IClassFixture<connect_to_existing_with_permissions.Fixture> {
		private const string Stream = nameof(connect_to_existing_with_permissions);

		private readonly Fixture _fixture;

		public connect_to_existing_with_permissions(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_succeeds() {
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();
			using var subscription = _fixture.Client.Subscribe(Stream, "agroupname17",
				delegate { return Task.CompletedTask; }, (s, reason, ex) => dropped.TrySetResult((reason, ex)),
				TestCredentials.Root);
			Assert.NotNull(subscription);

			await Assert.ThrowsAsync<TimeoutException>(() => dropped.Task.WithTimeout());
		}

		public class Fixture : EventStoreClientFixture {
			public Fixture() {
			}

			protected override Task Given() =>
				Client.CreateAsync(
					Stream,
					"agroupname17",
					new PersistentSubscriptionSettings(),
					TestCredentials.Root);

			protected override Task When() => Task.CompletedTask;
		}
	}
}
