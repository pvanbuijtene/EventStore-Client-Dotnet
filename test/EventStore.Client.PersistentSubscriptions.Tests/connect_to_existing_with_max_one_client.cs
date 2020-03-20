using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class connect_to_existing_with_max_one_client
		: IClassFixture<connect_to_existing_with_max_one_client.Fixture> {
		private const string Group = "startinbeginning1";
		private const string Stream = nameof(connect_to_existing_with_max_one_client);
		private readonly Fixture _fixture;

		public connect_to_existing_with_max_one_client(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_second_subscription_fails_to_connect() {
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();

			using var first = _fixture.Client
				.Subscribe(Stream, Group, delegate { return Task.CompletedTask; }, userCredentials: TestCredentials.Root);
			await first.Started.WithTimeout();
			using var second = _fixture.Client
				.Subscribe(Stream, Group, delegate { return Task.CompletedTask; },
					(s, r, e) => dropped.SetResult((r, e)), userCredentials: TestCredentials.Root);
			await second.Started.WithTimeout();

			var (reason, exception) = await dropped.Task.WithTimeout();

			Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
			var ex = Assert.IsType<MaximumSubscribersReachedException>(exception);
			Assert.Equal(Stream, ex.StreamName);
			Assert.Equal(Group, ex.GroupName);
		}

		public class Fixture : EventStoreClientFixture {
			public Fixture() {
			}

			protected override Task Given() {
				return Client.CreateAsync(
					Stream,
					Group,
					new PersistentSubscriptionSettings(maxSubscriberCount: 1),
					TestCredentials.Root);
			}

			protected override Task When() => Task.CompletedTask;
		}
	}
}
