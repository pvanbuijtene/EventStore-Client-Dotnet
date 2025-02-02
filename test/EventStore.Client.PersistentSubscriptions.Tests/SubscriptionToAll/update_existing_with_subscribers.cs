using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class update_existing_with_subscribers
		: IClassFixture<update_existing_with_subscribers.Fixture> {

		private const string Group = "existing";
		private readonly Fixture _fixture;

		public update_existing_with_subscribers(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task existing_subscriptions_are_dropped() {
			var (reason, exception) = await _fixture.Dropped.WithTimeout(TimeSpan.FromSeconds(10));
			Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
			var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);
			Assert.Equal(SystemStreams.AllStream, ex.StreamName);
			Assert.Equal(Group, ex.GroupName);
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception)> _droppedSource;
			public Task<(SubscriptionDroppedReason, Exception)> Dropped => _droppedSource.Task;
			private PersistentSubscription _subscription;

			public Fixture() {
				_droppedSource = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();
			}

			protected override async Task Given() {
				await Client.CreateToAllAsync(Group, new PersistentSubscriptionSettings(),
					TestCredentials.Root);
				_subscription = await Client.SubscribeToAllAsync(Group,
					delegate { return Task.CompletedTask; },
					(subscription, reason, ex) => _droppedSource.TrySetResult((reason, ex)), TestCredentials.Root);
			}

			protected override Task When() => Client.UpdateToAllAsync(Group,
				new PersistentSubscriptionSettings(), TestCredentials.Root);

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
